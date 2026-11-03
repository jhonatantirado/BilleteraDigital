using System.Net;
using BilleteraDigital.Application.Excepciones;
using BilleteraDigital.Domain.Excepciones;
using Microsoft.AspNetCore.Mvc;

namespace BilleteraDigital.Api.Middleware;

/// <summary>
/// Única tabla "excepción → código HTTP" de la API. La usan el middleware de
/// errores y el filtro de idempotencia (que guarda los errores de negocio para
/// devolver exactamente la misma respuesta si la petición se repite).
/// </summary>
public static class TraductorErrores
{
    public const string TipoContenido = "application/problem+json";

    public static (int Codigo, ProblemDetails Problema) Traducir(Exception ex, string? instancia)
    {
        var (estado, titulo) = ex switch
        {
            NoEncontradoException => (HttpStatusCode.NotFound, "Recurso no encontrado"),
            ConflictoException => (HttpStatusCode.Conflict, "Conflicto"),
            CredencialesInvalidasException => (HttpStatusCode.Unauthorized, "No autorizado"),
            AccesoDenegadoException => (HttpStatusCode.Forbidden, "Acceso denegado"),
            ReglaDeNegocioException => (HttpStatusCode.UnprocessableEntity, "Regla de negocio"),
            ArgumentException => (HttpStatusCode.BadRequest, "Solicitud inválida"),
            _ => (HttpStatusCode.InternalServerError, "Error interno")
        };

        var detalle = estado == HttpStatusCode.InternalServerError ? "Ocurrió un error inesperado." : ex.Message;
        return ((int)estado, Crear((int)estado, titulo, detalle, instancia));
    }

    public static ProblemDetails Crear(int codigo, string titulo, string detalle, string? instancia) => new()
    {
        Status = codigo,
        Title = titulo,
        Detail = detalle,
        Instance = instancia
    };
}
