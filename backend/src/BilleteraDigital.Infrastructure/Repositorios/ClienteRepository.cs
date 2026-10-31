using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Interfaces;
using BilleteraDigital.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BilleteraDigital.Infrastructure.Repositorios;

/// <summary>PATRÓN REPOSITORY — implementación concreta con EF Core + PostgreSQL.</summary>
public class ClienteRepository : IClienteRepository
{
    private readonly BilleteraDbContext _ctx;
    public ClienteRepository(BilleteraDbContext ctx) => _ctx = ctx;

    public Task<Cliente?> ObtenerPorIdAsync(Guid id) =>
        _ctx.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    public Task<Cliente?> ObtenerPorEmailAsync(string email) =>
        _ctx.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Email == email);

    public Task<bool> ExisteDniAsync(string dni) => _ctx.Clientes.AnyAsync(c => c.Dni == dni);

    public Task<bool> ExisteEmailAsync(string email) => _ctx.Clientes.AnyAsync(c => c.Email == email);

    public async Task AgregarAsync(Cliente cliente) => await _ctx.Clientes.AddAsync(cliente);
}
