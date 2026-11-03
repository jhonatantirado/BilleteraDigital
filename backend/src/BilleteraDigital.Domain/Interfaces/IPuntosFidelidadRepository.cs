using BilleteraDigital.Domain.Entidades;

namespace BilleteraDigital.Domain.Interfaces;

/// <summary>PATRÓN REPOSITORY: acceso a los puntos de fidelidad de los clientes.</summary>
public interface IPuntosFidelidadRepository
{
    /// <summary>¿Ya se otorgaron puntos por esta operación? (idempotencia)</summary>
    Task<bool> ExisteReferenciaAsync(Guid referencia);

    Task AgregarAsync(MovimientoPuntos movimiento);

    Task<int> ObtenerTotalAsync(Guid clienteId);

    Task<IReadOnlyList<MovimientoPuntos>> ObtenerUltimosAsync(Guid clienteId, int cantidad);
}
