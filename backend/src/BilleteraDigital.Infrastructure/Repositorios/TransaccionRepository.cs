using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Enums;
using BilleteraDigital.Domain.Interfaces;
using BilleteraDigital.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BilleteraDigital.Infrastructure.Repositorios;

public class TransaccionRepository : ITransaccionRepository
{
    private readonly BilleteraDbContext _ctx;
    public TransaccionRepository(BilleteraDbContext ctx) => _ctx = ctx;

    public async Task AgregarAsync(Transaccion transaccion) =>
        await _ctx.Transacciones.AddAsync(transaccion);

    public async Task<(IReadOnlyList<Transaccion> Items, int Total)> ObtenerHistorialAsync(
        Guid billeteraId, int pagina, int tamanoPagina, TipoTransaccion? tipo)
    {
        var consulta = _ctx.Transacciones.AsNoTracking().Where(t => t.BilleteraId == billeteraId);
        if (tipo.HasValue) consulta = consulta.Where(t => t.Tipo == tipo.Value);

        var total = await consulta.CountAsync();
        var items = await consulta
            .Include(t => t.BilleteraContraparte!)
                .ThenInclude(b => b.Cliente)
            .OrderByDescending(t => t.Fecha)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return (items, total);
    }
}
