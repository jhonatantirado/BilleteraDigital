using BilleteraDigital.Application.Patrones.FactoryMethod;
using BilleteraDigital.Application.Patrones.Observer;
using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Enums;
using BilleteraDigital.Domain.Interfaces;
using BilleteraDigital.Infrastructure.Observadores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BilleteraDigital.Tests;

/// <summary>Ejercicio 2 (parte A): el nuevo tipo Retiro con su creador (Factory Method).</summary>
public class RetiroTests
{
    private readonly ProveedorCreadores _proveedor = new(new CreadorTransaccion[]
    {
        new CreadorRecarga(), new CreadorTransferenciaEnviada(), new CreadorTransferenciaRecibida(),
        new CreadorRetiro()
    });

    [Fact]
    public void El_proveedor_entrega_el_creador_de_retiros_sin_haber_sido_modificado()
    {
        Assert.Equal(TipoTransaccion.Retiro, _proveedor.Para(TipoTransaccion.Retiro).Tipo);
    }

    [Fact]
    public void El_retiro_no_tiene_contraparte_y_usa_la_descripcion_por_defecto()
    {
        var b = Billetera.Abrir(Guid.NewGuid(), "1900000001");
        b.Acreditar(500m);
        b.Debitar(200m);

        var t = _proveedor.Para(TipoTransaccion.Retiro)
            .Registrar(new DatosTransaccion(b, 200m, 0m, Guid.NewGuid()));

        Assert.Equal(TipoTransaccion.Retiro, t.Tipo);
        Assert.Null(t.BilleteraContraparteId);
        Assert.Equal("Retiro de saldo", t.Descripcion);
        Assert.Equal(300m, t.SaldoResultante);
    }

    [Fact]
    public void Un_retiro_con_contraparte_es_rechazado()
    {
        var origen = Billetera.Abrir(Guid.NewGuid(), "1900000001");
        var otra = Billetera.Abrir(Guid.NewGuid(), "1900000002");
        Assert.Throws<ArgumentException>(() =>
            _proveedor.Para(TipoTransaccion.Retiro)
                .Registrar(new DatosTransaccion(origen, 10m, 0m, Guid.NewGuid(), otra)));
    }
}

/// <summary>Ejercicio 2 (parte B): regla de puntos (Dominio) y PuntosFidelidadObserver.</summary>
public class PuntosFidelidadTests
{
    [Theory]
    [InlineData(TipoTransaccion.TransferenciaEnviada, 1000, 100)]
    [InlineData(TipoTransaccion.Recarga, 55.90, 5)]
    [InlineData(TipoTransaccion.Recarga, 9.99, 0)]
    [InlineData(TipoTransaccion.Retiro, 1000, 0)]
    [InlineData(TipoTransaccion.TransferenciaRecibida, 1000, 0)]
    public void Calcula_los_puntos_segun_el_tipo_y_el_monto(TipoTransaccion tipo, decimal monto, int esperados)
    {
        Assert.Equal(esperados, MovimientoPuntos.Calcular(tipo, monto));
    }

    [Fact]
    public void Una_operacion_que_no_suma_no_crea_registro()
    {
        Assert.Null(MovimientoPuntos.Otorgar(Guid.NewGuid(), Guid.NewGuid(), TipoTransaccion.Retiro, 500m, DateTime.UtcNow));
    }

    // ------------------------------------------------ dobles de prueba (en memoria)
    private class BilleterasFalsas : IBilleteraRepository
    {
        public List<Billetera> Datos { get; } = new();
        public Task<Billetera?> ObtenerPorIdAsync(Guid id) => Task.FromResult(Datos.FirstOrDefault(b => b.Id == id));
        public Task<Billetera?> ObtenerPorClienteIdAsync(Guid c) => Task.FromResult(Datos.FirstOrDefault(b => b.ClienteId == c));
        public Task<Billetera?> ObtenerPorNumeroCuentaAsync(string n) => Task.FromResult(Datos.FirstOrDefault(b => b.NumeroCuenta == n));
        public Task<Billetera?> ObtenerPorIdParaActualizarAsync(Guid id) => ObtenerPorIdAsync(id);
        public Task<bool> ExisteNumeroCuentaAsync(string n) => Task.FromResult(Datos.Any(b => b.NumeroCuenta == n));
        public Task AgregarAsync(Billetera b) { Datos.Add(b); return Task.CompletedTask; }
    }

