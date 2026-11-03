using BilleteraDigital.Domain.Enums;

namespace BilleteraDigital.Domain.Entidades;

/// <summary>
/// Puntos de fidelidad otorgados a un cliente por UNA operación.
/// Se guarda un registro por operación (no un total acumulado) para saber
/// siempre por qué se dio cada punto; el total es la suma de los registros.
/// La regla de negocio (cuántos puntos vale cada operación) vive aquí, en el Dominio.
/// </summary>
public class MovimientoPuntos
{
    /// <summary>Se otorga 1 punto por cada S/ 10 (se descartan los decimales).</summary>
    public const decimal SolesPorPunto = 10m;

    public Guid Id { get; private set; }
    public Guid ClienteId { get; private set; }

    /// <summary>Referencia de la operación que originó los puntos (única: evita duplicados).</summary>
    public Guid Referencia { get; private set; }

    public TipoTransaccion TipoOperacion { get; private set; }
    public decimal Monto { get; private set; }
    public int Puntos { get; private set; }
    public DateTime Fecha { get; private set; }

    private MovimientoPuntos() { }

    /// <summary>
    /// Regla: suman las recargas y las transferencias ENVIADAS.
    /// Los retiros y las transferencias recibidas no suman puntos.
    /// </summary>
    public static int Calcular(TipoTransaccion tipo, decimal monto) => tipo switch
    {
        TipoTransaccion.Recarga or TipoTransaccion.TransferenciaEnviada
            => monto <= 0 ? 0 : (int)Math.Floor(monto / SolesPorPunto),
        _ => 0
    };

    /// <summary>Crea el registro de puntos, o null si la operación no otorga puntos.</summary>
    public static MovimientoPuntos? Otorgar(Guid clienteId, Guid referencia, TipoTransaccion tipo,
                                            decimal monto, DateTime fecha)
    {
        var puntos = Calcular(tipo, monto);
        if (puntos == 0) return null;

        return new MovimientoPuntos
        {
            Id = Guid.NewGuid(),
            ClienteId = clienteId,
            Referencia = referencia,
            TipoOperacion = tipo,
            Monto = monto,
            Puntos = puntos,
            Fecha = fecha
        };
    }
}
