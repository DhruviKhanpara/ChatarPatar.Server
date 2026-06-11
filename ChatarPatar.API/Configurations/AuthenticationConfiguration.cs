using ChatarPatar.API.Models;
using ChatarPatar.Common.Consts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace ChatarPatar.API.Configuration
{
    public static class AuthenticationConfiguration
    {
        public static void AddAuthenticationConfiguration(this IServiceCollection services, IConfiguration configuration)
          {
            var accessTokenName = configuration.GetSection("TokenSettings:AccessTokenName").Value!;

            services.AddAuthentication(option =>
            {
                option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(option =>
            {
                option.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetSection("TokenSettings:SecretKey").Value!)),

                    ValidateAudience = true,
                    ValidAudiences = configuration.GetSection("TokenSettings:Audience").Get<List<string>>(),

                    ValidateIssuer = true,
                    ValidIssuer = configuration.GetSection("TokenSettings:Issuer").Value,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = ClaimTypes.Role
                };

                option.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrWhiteSpace(context.Token))
                        {
                            context.Request.Cookies.TryGetValue(accessTokenName, out var token);
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },

                    OnChallenge = context =>
                    {
                        context.HandleResponse();

                        string exceptionCode;
                        string message;

                        bool hasToken = !string.IsNullOrWhiteSpace(context.Request.Headers["Authorization"]) || context.Request.Cookies.ContainsKey(accessTokenName);

                        if (!hasToken)
                        {
                            exceptionCode = ExceptionCodes.AUTH_REQUIRED;
                            message = "Authentication is required. Please provide a valid token.";
                        }
                        else if (context.AuthenticateFailure is SecurityTokenExpiredException)
                        {
                            exceptionCode = ExceptionCodes.TOKEN_EXPIRED;
                            message = "Your session has expired. Please log in again.";
                        }
                        else
                        {
                            exceptionCode = ExceptionCodes.TOKEN_INVALID;
                            message = "The provided token is invalid.";
                        }

                        SetErrorContext(context.HttpContext, HttpStatusCode.Unauthorized, exceptionCode, message);
                        return Task.CompletedTask;
                    },

                    OnForbidden = context =>
                    {
                        SetErrorContext(context.HttpContext, HttpStatusCode.Forbidden, ExceptionCodes.FORBIDDEN, "You do not have permission to perform this action.");
                        return Task.CompletedTask;
                    }
                };
            });
        }

        private static void SetErrorContext(HttpContext httpContext, HttpStatusCode statusCode, string exceptionCode, string message)
        {
            httpContext.Items["ExceptionCode"] = exceptionCode;
            httpContext.Items["StatusMessage"] = message;

            httpContext.Response.StatusCode = (int)statusCode;
        }
    }
}