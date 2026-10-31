using BilleteraDigital.Domain.Enums;

namespace BilleteraDigital.Domain.Entidades;

/// <summary>
/// Movimiento registrado en el historial de una billetera.
/// Una transferencia genera DOS transacciones (enviada y recibida)
/// enlazadas por el mismo valor de <see cref="Referencia"/>.
/// En la aplicación, las transacciones NO se crean llamando a Construir()
/// directamente desde los servicios: se delega a los "creadores" del
/// patrón Factory Method (capa de Aplicación).
/// </summary>
public class Transaccion
{
    public Guid Id { get; private set; }
    public Guid BilleteraId { get; private set; }
    public TipoTransaccion Tipo { get; private set; }
    public decimal Monto { get; private set; }
    public decimal Comision { get; private set; }
    public decimal SaldoResultante { get; private set; }
    public Guid? BilleteraContraparteId { get; private set; }
    public string Descripcion { get; private set; } = string.Empty;
    public Guid Referencia { get; private set; }
    public DateTime Fecha { get; private set; }

    public Billetera? BilleteraContraparte { get; private set; }

    private Transaccion() { }

    public static Transaccion Construir(Guid billeteraId, TipoTransaccion tipo, decimal monto,
        decimal comision, decimal saldoResultante, Guid? contraparteId,
        string descripcion, Guid referencia)
    {
        return new Transaccion
        {
            Id = Guid.NewGuid(),
            BilleteraId = billeteraId,
            Tipo = tipo,
            Monto = monto,
            Comision = comision,
            SaldoResultante = saldoResultante,
            BilleteraContraparteId = contraparteId,
            Descripcion = descripcion,
            Referencia = referencia,
            Fecha = DateTime.UtcNow
        };
    }
}
