using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using BilleteraDigital.Api.Configuracion;
using BilleteraDigital.Api.Middleware;
using BilleteraDigital.Application.Idempotencia;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace BilleteraDigital.Api.Filtros;

/// <summary>
/// Activa la idempotencia en una acción: <c>[Idempotente]</c>.
/// El filtro se crea con inyección de dependencias (TypeFilterAttribute).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotenteAttribute : TypeFilterAttribute
{
    public IdempotenteAttribute() : base(typeof(IdempotenciaFilter)) { }
}

/// <summary>
/// IDEMPOTENCIA con la cabecera <c>Idempotency-Key</c>: una misma intención de
/// pago se ejecuta UNA sola vez, aunque la petición llegue varias veces
/// (doble toque, reintento tras un timeout, red inestable).
///
/// El filtro ENVUELVE a la acción del controlador (misma idea que el Decorator,
/// dentro de la cadena de filtros de ASP.NET). BilleteraService no sabe nada de esto.
///
///   1. Sin cabecera o con formato inválido      → 400
///   2. Reservar la clave (INSERT atómico)        → solo una petición lo logra
///      - ya existía con otros datos              → 422
///      - ya existía y sigue en proceso           → 409
///      - ya existía y está completada            → misma respuesta + Idempotent-Replayed: true
///   3. Ejecutar la acción
///   4. 2xx o error de negocio (4xx)              → se guarda la respuesta
///      error inesperado (5xx)                    → se borra la reserva (se puede reintentar)
/// </summary>
public class IdempotenciaFilter : IAsyncActionFilter
{
    public const string Cabecera = "Idempotency-Key";
    public const string CabeceraRepetida = "Idempotent-Replayed";

    /// <summary>
    /// Una reserva EnProceso más antigua que esto se considera abandonada (el servidor
    /// se cayó a mitad de la petición) y se vuelve a procesar. Ver la limitación en el README.
    /// </summary>
    public static readonly TimeSpan ReservaAbandonada = TimeSpan.FromMinutes(2);

