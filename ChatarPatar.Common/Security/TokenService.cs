using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Common.Models;
using ChatarPatar.Common.Security.SecurityContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ChatarPatar.Common.Security
{
    internal class TokenService : ITokenService
    {
        private readonly TokenSettings _tokenSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenService(IOptions<TokenSettings> tokenSettings, IHttpContextAccessor httpContextAccessor)
        {
            _tokenSettings = tokenSettings.Value;
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

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenSettings.SecretKey));
            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                issuer: _tokenSettings.Issuer,
                audience: ValidateAndPickAudience(httpContext!.GetOriginBaseURL()),
                claims: claims,
                signingCredentials: cred,
                expires: DateTime.UtcNow.AddMinutes(_tokenSettings.TokenExpirationMinutes)
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
            if (_tokenSettings.Audience.Contains(requestOrigin))
                return requestOrigin;

            throw new InvalidDataAppException("Request origin is not whitelisted");
        }

        #endregion
    }
}
