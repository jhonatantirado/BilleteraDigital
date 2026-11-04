using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using BilleteraDigital.Api.Middleware;
using Microsoft.AspNetCore.RateLimiting;

namespace BilleteraDigital.Api.Configuracion;

/// <summary>
/// LÍMITE DE USO (rate limiting, incluido en ASP.NET Core 8).
/// Los celulares peruanos son 9 dígitos que empiezan con 9: se pueden recorrer.
/// Sin límite, alguien con una cuenta podría probar números para averiguar quién
/// está registrado. Se permiten 10 búsquedas de destinatario por minuto y por
/// usuario (por cuenta o por celular); al pasarse, la API responde 429.
/// </summary>
public static class LimitesDeUso
{
    public const string BusquedaDestinatario = "busqueda-destinatario";
    public const int BusquedasPorMinuto = 10;

    public static IServiceCollection AddLimitesDeUso(this IServiceCollection services) =>
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.OnRejected = async (contexto, cancelar) =>
            {
                var http = contexto.HttpContext;
                http.Response.ContentType = TraductorErrores.TipoContenido;
                if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                    http.Response.Headers.RetryAfter = ((int)Math.Ceiling(espera.TotalSeconds)).ToString();

                var problema = TraductorErrores.Crear(StatusCodes.Status429TooManyRequests,
                    "Demasiadas búsquedas",
                    $"Puedes buscar hasta {BusquedasPorMinuto} destinatarios por minuto. Intenta de nuevo en un momento.",
                    http.Request.Path);
                await JsonSerializer.SerializeAsync(http.Response.Body, problema,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web), cancelar);
            };

            // Una "ventana" de 1 minuto por usuario (el claim "sub" del token)
            o.AddPolicy(BusquedaDestinatario, http => RateLimitPartition.GetFixedWindowLimiter(
                http.User.FindFirstValue("sub") ?? http.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = BusquedasPorMinuto,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        });
}
