using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Interfaces;
using BilleteraDigital.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BilleteraDigital.Infrastructure.Repositorios;

public class BilleteraRepository : IBilleteraRepository
{
    private readonly BilleteraDbContext _ctx;
    public BilleteraRepository(BilleteraDbContext ctx) => _ctx = ctx;

    // Las consultas de solo lectura usan AsNoTracking: más rápidas y evitan que
    // EF devuelva una copia "vieja" en memoria cuando luego se pide la fila bloqueada.
    public Task<Billetera?> ObtenerPorIdAsync(Guid id) =>
        _ctx.Billeteras.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);

    public Task<Billetera?> ObtenerPorClienteIdAsync(Guid clienteId) =>
        _ctx.Billeteras.AsNoTracking().FirstOrDefaultAsync(b => b.ClienteId == clienteId);

    public Task<Billetera?> ObtenerPorNumeroCuentaAsync(string numeroCuenta) =>
        _ctx.Billeteras.AsNoTracking().FirstOrDefaultAsync(b => b.NumeroCuenta == numeroCuenta);

    public Task<Billetera?> ObtenerPorTelefonoAsync(string telefono) =>
        _ctx.Billeteras.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Cliente!.Telefono == telefono && b.Cliente.TelefonoVerificado);

    public async Task<Billetera?> ObtenerPorIdParaActualizarAsync(Guid id)
    {
        // Bloqueo pesimista a nivel de fila en PostgreSQL.
        // La fila queda bloqueada hasta el COMMIT/ROLLBACK de la Unidad de Trabajo.
        var filas = await _ctx.Billeteras
            .FromSqlInterpolated($"SELECT * FROM billeteras WHERE id = {id} FOR UPDATE")
            .ToListAsync();
        return filas.FirstOrDefault();
    }

    public Task<bool> ExisteNumeroCuentaAsync(string numeroCuenta) =>
        _ctx.Billeteras.AnyAsync(b => b.NumeroCuenta == numeroCuenta);

    public async Task AgregarAsync(Billetera billetera) => await _ctx.Billeteras.AddAsync(billetera);
}
