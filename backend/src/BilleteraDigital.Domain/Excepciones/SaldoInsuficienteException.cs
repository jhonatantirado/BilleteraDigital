namespace BilleteraDigital.Domain.Excepciones;

public class SaldoInsuficienteException : ReglaDeNegocioException
{
    public SaldoInsuficienteException(decimal saldo, decimal requerido)
        : base($"Saldo insuficiente. Disponible: S/ {saldo:N2}, requerido: S/ {requerido:N2}.") { }
}
