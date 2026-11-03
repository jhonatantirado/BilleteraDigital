using BilleteraDigital.Application.Patrones.FactoryMethod;
using BilleteraDigital.Application.Patrones.Observer;
using BilleteraDigital.Application.Patrones.Strategy;
using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Enums;
using BilleteraDigital.Domain.Excepciones;
using Xunit;

namespace BilleteraDigital.Tests;

/// <summary>
/// Pruebas unitarias: como los patrones dependen de abstracciones,
/// se prueban SIN base de datos ni servidor web.
/// </summary>
public class StrategyTests
{
    private static readonly DateTime Hoy = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    // Se registran desordenadas a propósito: la calculadora las ordena por prioridad
    private readonly CalculadoraComision _calc = new(new IComisionStrategy[]
    {
        new TarifaPlanaStrategy(), new SinComisionStrategy(), new ComisionPorcentualStrategy(),
        new ClienteAntiguoSinComisionStrategy()
    });

    /// <summary>Contexto de un cliente registrado hace <paramref name="dias"/> días.</summary>
    private static ContextoComision Contexto(decimal monto, int dias = 10) =>
        new(monto, Hoy.AddDays(-dias), Hoy);

    [Theory]
    [InlineData(100, 0, "Sin comisión (hasta S/ 500)")]
    [InlineData(500, 0, "Sin comisión (hasta S/ 500)")]
    [InlineData(1000, 5, "Porcentual 0.5 %")]
    [InlineData(5000, 25, "Porcentual 0.5 %")]
    [InlineData(8000, 25, "Tarifa plana S/ 25")]
    public void Selecciona_la_estrategia_correcta(decimal monto, decimal esperada, string nombre)
    {
        var (comision, estrategia) = _calc.Calcular(Contexto(monto));
        Assert.Equal(esperada, comision);
        Assert.Equal(nombre, estrategia);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(8000)]
    public void Cliente_con_mas_de_6_meses_no_paga_comision(decimal monto)
    {
        var contexto = new ContextoComision(monto, Hoy.AddMonths(-6).AddDays(-1), Hoy);

        var (comision, estrategia) = _calc.Calcular(contexto);

        Assert.Equal(0m, comision);
        Assert.Equal("Cliente con más de 6 meses: sin comisión", estrategia);
    }

    [Fact]
    public void Cliente_con_exactamente_6_meses_aun_paga_comision()
    {
        // "Más de 6 meses": al cumplir justo 6 meses todavía no aplica el beneficio
        var contexto = new ContextoComision(8000m, Hoy.AddMonths(-6), Hoy);

        var (comision, estrategia) = _calc.Calcular(contexto);

        Assert.Equal(25m, comision);
        Assert.Equal("Tarifa plana S/ 25", estrategia);
    }

    [Fact]
    public void Cliente_nuevo_paga_segun_el_monto()
    {
        var (comision, _) = _calc.Calcular(Contexto(1000m, dias: 150));
        Assert.Equal(5m, comision);
    }
}

public class FactoryMethodTests
{
    private readonly ProveedorCreadores _proveedor = new(new CreadorTransaccion[]
    {
        new CreadorRecarga(), new CreadorTransferenciaEnviada(), new CreadorTransferenciaRecibida()
    });

    [Fact]
    public void Cada_creador_produce_su_tipo_de_transaccion()
    {
        var origen = Billetera.Abrir(Guid.NewGuid(), "1900000001");
        var destino = Billetera.Abrir(Guid.NewGuid(), "1900000002");
        var referencia = Guid.NewGuid();

        var enviada = _proveedor.Para(TipoTransaccion.TransferenciaEnviada)
            .Registrar(new DatosTransaccion(origen, 100m, 0m, referencia, destino));
        var recibida = _proveedor.Para(TipoTransaccion.TransferenciaRecibida)
            .Registrar(new DatosTransaccion(destino, 100m, 3m, referencia, origen));

        Assert.Equal(TipoTransaccion.TransferenciaEnviada, enviada.Tipo);
        Assert.Equal(destino.Id, enviada.BilleteraContraparteId);
        Assert.Equal(TipoTransaccion.TransferenciaRecibida, recibida.Tipo);
        Assert.Equal(0m, recibida.Comision);               // el receptor nunca paga comisión
        Assert.Equal(enviada.Referencia, recibida.Referencia);
    }

    [Fact]
    public void Transferencia_sin_contraparte_es_rechazada()
    {
        var b = Billetera.Abrir(Guid.NewGuid(), "1900000001");
        Assert.Throws<ArgumentException>(() =>
            _proveedor.Para(TipoTransaccion.TransferenciaEnviada)
                .Registrar(new DatosTransaccion(b, 10m, 0m, Guid.NewGuid())));
    }
}

public class ObserverTests
{
    private class ObservadorEspia : ITransaccionObserver
    {
        public List<TransaccionEvento> Recibidos { get; } = new();
        public string Nombre => "Espía";
        public Task ActualizarAsync(TransaccionEvento e) { Recibidos.Add(e); return Task.CompletedTask; }
    }

    private class ObservadorRoto : ITransaccionObserver
    {
        public string Nombre => "Roto";
        public Task ActualizarAsync(TransaccionEvento e) => throw new InvalidOperationException("falla");
    }

    private static TransaccionEvento Evento() => new(TipoTransaccion.Recarga, Guid.NewGuid(),
        "1900000001", "a@a.pe", null, null, 50m, 0m, 50m, DateTime.UtcNow, Guid.NewGuid(), null);

    [Fact]
    public async Task Notifica_a_todos_los_suscritos_aunque_uno_falle()
    {
        var subject = new TransaccionSubject();
        var espia = new ObservadorEspia();
        subject.Suscribir(new ObservadorRoto());
        subject.Suscribir(espia);

        await subject.NotificarAsync(Evento());

        Assert.Single(espia.Recibidos);
    }

    [Fact]
    public async Task Un_observador_desuscrito_no_recibe_eventos()
    {
        var subject = new TransaccionSubject();
        var espia = new ObservadorEspia();
        subject.Suscribir(espia);
        subject.Desuscribir(espia);

        await subject.NotificarAsync(Evento());

        Assert.Empty(espia.Recibidos);
    }
}

public class DominioTests
{
    [Fact]
    public void No_se_puede_debitar_mas_que_el_saldo()
    {
        var b = Billetera.Abrir(Guid.NewGuid(), "1900000001");
        b.Acreditar(100m);
        Assert.Throws<SaldoInsuficienteException>(() => b.Debitar(100.01m));
        Assert.Equal(100m, b.Saldo); // el saldo no cambió
    }

    [Fact]
    public void Dni_invalido_es_rechazado()
    {
        Assert.Throws<ReglaDeNegocioException>(() =>
            Cliente.Crear("Ana", "Q", "123", "a@a.pe", "", "hash"));
    }
}
