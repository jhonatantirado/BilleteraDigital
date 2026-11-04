using System.Security.Cryptography;
using System.Text;
using BilleteraDigital.Domain.Excepciones;

namespace BilleteraDigital.Domain.Entidades;

/// <summary>
/// Código de verificación (OTP) enviado por SMS para demostrar que el celular
/// pertenece a quien se registró. Sin esta verificación, alguien podría
/// registrarse con el número de otra persona y recibir las transferencias
/// dirigidas a ese celular.
///
/// Reglas: el código vence a los 10 minutos, admite 5 intentos y solo se
/// puede reenviar cada 60 segundos. Se guarda el HASH, nunca el código.
/// </summary>
public class VerificacionTelefono
{
    public static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan EsperaReenvio = TimeSpan.FromSeconds(60);
    public const int MaximoIntentos = 5;

    public Guid ClienteId { get; private set; }
    public string CodigoHash { get; private set; } = string.Empty;
    public DateTime Expira { get; private set; }
    public int Intentos { get; private set; }
    public DateTime UltimoEnvio { get; private set; }

    private VerificacionTelefono() { }

    public static VerificacionTelefono Generar(Guid clienteId, string codigo, DateTime ahora)
    {
        var v = new VerificacionTelefono { ClienteId = clienteId };
        v.Asignar(codigo, ahora);
        return v;
    }

    /// <summary>Reemplaza el código por uno nuevo (respetando la espera entre envíos).</summary>
    public void Renovar(string codigo, DateTime ahora)
    {
        var restante = UltimoEnvio + EsperaReenvio - ahora;
        if (restante > TimeSpan.Zero)
            throw new ReglaDeNegocioException(
                $"Espera {Math.Ceiling(restante.TotalSeconds)} segundos antes de pedir otro código.");
        Asignar(codigo, ahora);
    }

    /// <summary>
    /// Comprueba el código. Devuelve false si es incorrecto (y cuenta el intento);
    /// lanza ReglaDeNegocioException si venció o si se agotaron los intentos.
    /// </summary>
    public bool Verificar(string? codigo, DateTime ahora)
    {
        if (Intentos >= MaximoIntentos)
            throw new ReglaDeNegocioException("Demasiados intentos. Solicita un código nuevo.");
        if (ahora > Expira)
            throw new ReglaDeNegocioException("El código venció. Solicita uno nuevo.");

        Intentos++;
        var esperado = Convert.FromHexString(CodigoHash);
        var recibido = Convert.FromHexString(Hash(ClienteId, (codigo ?? string.Empty).Trim()));
        return CryptographicOperations.FixedTimeEquals(esperado, recibido); // sin fugas por tiempo
    }

    private void Asignar(string codigo, DateTime ahora)
    {
        if (codigo.Length != 6 || !codigo.All(char.IsDigit))
            throw new ArgumentException("El código debe tener 6 dígitos.", nameof(codigo));

        CodigoHash = Hash(ClienteId, codigo);
        Expira = ahora + Vigencia;
        Intentos = 0;
        UltimoEnvio = ahora;
    }

    // El Id del cliente actúa como "sal": el mismo código de dos clientes da hashes distintos
    private static string Hash(Guid clienteId, string codigo) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{clienteId:N}:{codigo}")));
}
