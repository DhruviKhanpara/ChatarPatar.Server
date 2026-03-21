using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.HttpUserDetails;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ChatarPatar.Common.Security
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        #region Create Token
        public string CreateToken(string email, Guid id, string name)
        {
            var httpContext = _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

            List<Claim> claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.NameIdentifier, id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("TokenSettings:SecretKey").Value!));
            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                issuer: _configuration.GetSection("TokenSettings:Issuer").Value!,
                audience: ValidateAndPickAudience(httpContext!.GetOriginBaseURL()),
                claims: claims,
                signingCredentials: cred,
                expires: DateTime.UtcNow.AddMinutes(_configuration.GetValue<double>("TokenSettings:TokenExpirationMinutes"))
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
        #endregion

        #region Refresh Token generation

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return Convert.ToBase64String(randomBytes);
        }

        #endregion

        #region Encode refresh token

        public string HashToken(string token)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hash);
        }

        #endregion

        #region Private Section

        private string ValidateAndPickAudience(string requestOrigin)
        {
            var allowed = _configuration.GetSection("TokenSettings:Audience").Get<List<string>>();
            if (allowed != null && allowed.Contains(requestOrigin))
                return requestOrigin;

            throw new InvalidDataAppException("Request origin is not whitelisted");
        }

        #endregion
    }
}
