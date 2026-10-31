namespace BilleteraDigital.Domain.Interfaces;

/// <summary>
/// Complemento del patrón Repository: UNIT OF WORK.
/// Agrupa los cambios de varios repositorios en UNA sola transacción
/// de base de datos: o se guardan todos (débito, crédito y movimientos)
/// o no se guarda ninguno.
/// </summary>
public interface IUnitOfWork
{
    Task IniciarTransaccionAsync();
    Task ConfirmarAsync();      // SaveChanges + COMMIT
    Task RevertirAsync();       // ROLLBACK
    Task GuardarCambiosAsync(); // SaveChanges sin transacción explícita
}
