using System.Security.Cryptography;
using BilleteraDigital.Application.Interfaces;

namespace BilleteraDigital.Infrastructure.Seguridad;

/// <summary>
/// Hash de contraseñas con PBKDF2-SHA256 (incluido en .NET, sin paquetes externos).
/// Formato almacenado: PBKDF2$iteraciones$salBase64$hashBase64
/// </summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iteraciones = 100_000;
    private const int TamanoSal = 16;
    private const int TamanoHash = 32;

    public string Hash(string password)
    {
        var sal = RandomNumberGenerator.GetBytes(TamanoSal);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, sal, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);
        return $"PBKDF2${Iteraciones}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    public bool Verificar(string password, string almacenado)
    {
        var partes = almacenado.Split('$');
        if (partes.Length != 4 || partes[0] != "PBKDF2") return false;

        var iteraciones = int.Parse(partes[1]);
        var sal = Convert.FromBase64String(partes[2]);
        var esperado = Convert.FromBase64String(partes[3]);
        var calculado = Rfc2898DeriveBytes.Pbkdf2(password, sal, iteraciones, HashAlgorithmName.SHA256, esperado.Length);

        // Comparación en tiempo constante (evita ataques de temporización)
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }
}
