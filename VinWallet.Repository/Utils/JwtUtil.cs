using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VinWallet.Domain.Models;
using VinWallet.Repository.Payload.Request;

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
            new Claim(ClaimTypes.Role, user.Role.Name),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
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
            new Claim(ClaimTypes.Role, user.Role.Name),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),

        };

            if (guidClaim != null)
            {
                refreshClaims.Add(new Claim(guidClaim.Item1, guidClaim.Item2.ToString()));
            }
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


        public static JwtResponse RefreshToken(RefreshTokenRequest refreshTokenRequest)
        {
            var refreshTokenSecret = "AnotherSuperSecretKeyForRefreshToken!";
            var accessTokenSecret = "SuperStrongSecretKeyForJwtToken123!";

            var tokenHandler = new JwtSecurityTokenHandler();


            try
            {
                var tokenValidationParams = new TokenValidationParameters
                {
                    ValidIssuer = "Issuer",
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey =
                         new SymmetricSecurityKey(Encoding.UTF8.GetBytes(refreshTokenSecret)),
                    RoleClaimType = ClaimTypes.Role
                };


                var principal = tokenHandler.ValidateToken(refreshTokenRequest.RefreshToken, tokenValidationParams, out var validatedToken);

                if (validatedToken is not JwtSecurityToken jwtToken)
                {
                    throw new SecurityTokenException("Invalid refresh token");
                }
                var userId = principal.FindFirst("UserId")?.Value;
                var role = principal.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role) || !userId.ToString().Equals(refreshTokenRequest.UserId.ToString()))
                {
                    throw new SecurityTokenException("Invalid refresh token data");
                }

                var user = new User
                {
                    Id = refreshTokenRequest.UserId,
                    Role = new Role { Name = role }
                };
                Tuple<string, Guid> guidClaim = new Tuple<string, Guid>("UserId", user.Id);
                return GenerateJwtToken(user, guidClaim);
            }
            catch (Exception)
            {
                throw new SecurityTokenException("Invalid or expired refresh token");
            }
        }

    }
}

