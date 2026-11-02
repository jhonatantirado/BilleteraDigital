namespace BilleteraDigital.Application.Patrones.Strategy;

/// <summary>
/// Datos que las estrategias necesitan para decidir y calcular la comisión.
/// Agrupar los datos en un objeto permite agregar criterios nuevos (antigüedad,
/// tipo de cliente, promociones...) sin cambiar la firma de <see cref="IComisionStrategy"/>.
/// </summary>
/// <param name="Monto">Monto de la transferencia.</param>
/// <param name="FechaRegistroCliente">Fecha (UTC) en que se registró el cliente emisor.</param>
/// <param name="Ahora">Fecha (UTC) de la operación; se inyecta para poder probar con fechas fijas.</param>
public record ContextoComision(decimal Monto, DateTime FechaRegistroCliente, DateTime Ahora)
{
    /// <summary>¿El cliente tiene MÁS de <paramref name="meses"/> meses de antigüedad?</summary>
    public bool ClienteConMasDe(int meses) => FechaRegistroCliente.AddMonths(meses) < Ahora;
}
