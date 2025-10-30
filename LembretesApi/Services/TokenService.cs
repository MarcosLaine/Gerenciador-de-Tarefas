using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using LembretesApi.Models;

namespace LembretesApi.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GerarToken(Usuario usuario)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id),
                new Claim(ClaimTypes.Email, usuario.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, usuario.Nome ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Usa a mesma lógica do Program.cs para ler JWT_KEY (prioridade: env var, depois config)
            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") 
                ?? Environment.GetEnvironmentVariable("Jwt__Key")
                ?? _configuration["Jwt:Key"] 
                ?? throw new InvalidOperationException("JWT Key não configurada");

            var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
                ?? Environment.GetEnvironmentVariable("Jwt__Issuer")
                ?? _configuration["Jwt:Issuer"]
                ?? "LembretesApi";

            var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                ?? Environment.GetEnvironmentVariable("Jwt__Audience")
                ?? _configuration["Jwt:Audience"]
                ?? "LembretesApp";

            // IMPORTANTE: Usa ASCII (mesmo encoding do Program.cs)
            var chave = new SymmetricSecurityKey(
                Encoding.ASCII.GetBytes(jwtKey)
            );

            var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credenciais
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

