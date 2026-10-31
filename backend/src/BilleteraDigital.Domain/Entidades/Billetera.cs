using BilleteraDigital.Domain.Excepciones;

namespace BilleteraDigital.Domain.Entidades;

/// <summary>
/// Entidad de dominio "rica": la lógica que protege el saldo vive aquí,
/// no en el controlador ni en el repositorio. Nadie puede modificar
/// el saldo sin pasar por Acreditar/Debitar.
/// </summary>
public class Billetera
{
    public Guid Id { get; private set; }
    public string NumeroCuenta { get; private set; } = string.Empty;
    public Guid ClienteId { get; private set; }
    public decimal Saldo { get; private set; }
    public string Moneda { get; private set; } = "PEN";
    public DateTime FechaCreacion { get; private set; }

    public Cliente? Cliente { get; private set; }

    private Billetera() { }

    public static Billetera Abrir(Guid clienteId, string numeroCuenta)
    {
        if (numeroCuenta.Length != 10 || !numeroCuenta.All(char.IsDigit))
            throw new ReglaDeNegocioException("El número de cuenta debe tener 10 dígitos.");

        return new Billetera
        {
            Id = Guid.NewGuid(),
            ClienteId = clienteId,
            NumeroCuenta = numeroCuenta,
            Saldo = 0m,
            Moneda = "PEN",
            FechaCreacion = DateTime.UtcNow
        };
    }

    public void Acreditar(decimal monto)
    {
        if (monto <= 0) throw new ReglaDeNegocioException("El monto a acreditar debe ser mayor a cero.");
        Saldo += monto;
    }

    public void Debitar(decimal monto)
    {
        if (monto <= 0) throw new ReglaDeNegocioException("El monto a debitar debe ser mayor a cero.");
        if (Saldo < monto) throw new SaldoInsuficienteException(Saldo, monto);
        Saldo -= monto;
    }
}
