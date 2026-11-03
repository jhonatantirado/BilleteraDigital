using BilleteraDigital.Application.DTOs;
using BilleteraDigital.Application.Interfaces;
using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Enums;
using BilleteraDigital.Domain.Excepciones;
using BilleteraDigital.Domain.Interfaces;
using BilleteraDigital.Infrastructure.Persistencia;
using BilleteraDigital.Infrastructure.Repositorios;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace BilleteraDigital.IntegrationTests;

/// <summary>
/// Ejercicio 6: transferencias SIMULTÁNEAS contra PostgreSQL real.
/// Se llama al servicio directamente (sin HTTP): lo que se mide es el
/// comportamiento de los bloqueos de fila, no el filtro de idempotencia.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
[Trait("Categoria", "Integracion")]
public class ConcurrenciaTests
{
    private readonly BaseDeDatosFixture _bd;
    private readonly ITestOutputHelper _salida;

    public ConcurrenciaTests(BaseDeDatosFixture bd, ITestOutputHelper salida)
    {
        _bd = bd;
        _salida = salida;
    }

    /// <summary>
    /// Lanza <paramref name="cantidad"/> operaciones A LA VEZ. Cada una tiene su
    /// propio scope (su propio DbContext y su propia conexión: un DbContext no se
    /// puede usar desde dos hilos) y todas esperan la misma "señal de salida".
    /// Devuelve la excepción de cada operación, o null si terminó bien.
    /// </summary>
    private static async Task<List<Exception?>> LanzarSimultaneas(
        ServiceProvider servicios, int cantidad, Func<int, IBilleteraService, Task> operacion)
    {
        var salida = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var listas = new CountdownEvent(cantidad);

        var tareas = Enumerable.Range(0, cantidad).Select(i => Task.Run(async () =>
        {
            await using var scope = servicios.CreateAsyncScope();
            var servicio = scope.ServiceProvider.GetRequiredService<IBilleteraService>();
            listas.Signal();
            await salida.Task;                       // todas esperan aquí…
            try
            {
                await operacion(i, servicio);
                return (Exception?)null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        })).ToList();

        listas.Wait(TimeSpan.FromSeconds(30));
        salida.SetResult();                          // …y salen juntas
        return (await Task.WhenAll(tareas)).ToList();
    }

    private static TransferenciaRequest Transferencia(Billetera origen, Billetera destino, decimal monto) =>
        new(origen.Id, destino.NumeroCuenta, monto, "Prueba de concurrencia");

    private void Informar(string titulo, List<Exception?> errores)
    {
        _salida.WriteLine($"--- {titulo}");
        _salida.WriteLine($"Exitosas: {errores.Count(e => e is null)}");
        foreach (var grupo in errores.Where(e => e is not null).GroupBy(e => e!.GetType().Name))
            _salida.WriteLine($"{grupo.Key}: {grupo.Count()}  (p. ej. \"{grupo.First()!.Message}\")");
    }

    // ===================================================================== ESCENARIO 1
    [Fact]
    public async Task Cincuenta_transferencias_simultaneas_no_dejan_saldo_negativo_ni_crean_dinero()
    {
        // Ana tiene dinero para EXACTAMENTE 10 transferencias de S/ 100 (sin comisión: ≤ S/ 500)
        var ana = await _bd.CrearBilleteraAsync(1000m);
        var luis = await _bd.CrearBilleteraAsync(0m);

        var errores = await LanzarSimultaneas(_bd.Servicios, 50,
            (_, s) => s.TransferirAsync(Transferencia(ana, luis, 100m)));
        Informar("Con FOR UPDATE (código real)", errores);

        var saldoAna = await _bd.SaldoAsync(ana.Id);
        var saldoLuis = await _bd.SaldoAsync(luis.Id);
        _salida.WriteLine($"Saldo Ana: {saldoAna}  Saldo Luis: {saldoLuis}  Suma: {saldoAna + saldoLuis}");

        Assert.Equal(10, errores.Count(e => e is null));
        Assert.Equal(40, errores.Count(e => e is SaldoInsuficienteException));
        Assert.Equal(50, errores.Count(e => e is null or SaldoInsuficienteException)); // ningún otro error

        Assert.Equal(0m, saldoAna);
        Assert.Equal(1000m, saldoLuis);
        Assert.Equal(1000m, saldoAna + saldoLuis);   // CONSERVACIÓN: ni se crea ni se pierde dinero

        // El historial cuadra con los saldos
        var enviadas = await _bd.MovimientosAsync(ana.Id, TipoTransaccion.TransferenciaEnviada);
        var recibidas = await _bd.MovimientosAsync(luis.Id, TipoTransaccion.TransferenciaRecibida);
        Assert.Equal((10, 1000m), enviadas);
        Assert.Equal((10, 1000m), recibidas);
    }

    // ===================================================================== ESCENARIO 2
    [Fact]
    public async Task Transferencias_cruzadas_simultaneas_no_producen_interbloqueos()
    {
        // 25 transferencias A→B y 25 B→A de S/ 40: a nadie le falta saldo (25 × 40 = 1000)
        var ana = await _bd.CrearBilleteraAsync(1000m);
        var luis = await _bd.CrearBilleteraAsync(1000m);

        var errores = await LanzarSimultaneas(_bd.Servicios, 50, (i, s) => i % 2 == 0
            ? s.TransferirAsync(Transferencia(ana, luis, 40m))
            : s.TransferirAsync(Transferencia(luis, ana, 40m)));
        Informar("Transferencias cruzadas A↔B", errores);

        // Si BilleteraService no bloqueara las filas SIEMPRE en el mismo orden (por Id),
        // aquí aparecerían errores 40P01 "deadlock detected": al reproducirlo en
        // PostgreSQL 16 sin ese orden, entre 46 y 49 de las 50 transferencias fallaron así.
        Assert.All(errores, e => Assert.Null(e));
        Assert.Equal(1000m, await _bd.SaldoAsync(ana.Id));
        Assert.Equal(1000m, await _bd.SaldoAsync(luis.Id));
    }

    // ===================================================================== ESCENARIO 3
    /// <summary>
    /// DEMOSTRACIÓN: misma prueba que el escenario 1, pero SIN "FOR UPDATE".
    /// Se espera que el invariante SE ROMPA (actualización perdida / lost update):
    /// dos transacciones leen el mismo saldo, ambas aprueban el débito y la segunda
    /// escritura pisa a la primera.
    ///
    /// Resultado observado al reproducir estas sentencias en PostgreSQL 16 (3 corridas):
    /// las 50 transferencias "exitosas", 50 movimientos por S/ 5,000 en el historial,
    /// y aun así Ana termina con S/ 900 y Luis con S/ 100. La SUMA sigue dando S/ 1,000
    /// (las dos cuentas pierden las mismas actualizaciones): por eso la conservación
    /// sola no basta, y lo que delata el error es el historial.
    /// Excluir de CI: dotnet test --filter "Categoria!=Demostracion"
    /// </summary>
    [Fact]
    [Trait("Categoria", "Demostracion")]
    public async Task Sin_FOR_UPDATE_se_pierden_actualizaciones()
    {
        var ana = await _bd.CrearBilleteraAsync(1000m);
        var luis = await _bd.CrearBilleteraAsync(0m);

        await using var sinBloqueo = _bd.CrearServicios(s =>
            s.AddScoped<IBilleteraRepository>(sp => new BilleteraRepositorioSinBloqueo(
                sp.GetRequiredService<BilleteraDbContext>(),
                sp.GetRequiredService<BilleteraRepository>())));   // el último registro gana

        var errores = await LanzarSimultaneas(sinBloqueo, 50,
            (_, s) => s.TransferirAsync(Transferencia(ana, luis, 100m)));
        Informar("SIN FOR UPDATE (demostración)", errores);

        var exitosas = errores.Count(e => e is null);
        var saldoAna = await _bd.SaldoAsync(ana.Id);
        var saldoLuis = await _bd.SaldoAsync(luis.Id);
        var enviadas = await _bd.MovimientosAsync(ana.Id, TipoTransaccion.TransferenciaEnviada);

        _salida.WriteLine($"Saldo Ana: {saldoAna}  Saldo Luis: {saldoLuis}  Suma: {saldoAna + saldoLuis}");
        _salida.WriteLine($"Movimientos enviados registrados: {enviadas.Cantidad} por S/ {enviadas.Total}");
        _salida.WriteLine("Con FOR UPDATE serían 10 exitosas, Ana en S/ 0, Luis en S/ 1000 y suma S/ 1000.");

        // Al menos uno de los invariantes del escenario 1 se rompe (normalmente el 1.º y el 3.º;
        // la suma puede seguir cuadrando porque ambas cuentas pierden lo mismo):
        var seRompio = exitosas > 10                       // se aprobaron débitos sin saldo real
                    || 1000m - enviadas.Total != saldoAna  // el historial no cuadra con el saldo
                    || saldoAna + saldoLuis != 1000m;      // se creó o se perdió dinero
        Assert.True(seRompio, "Se esperaba una actualización perdida sin FOR UPDATE.");

        // …y el CHECK (saldo >= 0) NO lo detecta: ningún saldo llega a ser negativo
        Assert.True(saldoAna >= 0 && saldoLuis >= 0);
    }
}
