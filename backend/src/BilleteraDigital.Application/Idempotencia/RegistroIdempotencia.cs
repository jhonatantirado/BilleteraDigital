namespace BilleteraDigital.Application.Idempotencia;

public enum EstadoIdempotencia
{
    /// <summary>La primera petición con esta clave todavía se está procesando.</summary>
    EnProceso,
    /// <summary>Ya hay una respuesta definitiva guardada para esta clave.</summary>
    Completada
}

/// <summary>
/// Una clave de idempotencia recibida en la cabecera <c>Idempotency-Key</c>,
/// junto con la respuesta que se dio la primera vez.
/// </summary>
public class RegistroIdempotencia
{
    public Guid ClienteId { get; init; }
    public string Clave { get; init; } = string.Empty;
    public string Ruta { get; init; } = string.Empty;

    /// <summary>SHA-256 de los datos de la petición: detecta la misma clave usada para otra operación.</summary>
    public string HashCuerpo { get; init; } = string.Empty;

    public EstadoIdempotencia Estado { get; init; }
    public int? CodigoHttp { get; init; }
    public string? Respuesta { get; init; }
    public string? TipoContenido { get; init; }
    public DateTime FechaCreacion { get; init; }
}
