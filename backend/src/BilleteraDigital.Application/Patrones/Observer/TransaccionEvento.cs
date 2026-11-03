using BilleteraDigital.Domain.Enums;

namespace BilleteraDigital.Application.Patrones.Observer;

/// <summary>Información que el Subject envía a sus observadores.</summary>
public record TransaccionEvento(
    TipoTransaccion Tipo,
    Guid Referencia,
    string CuentaOrigen,
    string EmailTitular,
    string? CuentaDestino,
    string? EmailDestinatario,
    decimal Monto,
    decimal Comision,
    decimal SaldoResultante,
    DateTime Fecha,
    Guid BilleteraOrigenId,
    Guid? BilleteraDestinoId);
