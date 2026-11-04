using BilleteraDigital.Domain.Entidades;

namespace BilleteraDigital.Domain.Interfaces;

public interface IBilleteraRepository
{
    Task<Billetera?> ObtenerPorIdAsync(Guid id);
    Task<Billetera?> ObtenerPorClienteIdAsync(Guid clienteId);
    Task<Billetera?> ObtenerPorNumeroCuentaAsync(string numeroCuenta);

    /// <summary>Billetera cuyo titular tiene este celular VERIFICADO (formato E.164).</summary>
    Task<Billetera?> ObtenerPorTelefonoAsync(string telefono);

    /// <summary>
    /// Obtiene la billetera bloqueando su fila (SELECT ... FOR UPDATE)
    /// hasta que termine la transacción. Evita que dos transferencias
    /// simultáneas lean el mismo saldo (condición de carrera).
    /// </summary>
    Task<Billetera?> ObtenerPorIdParaActualizarAsync(Guid id);

    Task<bool> ExisteNumeroCuentaAsync(string numeroCuenta);
    Task AgregarAsync(Billetera billetera);
}
