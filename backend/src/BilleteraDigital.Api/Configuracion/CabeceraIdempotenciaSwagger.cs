using BilleteraDigital.Api.Filtros;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BilleteraDigital.Api.Configuracion;

/// <summary>Muestra en Swagger el campo Idempotency-Key en las acciones marcadas con [Idempotente].</summary>
public class CabeceraIdempotenciaSwagger : IOperationFilter
{
    public void Apply(OpenApiOperation operacion, OperationFilterContext contexto)
    {
        if (!contexto.MethodInfo.GetCustomAttributes(true).OfType<IdempotenteAttribute>().Any()) return;

        operacion.Parameters ??= new List<OpenApiParameter>();
        operacion.Parameters.Add(new OpenApiParameter
        {
            Name = IdempotenciaFilter.Cabecera,
            In = ParameterLocation.Header,
            Required = true,
            Description = "Identificador único de esta operación (p. ej. un UUID). " +
                          "Repetir la petición con la misma clave NO repite el movimiento de dinero.",
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
        });
    }
}
