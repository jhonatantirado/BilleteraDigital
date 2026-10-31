using BilleteraDigital.Domain.Entidades;

namespace BilleteraDigital.Application.Patrones.FactoryMethod;

/// <summary>Parámetros comunes que necesitan todos los creadores.</summary>
public record DatosTransaccion(
    Billetera Billetera,          // billetera cuyo historial recibe el movimiento
    decimal Monto,
    decimal Comision,
    Guid Referencia,
    Billetera? Contraparte = null, // la otra billetera en una transferencia
    string? Descripcion = null);
