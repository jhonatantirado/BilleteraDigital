using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Interfaces;
using BilleteraDigital.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BilleteraDigital.IntegrationTests;

/// <summary>
/// SOLO PARA LA DEMOSTRACIÓN (Decorator): igual al repositorio real, pero
/// <see cref="ObtenerPorIdParaActualizarAsync"/> hace un SELECT normal, SIN
/// "FOR UPDATE", y espera un momento antes de devolver la fila. La pausa agranda
/// la ventana entre "leer el saldo" y "escribir el saldo", para que la carrera
/// se reproduzca de forma confiable en cada ejecución.
/// El código de producción no se modifica.
/// </summary>
public class BilleteraRepositorioSinBloqueo : IBilleteraRepository
{
    public static readonly TimeSpan Pausa = TimeSpan.FromMilliseconds(20);

    private readonly BilleteraDbContext _ctx;
    private readonly IBilleteraRepository _real;

    public BilleteraRepositorioSinBloqueo(BilleteraDbContext ctx, IBilleteraRepository real)
    {
        _ctx = ctx;
        _real = real;
    }

    public async Task<Billetera?> ObtenerPorIdParaActualizarAsync(Guid id)
    {
        var billetera = await _ctx.Billeteras.FirstOrDefaultAsync(b => b.Id == id); // con tracking, SIN bloqueo
        await Task.Delay(Pausa);
        return billetera;
    }

    public Task<Billetera?> ObtenerPorIdAsync(Guid id) => _real.ObtenerPorIdAsync(id);
    public Task<Billetera?> ObtenerPorClienteIdAsync(Guid clienteId) => _real.ObtenerPorClienteIdAsync(clienteId);
    public Task<Billetera?> ObtenerPorNumeroCuentaAsync(string numeroCuenta) => _real.ObtenerPorNumeroCuentaAsync(numeroCuenta);
    public Task<bool> ExisteNumeroCuentaAsync(string numeroCuenta) => _real.ExisteNumeroCuentaAsync(numeroCuenta);
    public Task AgregarAsync(Billetera billetera) => _real.AgregarAsync(billetera);
}
