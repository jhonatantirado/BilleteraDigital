using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Interfaces;
using BilleteraDigital.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BilleteraDigital.Infrastructure.Repositorios;

/// <summary>PATRÓN REPOSITORY — puntos de fidelidad sobre EF Core + PostgreSQL.</summary>
public class PuntosFidelidadRepository : IPuntosFidelidadRepository
{
    private readonly BilleteraDbContext _ctx;
    public PuntosFidelidadRepository(BilleteraDbContext ctx) => _ctx = ctx;

    public Task<bool> ExisteReferenciaAsync(Guid referencia) =>
        _ctx.MovimientosPuntos.AnyAsync(p => p.Referencia == referencia);

    public async Task AgregarAsync(MovimientoPuntos movimiento) =>
        await _ctx.MovimientosPuntos.AddAsync(movimiento);

    public Task<int> ObtenerTotalAsync(Guid clienteId) =>
        _ctx.MovimientosPuntos.Where(p => p.ClienteId == clienteId).SumAsync(p => p.Puntos);

    public async Task<IReadOnlyList<MovimientoPuntos>> ObtenerUltimosAsync(Guid clienteId, int cantidad) =>
        await _ctx.MovimientosPuntos.AsNoTracking()
            .Where(p => p.ClienteId == clienteId)
            .OrderByDescending(p => p.Fecha)
            .Take(cantidad)
            .ToListAsync();
}
