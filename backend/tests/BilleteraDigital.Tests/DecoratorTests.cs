using BilleteraDigital.Application.Patrones.Observer;
using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Enums;
using BilleteraDigital.Domain.Interfaces;
using BilleteraDigital.Infrastructure.Cache;
using BilleteraDigital.Infrastructure.Observadores;
using BilleteraDigital.Infrastructure.Repositorios;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BilleteraDigital.Tests;

/// <summary>
/// Pruebas del patrón Decorator: se envuelve un repositorio FALSO que cuenta
/// cuántas veces lo llaman, así se ve cuándo la consulta "llega a la BD".
/// </summary>
public class DecoratorTests
{
    /// <summary>Repositorio en memoria que registra cada llamada.</summary>
    private class RepositorioContador : IBilleteraRepository
    {
        public List<Billetera> Datos { get; } = new();
        public int Lecturas { get; private set; }
        public int LecturasConBloqueo { get; private set; }

        public Task<Billetera?> ObtenerPorIdAsync(Guid id) { Lecturas++; return Task.FromResult(Datos.FirstOrDefault(b => b.Id == id)); }
        public Task<Billetera?> ObtenerPorClienteIdAsync(Guid c) { Lecturas++; return Task.FromResult(Datos.FirstOrDefault(b => b.ClienteId == c)); }
        public Task<Billetera?> ObtenerPorNumeroCuentaAsync(string n) { Lecturas++; return Task.FromResult(Datos.FirstOrDefault(b => b.NumeroCuenta == n)); }
        public Dictionary<string, Guid> Celulares { get; } = new();
        public Task<Billetera?> ObtenerPorTelefonoAsync(string t) { Lecturas++; return Task.FromResult(Celulares.TryGetValue(t, out var id) ? Datos.FirstOrDefault(b => b.Id == id) : null); }
        public Task<Billetera?> ObtenerPorIdParaActualizarAsync(Guid id) { LecturasConBloqueo++; return Task.FromResult(Datos.FirstOrDefault(b => b.Id == id)); }
        public Task<bool> ExisteNumeroCuentaAsync(string n) => Task.FromResult(Datos.Any(b => b.NumeroCuenta == n));
        public Task AgregarAsync(Billetera b) { Datos.Add(b); return Task.CompletedTask; }
    }

    private readonly RepositorioContador _real = new();
    private readonly CacheBilleteras _cache = new(new MemoryCache(new MemoryCacheOptions()));
    private readonly BilleteraRepositoryConCache _decorado;
    private readonly Billetera _billetera = Billetera.Abrir(Guid.NewGuid(), "1900000001");

    public DecoratorTests()
    {
        _decorado = new BilleteraRepositoryConCache(_real, _cache, NullLogger<BilleteraRepositoryConCache>.Instance);
        _real.Datos.Add(_billetera);
    }

    [Fact]
    public async Task La_segunda_consulta_sale_de_la_cache()
    {
        await _decorado.ObtenerPorIdAsync(_billetera.Id);
        var segunda = await _decorado.ObtenerPorIdAsync(_billetera.Id);

        Assert.Equal(1, _real.Lecturas);          // solo la primera llegó a la "BD"
        Assert.Equal(_billetera.Id, segunda!.Id);
    }

    [Fact]
    public async Task Buscar_por_cuenta_o_por_cliente_reutiliza_la_misma_entrada()
    {
        await _decorado.ObtenerPorNumeroCuentaAsync("1900000001");
        await _decorado.ObtenerPorIdAsync(_billetera.Id);
        await _decorado.ObtenerPorClienteIdAsync(_billetera.ClienteId);

        Assert.Equal(1, _real.Lecturas);
    }

    [Fact]
    public async Task Buscar_por_celular_tambien_sale_de_la_cache()
    {
        _real.Celulares["+51987654321"] = _billetera.Id;

        await _decorado.ObtenerPorTelefonoAsync("+51987654321");
        var segunda = await _decorado.ObtenerPorTelefonoAsync("+51987654321");

        Assert.Equal(1, _real.Lecturas);
        Assert.Equal(_billetera.Id, segunda!.Id);
        Assert.Null(await _decorado.ObtenerPorTelefonoAsync("+51900000000")); // un "no existe" no se cachea
    }

    [Fact]
    public async Task La_lectura_con_bloqueo_nunca_usa_la_cache()
    {
        await _decorado.ObtenerPorIdAsync(_billetera.Id);           // queda en caché
        await _decorado.ObtenerPorIdParaActualizarAsync(_billetera.Id);
        await _decorado.ObtenerPorIdParaActualizarAsync(_billetera.Id);

        Assert.Equal(2, _real.LecturasConBloqueo); // ambas fueron a la BD (FOR UPDATE)
    }

    [Fact]
    public async Task No_se_guarda_en_cache_un_no_existe()
    {
        Assert.Null(await _decorado.ObtenerPorNumeroCuentaAsync("1900000099"));

        var nueva = Billetera.Abrir(Guid.NewGuid(), "1900000099");
        await _decorado.AgregarAsync(nueva);

        Assert.NotNull(await _decorado.ObtenerPorNumeroCuentaAsync("1900000099"));
    }

    [Fact]
    public async Task El_observador_invalida_la_cache_despues_de_una_transferencia()
    {
        var destino = Billetera.Abrir(Guid.NewGuid(), "1900000002");
        _real.Datos.Add(destino);
        await _decorado.ObtenerPorIdAsync(_billetera.Id);
        await _decorado.ObtenerPorIdAsync(destino.Id);
        Assert.Equal(2, _real.Lecturas);

        var observador = new CacheBilleterasObserver(_cache);
        await observador.ActualizarAsync(new TransaccionEvento(
            TipoTransaccion.TransferenciaEnviada, Guid.NewGuid(), "1900000001", "a@a.pe",
            "1900000002", "b@b.pe", 10m, 0m, 90m, DateTime.UtcNow, _billetera.Id, destino.Id));

        await _decorado.ObtenerPorIdAsync(_billetera.Id);
        await _decorado.ObtenerPorIdAsync(destino.Id);
        Assert.Equal(4, _real.Lecturas);           // ambas volvieron a la BD: saldo fresco
    }
}
