namespace BilleteraDigital.Application.Idempotencia;

/// <summary>
/// Almacén de claves de idempotencia. La garantía contra peticiones simultáneas
/// está en <see cref="IntentarReservarAsync"/>: debe ser ATÓMICO (en PostgreSQL,
/// un INSERT sobre la clave primaria), de modo que solo una petición lo consiga.
/// </summary>
public interface IIdempotenciaRepository
{
    /// <summary>Reserva la clave en estado EnProceso. Devuelve false si ya existía.</summary>
    Task<bool> IntentarReservarAsync(Guid clienteId, string clave, string ruta, string hashCuerpo, DateTime fecha);

    Task<RegistroIdempotencia?> ObtenerAsync(Guid clienteId, string clave);

    /// <summary>Guarda la respuesta definitiva y marca la clave como Completada.</summary>
    Task CompletarAsync(Guid clienteId, string clave, int codigoHttp, string respuesta, string tipoContenido);

    /// <summary>Borra la reserva (p. ej. tras un error 5xx) para que el cliente pueda reintentar.</summary>
    Task LiberarAsync(Guid clienteId, string clave);

    /// <summary>Elimina las claves creadas antes de <paramref name="limite"/>. Devuelve cuántas borró.</summary>
    Task<int> EliminarAnterioresAAsync(DateTime limite);
}
