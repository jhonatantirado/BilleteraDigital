using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Interfaces;
using BilleteraDigital.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BilleteraDigital.Infrastructure.Repositorios;

public class VerificacionTelefonoRepository : IVerificacionTelefonoRepository
{
    private readonly BilleteraDbContext _ctx;
    public VerificacionTelefonoRepository(BilleteraDbContext ctx) => _ctx = ctx;

    public Task<VerificacionTelefono?> ObtenerAsync(Guid clienteId) =>
        _ctx.VerificacionesTelefono.FirstOrDefaultAsync(v => v.ClienteId == clienteId);   // con tracking

    public async Task AgregarAsync(VerificacionTelefono verificacion) =>
        await _ctx.VerificacionesTelefono.AddAsync(verificacion);

    public void Eliminar(VerificacionTelefono verificacion) => _ctx.VerificacionesTelefono.Remove(verificacion);
}
