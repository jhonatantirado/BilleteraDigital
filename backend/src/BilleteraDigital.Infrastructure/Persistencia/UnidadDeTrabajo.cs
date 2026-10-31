using BilleteraDigital.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace BilleteraDigital.Infrastructure.Persistencia;

/// <summary>
/// UNIT OF WORK sobre EF Core: todos los repositorios comparten el MISMO
/// DbContext (registrado como Scoped = uno por petición HTTP), por lo que
/// una sola llamada a SaveChanges + COMMIT persiste todo de forma atómica.
/// </summary>
public class UnidadDeTrabajo : IUnitOfWork, IAsyncDisposable
{
    private readonly BilleteraDbContext _contexto;
    private IDbContextTransaction? _transaccion;

    public UnidadDeTrabajo(BilleteraDbContext contexto) => _contexto = contexto;

    public async Task IniciarTransaccionAsync()
    {
        _transaccion ??= await _contexto.Database.BeginTransactionAsync();
    }

    public async Task ConfirmarAsync()
    {
        await _contexto.SaveChangesAsync();
        if (_transaccion is not null)
        {
            await _transaccion.CommitAsync();
            await _transaccion.DisposeAsync();
            _transaccion = null;
        }
    }

    public async Task RevertirAsync()
    {
        if (_transaccion is not null)
        {
            await _transaccion.RollbackAsync();
            await _transaccion.DisposeAsync();
            _transaccion = null;
        }
        _contexto.ChangeTracker.Clear(); // descarta cambios en memoria
    }

    public Task GuardarCambiosAsync() => _contexto.SaveChangesAsync();

    public async ValueTask DisposeAsync()
    {
        if (_transaccion is not null) await _transaccion.DisposeAsync();
    }
}