    /// <summary>Simula la tabla: lo agregado solo "existe" después de GuardarCambios.</summary>
    private class PuntosFalsos : IPuntosFidelidadRepository
    {
        public List<MovimientoPuntos> Guardados { get; } = new();
        public List<MovimientoPuntos> Pendientes { get; } = new();
        public Task<bool> ExisteReferenciaAsync(Guid r) => Task.FromResult(Guardados.Any(p => p.Referencia == r));
        public Task AgregarAsync(MovimientoPuntos m) { Pendientes.Add(m); return Task.CompletedTask; }
        public Task<int> ObtenerTotalAsync(Guid c) => Task.FromResult(Guardados.Where(p => p.ClienteId == c).Sum(p => p.Puntos));
        public Task<IReadOnlyList<MovimientoPuntos>> ObtenerUltimosAsync(Guid c, int n) =>
            Task.FromResult<IReadOnlyList<MovimientoPuntos>>(Guardados.Where(p => p.ClienteId == c).Take(n).ToList());
    }

    private class UnidadFalsa : IUnitOfWork
    {
        private readonly PuntosFalsos _puntos;
        public UnidadFalsa(PuntosFalsos puntos) => _puntos = puntos;
        public Task IniciarTransaccionAsync() => Task.CompletedTask;
        public Task ConfirmarAsync() => GuardarCambiosAsync();
        public Task RevertirAsync() { _puntos.Pendientes.Clear(); return Task.CompletedTask; }
        public Task GuardarCambiosAsync()
        {
            _puntos.Guardados.AddRange(_puntos.Pendientes);
            _puntos.Pendientes.Clear();
            return Task.CompletedTask;
        }
    }

    private readonly BilleterasFalsas _billeteras = new();
    private readonly PuntosFalsos _puntos = new();
    private readonly PuntosFidelidadObserver _observador;
    private readonly Billetera _billetera = Billetera.Abrir(Guid.NewGuid(), "1900000001");

    public PuntosFidelidadTests()
    {
        _billeteras.Datos.Add(_billetera);

        // Mismo mecanismo que en la API: el observador crea un scope por evento
        var servicios = new ServiceCollection();
        servicios.AddScoped<IBilleteraRepository>(_ => _billeteras);
        servicios.AddScoped<IPuntosFidelidadRepository>(_ => _puntos);
        servicios.AddScoped<IUnitOfWork>(_ => new UnidadFalsa(_puntos));
        var proveedor = servicios.BuildServiceProvider();

        _observador = new PuntosFidelidadObserver(
            proveedor.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<PuntosFidelidadObserver>.Instance);
    }

    private TransaccionEvento Evento(TipoTransaccion tipo, decimal monto, Guid? referencia = null) =>
        new(tipo, referencia ?? Guid.NewGuid(), "1900000001", "ana@demo.pe", null, null,
            monto, 0m, 0m, DateTime.UtcNow, _billetera.Id, null);

    [Fact]
    public async Task Guarda_los_puntos_del_titular_de_la_billetera()
    {
        await _observador.ActualizarAsync(Evento(TipoTransaccion.TransferenciaEnviada, 1000m));

        var registro = Assert.Single(_puntos.Guardados);
        Assert.Equal(_billetera.ClienteId, registro.ClienteId);
        Assert.Equal(100, registro.Puntos);
    }

    [Fact]
    public async Task El_mismo_evento_procesado_dos_veces_no_duplica_puntos()
    {
        var referencia = Guid.NewGuid();
        await _observador.ActualizarAsync(Evento(TipoTransaccion.Recarga, 500m, referencia));
        await _observador.ActualizarAsync(Evento(TipoTransaccion.Recarga, 500m, referencia));

        Assert.Equal(50, await _puntos.ObtenerTotalAsync(_billetera.ClienteId));
    }

    [Fact]
    public async Task Un_retiro_no_otorga_puntos()
    {
        await _observador.ActualizarAsync(Evento(TipoTransaccion.Retiro, 1000m));
        Assert.Empty(_puntos.Guardados);
    }
}
