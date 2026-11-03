using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Enums;

namespace BilleteraDigital.Application.Patrones.FactoryMethod;

/// <summary>
/// PATRÓN FACTORY METHOD (creacional - GoF)
/// "Define una interfaz para crear un objeto, pero deja que las subclases
///  decidan qué clase/configuración instanciar."
///
/// - Creator (esta clase abstracta) declara el factory method <see cref="CrearTransaccion"/>
///   y una operación <see cref="Registrar"/> que lo utiliza.
/// - Cada ConcreteCreator (Recarga, Enviada, Recibida) sobreescribe el factory
///   method y decide cómo se arma su transacción (signo, contraparte, descripción).
/// El servicio solo pide: "creador de TransferenciaEnviada, registra esto".
/// </summary>
public abstract class CreadorTransaccion
{
    public abstract TipoTransaccion Tipo { get; }

    /// <summary>FACTORY METHOD: cada subclase decide cómo construir el producto.</summary>
    protected abstract Transaccion CrearTransaccion(DatosTransaccion datos);

    /// <summary>
    /// Operación del Creator que usa el factory method: aplica validaciones
    /// comunes a TODAS las transacciones y luego delega la creación.
    /// </summary>
    public Transaccion Registrar(DatosTransaccion datos)
    {
        if (datos.Monto <= 0)
            throw new ArgumentException("El monto de la transacción debe ser positivo.");
        if (datos.Comision < 0)
            throw new ArgumentException("La comisión no puede ser negativa.");

        return CrearTransaccion(datos);
    }

    protected static string Limpiar(string? descripcion, string porDefecto) =>
        string.IsNullOrWhiteSpace(descripcion) ? porDefecto : descripcion.Trim()[..Math.Min(descripcion.Trim().Length, 140)];
}

/// <summary>ConcreteCreator: recarga de saldo (sin contraparte, sin comisión).</summary>
public sealed class CreadorRecarga : CreadorTransaccion
{
    public override TipoTransaccion Tipo => TipoTransaccion.Recarga;

    protected override Transaccion CrearTransaccion(DatosTransaccion d) =>
        Transaccion.Construir(
            billeteraId: d.Billetera.Id,
            tipo: Tipo,
            monto: d.Monto,
            comision: 0m,
            saldoResultante: d.Billetera.Saldo,
            contraparteId: null,
            descripcion: Limpiar(d.Descripcion, "Recarga de saldo"),
            referencia: d.Referencia);
}

/// <summary>ConcreteCreator: salida de dinero del emisor (registra la comisión cobrada).</summary>
public sealed class CreadorTransferenciaEnviada : CreadorTransaccion
{
    public override TipoTransaccion Tipo => TipoTransaccion.TransferenciaEnviada;

    protected override Transaccion CrearTransaccion(DatosTransaccion d)
    {
        if (d.Contraparte is null)
            throw new ArgumentException("Una transferencia enviada requiere billetera destino.");

        return Transaccion.Construir(d.Billetera.Id, Tipo, d.Monto, d.Comision, d.Billetera.Saldo,
            d.Contraparte.Id,
            Limpiar(d.Descripcion, $"Transferencia a cuenta {d.Contraparte.NumeroCuenta}"),
            d.Referencia);
    }
}

/// <summary>ConcreteCreator: entrada de dinero al receptor (el receptor nunca paga comisión).</summary>
public sealed class CreadorTransferenciaRecibida : CreadorTransaccion
{
    public override TipoTransaccion Tipo => TipoTransaccion.TransferenciaRecibida;

    protected override Transaccion CrearTransaccion(DatosTransaccion d)
    {
        if (d.Contraparte is null)
            throw new ArgumentException("Una transferencia recibida requiere billetera origen.");

        return Transaccion.Construir(d.Billetera.Id, Tipo, d.Monto, 0m, d.Billetera.Saldo,
            d.Contraparte.Id,
            Limpiar(d.Descripcion, $"Transferencia de cuenta {d.Contraparte.NumeroCuenta}"),
            d.Referencia);
    }
}

/// <summary>
/// ConcreteCreator: salida de dinero hacia fuera del sistema (cuenta bancaria, cajero).
/// Como la recarga, no tiene contraparte; a diferencia de ella, el saldo se DEBITA.
/// Agregar este tipo no modificó la clase base ni <see cref="ProveedorCreadores"/>.
/// </summary>
public sealed class CreadorRetiro : CreadorTransaccion
{
    public override TipoTransaccion Tipo => TipoTransaccion.Retiro;

    protected override Transaccion CrearTransaccion(DatosTransaccion d)
    {
        if (d.Contraparte is not null)
            throw new ArgumentException("Un retiro no tiene billetera contraparte.");

        return Transaccion.Construir(
            billeteraId: d.Billetera.Id,
            tipo: Tipo,
            monto: d.Monto,
            comision: d.Comision,
            saldoResultante: d.Billetera.Saldo,
            contraparteId: null,
            descripcion: Limpiar(d.Descripcion, "Retiro de saldo"),
            referencia: d.Referencia);
    }
}
