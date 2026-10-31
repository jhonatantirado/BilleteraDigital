namespace BilleteraDigital.Application.Interfaces;

/// <summary>Abstracción del algoritmo de hash (implementado en Infraestructura con PBKDF2).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verificar(string password, string hash);
}
