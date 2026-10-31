using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BilleteraDigital.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BilleteraDigital.Infrastructure.Seguridad;

/// <summary>
/// Emite tokens JWT firmados con HMAC-SHA256 usando la sección "Jwt" de la configuración.
/// Claims: sub (cliente), email, billetera_id y jti (identificador único del token).
/// </summary>
public class JwtGeneradorToken : IGeneradorToken
{
    private readonly IConfigurationSection _cfg;

    public JwtGeneradorToken(IConfiguration config) => _cfg = config.GetSection("Jwt");

    public (string Token, DateTime Expira) Generar(Guid clienteId, Guid billeteraId, string email)
    {
        var claveTexto = _cfg["Clave"]
            ?? throw new InvalidOperationException("Falta Jwt:Clave en la configuración.");
        var expira = DateTime.UtcNow.AddMinutes(_cfg.GetValue("MinutosExpiracion", 60));
        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveTexto));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, clienteId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("billetera_id", billeteraId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var jwt = new JwtSecurityToken(
            issuer: _cfg["Emisor"],
            audience: _cfg["Audiencia"],
            claims: claims,
            expires: expira,
            signingCredentials: new SigningCredentials(clave, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expira);
    }
}
