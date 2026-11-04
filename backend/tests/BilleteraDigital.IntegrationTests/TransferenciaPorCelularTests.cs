using BilleteraDigital.Application.DTOs;
using BilleteraDigital.Application.Excepciones;
using BilleteraDigital.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BilleteraDigital.IntegrationTests;

/// <summary>Búsqueda y transferencia por celular con PostgreSQL real (índice único, join con clientes).</summary>
[Collection(ColeccionPostgres.Nombre)]
[Trait("Categoria", "Integracion")]
public class TransferenciaPorCelularTests
{
    private readonly BaseDeDatosFixture _bd;
    public TransferenciaPorCelularTests(BaseDeDatosFixture bd) => _bd = bd;

    private async Task<T> ConServicio<T>(Func<IBilleteraService, Task<T>> accion)
    {
        await using var scope = _bd.Servicios.CreateAsyncScope();
        return await accion(scope.ServiceProvider.GetRequiredService<IBilleteraService>());
    }

    [Fact]
    public async Task Se_transfiere_por_celular_verificado_sin_revelar_la_cuenta()
    {
        var (ana, _) = await _bd.CrearClienteConCelularAsync(500m, celularVerificado: true);
        var (luis, celularLuis) = await _bd.CrearClienteConCelularAsync(0m, celularVerificado: true);
        var conEspacios = $"{celularLuis[..3]} {celularLuis[3..6]} {celularLuis[6..]}";   // "9xx xxx xxx"

        var destinatario = await ConServicio(s => s.BuscarDestinatarioPorTelefonoAsync(conEspacios));
        var r = await ConServicio(s => s.TransferirAsync(
            new TransferenciaRequest(ana.Id, null, 100m, null, TelefonoDestino: "+51" + celularLuis)));

        Assert.Null(destinatario.NumeroCuenta);                 // no se revela la cuenta
        Assert.EndsWith(celularLuis[^3..], destinatario.Telefono);
        Assert.Null(r.CuentaDestino);
        Assert.Equal(400m, await _bd.SaldoAsync(ana.Id));
        Assert.Equal(100m, await _bd.SaldoAsync(luis.Id));
    }

    [Fact]
    public async Task Un_celular_no_verificado_no_se_encuentra()
    {
        var (ana, _) = await _bd.CrearClienteConCelularAsync(500m, celularVerificado: true);
        var (_, celularPedro) = await _bd.CrearClienteConCelularAsync(0m, celularVerificado: false);

        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            ConServicio(s => s.BuscarDestinatarioPorTelefonoAsync(celularPedro)));
        await Assert.ThrowsAsync<NoEncontradoException>(() => ConServicio(s => s.TransferirAsync(
            new TransferenciaRequest(ana.Id, null, 100m, null, TelefonoDestino: celularPedro))));
        Assert.Equal(500m, await _bd.SaldoAsync(ana.Id));
    }

    [Fact]
    public async Task Cuenta_y_celular_a_la_vez_es_una_solicitud_invalida()
    {
        var (ana, _) = await _bd.CrearClienteConCelularAsync(500m, celularVerificado: true);
        var (luis, celularLuis) = await _bd.CrearClienteConCelularAsync(0m, celularVerificado: true);

        await Assert.ThrowsAsync<ArgumentException>(() => ConServicio(s => s.TransferirAsync(
            new TransferenciaRequest(ana.Id, luis.NumeroCuenta, 100m, null, TelefonoDestino: celularLuis))));
    }
}