    private const string TipoJson = "application/json; charset=utf-8";
    private static readonly Regex Formato = new("^[A-Za-z0-9_-]{8,64}$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions OpcionesWeb = new(JsonSerializerDefaults.Web);

    private readonly IIdempotenciaRepository _repo;
    private readonly TimeProvider _reloj;
    private readonly JsonSerializerOptions _json;
    private readonly ILogger<IdempotenciaFilter> _log;

    public IdempotenciaFilter(IIdempotenciaRepository repo, TimeProvider reloj,
                              IOptions<JsonOptions> json, ILogger<IdempotenciaFilter> log)
    {
        _repo = repo;
        _reloj = reloj;
        _json = json.Value.JsonSerializerOptions; // mismas opciones que usan los controladores
        _log = log;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext contexto, ActionExecutionDelegate siguiente)
    {
        var http = contexto.HttpContext;
        var ruta = http.Request.Path.Value ?? string.Empty;

        // ---------------------------------------------------------------- 1. cabecera
        var clave = http.Request.Headers[Cabecera].ToString().Trim();
        if (clave.Length == 0)
        {
            contexto.Result = Problema(400, "Falta la cabecera Idempotency-Key",
                "Las operaciones que mueven dinero exigen la cabecera Idempotency-Key (por ejemplo, un UUID).", ruta);
            return;
        }
        if (!Formato.IsMatch(clave))
        {
            contexto.Result = Problema(400, "Idempotency-Key inválida",
                "La clave debe tener entre 8 y 64 caracteres: letras, números, '-' o '_'.", ruta);
            return;
        }

        var clienteId = http.User.ClienteId();       // la clave es por cliente (sale del token)
        var hash = CalcularHash(contexto.ActionArguments);

        // ---------------------------------------------------------------- 2. reserva
        var reservada = false;
        for (var intento = 0; intento < 2 && !reservada; intento++)
        {
            if (await _repo.IntentarReservarAsync(clienteId, clave, ruta, hash, Ahora()))
            {
                reservada = true;
                break;
            }

            var previo = await _repo.ObtenerAsync(clienteId, clave);
            if (previo is null) continue;            // se liberó justo ahora: reintentar la reserva

            if (previo.Ruta != ruta || previo.HashCuerpo != hash)
            {
                contexto.Result = Problema(422, "Idempotency-Key reutilizada",
                    "Esta clave ya se usó para una operación distinta. Genera una clave nueva para cada operación.", ruta);
                return;
            }

            if (previo.Estado == EstadoIdempotencia.Completada)
            {
                _log.LogInformation("Idempotencia: se repite la respuesta de {Ruta} para la clave {Clave}", ruta, clave);
                http.Response.Headers[CabeceraRepetida] = "true";
                contexto.Result = Contenido(previo.CodigoHttp ?? 200, previo.Respuesta ?? "", previo.TipoContenido ?? TipoJson);
                return;
            }

            if (Ahora() - previo.FechaCreacion > ReservaAbandonada)
            {
                _log.LogWarning("Idempotencia: reserva abandonada para la clave {Clave}; se vuelve a procesar", clave);
                await _repo.LiberarAsync(clienteId, clave);
                continue;
            }

            break; // sigue en proceso
        }

        if (!reservada)
        {
            contexto.Result = Problema(409, "Operación en curso",
                "Tu operación anterior todavía se está procesando. Intenta de nuevo en unos segundos.", ruta);
            return;
        }

        // ---------------------------------------------------------------- 3. acción
        ActionExecutedContext ejecutado;
        try
        {
            ejecutado = await siguiente();
        }
        catch
        {
            await _repo.LiberarAsync(clienteId, clave);
            throw;
        }

        // ---------------------------------------------------------------- 4. resultado
        if (ejecutado.Exception is not null && !ejecutado.ExceptionHandled)
        {
            var (codigo, problema) = TraductorErrores.Traducir(ejecutado.Exception, ruta);
            if (codigo >= 500)
            {
                await _repo.LiberarAsync(clienteId, clave); // no es definitivo: se permite reintentar
                return;                                     // el middleware responde el 500
            }

            // Error de negocio (p. ej. saldo insuficiente): es una respuesta definitiva
            var json = JsonSerializer.Serialize(problema, OpcionesWeb);
            await GuardarAsync(clienteId, clave, codigo, json, TraductorErrores.TipoContenido);
            ejecutado.Result = Contenido(codigo, json, TraductorErrores.TipoContenido);
            ejecutado.ExceptionHandled = true;
            return;
        }

        if (ejecutado.Result is ObjectResult { StatusCode: var estado } resultado && (estado ?? 200) < 500)
        {
            var valor = resultado.Value;
            var json = JsonSerializer.Serialize(valor, valor?.GetType() ?? typeof(object), _json);
            await GuardarAsync(clienteId, clave, estado ?? 200, json, TipoJson);
            return;
        }

        await _repo.LiberarAsync(clienteId, clave); // resultado sin cuerpo: no se guarda
    }

    private async Task GuardarAsync(Guid clienteId, string clave, int codigo, string json, string tipo)
    {
        try
        {
            await _repo.CompletarAsync(clienteId, clave, codigo, json, tipo);
        }
        catch (Exception ex)
        {
            // La operación YA ocurrió: no se debe convertir en error para el usuario.
            _log.LogError(ex, "Idempotencia: no se pudo guardar la respuesta de la clave {Clave}", clave);
        }
    }

    /// <summary>
    /// SHA-256 de los argumentos de la acción (el body ya convertido en DTO), ordenados
    /// por nombre. Detecta la misma clave reutilizada con otro monto o destino.
    /// </summary>
    public static string CalcularHash(IDictionary<string, object?> argumentos)
    {
        var ordenados = argumentos.OrderBy(a => a.Key, StringComparer.Ordinal)
                                  .ToDictionary(a => a.Key, a => a.Value);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(ordenados, OpcionesWeb);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private DateTime Ahora() => _reloj.GetUtcNow().UtcDateTime;

    private static ContentResult Contenido(int codigo, string cuerpo, string tipo) =>
        new() { StatusCode = codigo, Content = cuerpo, ContentType = tipo };

    private static ContentResult Problema(int codigo, string titulo, string detalle, string ruta) =>
        Contenido(codigo, JsonSerializer.Serialize(TraductorErrores.Crear(codigo, titulo, detalle, ruta), OpcionesWeb),
                  TraductorErrores.TipoContenido);
}
