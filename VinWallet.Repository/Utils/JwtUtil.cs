using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using VinWallet.Domain.Models;

namespace VinWallet.Repository.Utils
{
    public class JwtUtil
    {
        private JwtUtil() { }

        public static JwtResponse GenerateJwtToken(User user, Tuple<string, Guid> guidClaim)
        {
            var accessTokenSecret = "SuperStrongSecretKeyForJwtToken123!";
            var refreshTokenSecret = "AnotherSuperSecretKeyForRefreshToken!";

            var accessKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(accessTokenSecret));
            var refreshKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(refreshTokenSecret));

            var accessCredentials = new SigningCredentials(accessKey, SecurityAlgorithms.HmacSha256Signature);
            var refreshCredentials = new SigningCredentials(refreshKey, SecurityAlgorithms.HmacSha256Signature);

            string issuer = "Issuer";

            List<Claim> accessClaims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(ClaimTypes.Role, user.Role.Name),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

            if (guidClaim != null)
            {
                accessClaims.Add(new Claim(guidClaim.Item1, guidClaim.Item2.ToString()));
            }

            var accessTokenExpires = DateTime.UtcNow.AddHours(6);
            var refreshTokenExpires = DateTime.UtcNow.AddDays(7);

            var accessToken = new JwtSecurityToken(
                issuer: issuer,
                audience: null,
                claims: accessClaims,
                notBefore: DateTime.UtcNow,
                expires: accessTokenExpires,
                signingCredentials: accessCredentials
            );

            string accessTokenString = new JwtSecurityTokenHandler().WriteToken(accessToken);

            List<Claim> refreshClaims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(ClaimTypes.Role, user.Role.Name),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

            var refreshToken = new JwtSecurityToken(
                issuer: issuer,
                audience: null,
                claims: refreshClaims,
                notBefore: DateTime.UtcNow,
                expires: refreshTokenExpires,
                signingCredentials: refreshCredentials
            );

            string refreshTokenString = new JwtSecurityTokenHandler().WriteToken(refreshToken);

            return new JwtResponse
            {
                AccessToken = accessTokenString,
                AccessTokenExpires = accessTokenExpires,
                RefreshToken = refreshTokenString,
                RefreshTokenExpires = refreshTokenExpires
            };
        }

        public class JwtResponse
        {
            public string AccessToken { get; set; }
            public DateTime AccessTokenExpires { get; set; }
            public string RefreshToken { get; set; }
            public DateTime RefreshTokenExpires { get; set; }
        }
    }
}

