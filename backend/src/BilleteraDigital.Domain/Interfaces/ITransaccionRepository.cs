using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Enums;

namespace BilleteraDigital.Domain.Interfaces;

public interface ITransaccionRepository
{
    Task AgregarAsync(Transaccion transaccion);

    /// <summary>Historial paginado, del más reciente al más antiguo.</summary>
    Task<(IReadOnlyList<Transaccion> Items, int Total)> ObtenerHistorialAsync(
        Guid billeteraId, int pagina, int tamanoPagina, TipoTransaccion? tipo);
}
