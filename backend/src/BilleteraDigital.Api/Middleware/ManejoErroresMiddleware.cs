namespace BilleteraDigital.Api.Middleware;

/// <summary>
/// Traduce excepciones de las capas internas a códigos HTTP con un cuerpo
/// estándar (RFC 7807 - ProblemDetails). Así los controladores quedan limpios
/// de try/catch y el cliente Flutter siempre recibe el mismo formato de error.
/// La tabla de traducción está en <see cref="TraductorErrores"/>.
/// </summary>
public class ManejoErroresMiddleware
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ManejoErroresMiddleware> _log;

    public ManejoErroresMiddleware(RequestDelegate siguiente, ILogger<ManejoErroresMiddleware> log)
    {
        _siguiente = siguiente;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _siguiente(contexto);
        }
        catch (Exception ex)
        {
            var (codigo, problema) = TraductorErrores.Traducir(ex, contexto.Request.Path);

            if (codigo >= StatusCodes.Status500InternalServerError)
                _log.LogError(ex, "Error no controlado");

            contexto.Response.StatusCode = codigo;
            await contexto.Response.WriteAsJsonAsync(problema, (System.Text.Json.JsonSerializerOptions?)null,
                TraductorErrores.TipoContenido);
        }
    }
}
