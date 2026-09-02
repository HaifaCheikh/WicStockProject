using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WicStock_.Models;

namespace WicStock_.Services
{
    public class JwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenererToken(Utilisateur utilisateur)
        {
            var jwtKey = _config["Jwt:Key"] ?? "WicStockDefaultFallbackSecretKey2026Min32Chars!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var roleString = utilisateur.Role.ToString();
            Console.WriteLine($"[JWT] Génération token pour {utilisateur.Email}, Rôle: {roleString} (enum: {utilisateur.Role})");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, utilisateur.Id.ToString()),
                new Claim(ClaimTypes.Name, utilisateur.Nom),
                new Claim(ClaimTypes.Email, utilisateur.Email),
                new Claim(ClaimTypes.Role, roleString)
            };

            var expireMinutes = 60.0;
            var expireConfig = _config["Jwt:ExpireMinutes"];
            if (!string.IsNullOrWhiteSpace(expireConfig) && !double.TryParse(expireConfig, out expireMinutes))
            {
                expireMinutes = 60.0;
            }

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "WicStock",
                audience: _config["Jwt:Audience"] ?? "WicStockUsers",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}