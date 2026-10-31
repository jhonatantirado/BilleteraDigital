using System.Net;
using BilleteraDigital.Application.Excepciones;
using BilleteraDigital.Domain.Excepciones;
using Microsoft.AspNetCore.Mvc;

namespace BilleteraDigital.Api.Middleware;

/// <summary>
/// Traduce excepciones de las capas internas a códigos HTTP con un cuerpo
/// estándar (RFC 7807 - ProblemDetails). Así los controladores quedan limpios
/// de try/catch y el cliente Flutter siempre recibe el mismo formato de error.
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
            var (estado, titulo) = ex switch
            {
                NoEncontradoException => (HttpStatusCode.NotFound, "Recurso no encontrado"),
                ConflictoException => (HttpStatusCode.Conflict, "Conflicto"),
                CredencialesInvalidasException => (HttpStatusCode.Unauthorized, "No autorizado"),
                ReglaDeNegocioException => (HttpStatusCode.UnprocessableEntity, "Regla de negocio"),
                ArgumentException => (HttpStatusCode.BadRequest, "Solicitud inválida"),
                _ => (HttpStatusCode.InternalServerError, "Error interno")
            };

            if (estado == HttpStatusCode.InternalServerError)
                _log.LogError(ex, "Error no controlado");

            var problema = new ProblemDetails
            {
                Status = (int)estado,
                Title = titulo,
                Detail = estado == HttpStatusCode.InternalServerError
                    ? "Ocurrió un error inesperado." : ex.Message,
                Instance = contexto.Request.Path
            };

            contexto.Response.StatusCode = (int)estado;
            await contexto.Response.WriteAsJsonAsync(problema, (System.Text.Json.JsonSerializerOptions?)null,
                "application/problem+json");
        }
    }
}
