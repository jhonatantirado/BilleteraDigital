using BilleteraDigital.Api.Configuracion;
using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Interfaces;
using BilleteraDigital.Infrastructure;
using BilleteraDigital.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace BilleteraDigital.IntegrationTests;

/// <summary>
/// Levanta UN contenedor PostgreSQL 16 para todas las pruebas de la colección,
/// ejecuta los scripts de database/ (los mismos de docker compose) y arma el
/// contenedor de dependencias con los MISMOS registros que usa la API.
/// </summary>
public class BaseDeDatosFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .Build();

    private int _secuencia;

    /// <summary>Servicios de la aplicación real (con FOR UPDATE).</summary>
    public ServiceProvider Servicios { get; private set; } = null!;

    public string CadenaConexion => _pg.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();

        var carpeta = Path.Combine(AppContext.BaseDirectory, "database");
        foreach (var script in Directory.GetFiles(carpeta, "0*.sql").Order(StringComparer.Ordinal))
        {
            // ON_ERROR_STOP: sin esto psql termina con código 0 aunque falle una sentencia
            var contenido = "\\set ON_ERROR_STOP on\n" + await File.ReadAllTextAsync(script);
            var resultado = await _pg.ExecScriptAsync(contenido);
            if (resultado.ExitCode != 0)
                throw new InvalidOperationException($"Falló {Path.GetFileName(script)}: {resultado.Stderr}");
        }

        Servicios = CrearServicios();
    }

    public async Task DisposeAsync()
    {
        await Servicios.DisposeAsync();
        await _pg.DisposeAsync();
    }

    /// <summary>
    /// Arma el contenedor de dependencias. <paramref name="ajustar"/> permite
    /// reemplazar registros (p. ej. el repositorio SIN bloqueo de la demostración).
    /// No se suscriben los observadores: la prueba mide solo la concurrencia.
    /// </summary>
    public ServiceProvider CrearServicios(Action<IServiceCollection>? ajustar = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = CadenaConexion
            })
            .Build();

        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddSingleton<IConfiguration>(config);
        servicios.AddAplicacion();
        servicios.AddInfraestructura(config);
        ajustar?.Invoke(servicios);
        return servicios.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    /// <summary>Crea un cliente con su billetera y el saldo indicado. Devuelve la billetera.</summary>
    public async Task<Billetera> CrearBilleteraAsync(decimal saldo) =>
        (await CrearClienteConCelularAsync(saldo, celularVerificado: false)).Billetera;

    /// <summary>Igual que <see cref="CrearBilleteraAsync"/>, y devuelve también su celular (9 dígitos).</summary>
    public async Task<(Billetera Billetera, string Celular)> CrearClienteConCelularAsync(
        decimal saldo, bool celularVerificado)
    {
        var n = Interlocked.Increment(ref _secuencia);
        var sufijo = $"{Random.Shared.Next(0, 10_000):D4}{n:D4}";        // 8 dígitos únicos
        var celular = "9" + sufijo;                                     // celular único y válido

        await using var scope = Servicios.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var cliente = Cliente.Crear("Prueba", $"Concurrencia {n}", sufijo, $"prueba{sufijo}@test.pe", celular, "hash");
        if (celularVerificado) cliente.MarcarTelefonoVerificado();
        var billetera = Billetera.Abrir(cliente.Id, "28" + sufijo);
        if (saldo > 0) billetera.Acreditar(saldo);

        await sp.GetRequiredService<IClienteRepository>().AgregarAsync(cliente);
        await sp.GetRequiredService<IBilleteraRepository>().AgregarAsync(billetera);
        await sp.GetRequiredService<IUnitOfWork>().GuardarCambiosAsync();
        return (billetera, celular);
    }

    /// <summary>Saldo leído directamente de la BD (sin caché).</summary>
    public async Task<decimal> SaldoAsync(Guid billeteraId)
    {
        await using var scope = Servicios.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BilleteraDbContext>();
        return await ctx.Billeteras.AsNoTracking()
            .Where(b => b.Id == billeteraId).Select(b => b.Saldo).SingleAsync();
    }

    /// <summary>Cantidad y suma de los movimientos de un tipo en una billetera.</summary>
    public async Task<(int Cantidad, decimal Total)> MovimientosAsync(Guid billeteraId, Domain.Enums.TipoTransaccion tipo)
    {
        await using var scope = Servicios.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BilleteraDbContext>();
        var montos = await ctx.Transacciones.AsNoTracking()
            .Where(t => t.BilleteraId == billeteraId && t.Tipo == tipo)
            .Select(t => t.Monto).ToListAsync();
        return (montos.Count, montos.Sum());
    }
}

[CollectionDefinition(Nombre)]
public class ColeccionPostgres : ICollectionFixture<BaseDeDatosFixture>
{
    public const string Nombre = "PostgreSQL";
}
