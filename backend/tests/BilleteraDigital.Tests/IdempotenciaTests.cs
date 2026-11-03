using System.Collections.Concurrent;
using System.Security.Claims;
using BilleteraDigital.Api.Filtros;
using BilleteraDigital.Application.DTOs;
using BilleteraDigital.Application.Idempotencia;
using BilleteraDigital.Domain.Excepciones;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BilleteraDigital.Tests;

/// <summary>
/// Pruebas del filtro [Idempotente] sin servidor ni base de datos: se arma el
/// contexto de MVC a mano y el almacén de claves es un diccionario en memoria
/// cuya reserva es atómica (como la clave primaria de PostgreSQL).
/// </summary>
public class IdempotenciaTests
{
    private const string Ruta = "/api/transacciones/transferencias";

    // ------------------------------------------------------------ dobles de prueba
    private class AlmacenEnMemoria : IIdempotenciaRepository
    {
        public ConcurrentDictionary<(Guid, string), RegistroIdempotencia> Filas { get; } = new();

        public Task<bool> IntentarReservarAsync(Guid c, string k, string ruta, string hash, DateTime fecha) =>
            Task.FromResult(Filas.TryAdd((c, k), new RegistroIdempotencia
            {
                ClienteId = c, Clave = k, Ruta = ruta, HashCuerpo = hash,
                Estado = EstadoIdempotencia.EnProceso, FechaCreacion = fecha
            }));

        public Task<RegistroIdempotencia?> ObtenerAsync(Guid c, string k) =>
            Task.FromResult(Filas.TryGetValue((c, k), out var r) ? r : null);

        public Task CompletarAsync(Guid c, string k, int codigo, string respuesta, string tipo)
        {
            var r = Filas[(c, k)];
            Filas[(c, k)] = new RegistroIdempotencia
            {
                ClienteId = c, Clave = k, Ruta = r.Ruta, HashCuerpo = r.HashCuerpo, FechaCreacion = r.FechaCreacion,
                Estado = EstadoIdempotencia.Completada, CodigoHttp = codigo, Respuesta = respuesta, TipoContenido = tipo
            };
            return Task.CompletedTask;
        }

        public Task LiberarAsync(Guid c, string k) { Filas.TryRemove((c, k), out _); return Task.CompletedTask; }

        public Task<int> EliminarAnterioresAAsync(DateTime limite) => Task.FromResult(0);
    }

    private class RelojFijo : TimeProvider
    {
        public DateTimeOffset Ahora { get; set; } = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Ahora;
    }

    private record Resultado(int Codigo, string? Cuerpo, bool Repetida);

    private readonly AlmacenEnMemoria _almacen = new();
    private readonly RelojFijo _reloj = new();
    private readonly Guid _ana = Guid.NewGuid();
    private int _ejecuciones;

    private IdempotenciaFilter NuevoFiltro() => new(_almacen, _reloj,
        Options.Create(new JsonOptions()), NullLogger<IdempotenciaFilter>.Instance);

    /// <summary>Simula una petición HTTP completa que pasa por el filtro.</summary>
    private async Task<Resultado> Enviar(string? clave, decimal monto, Guid? cliente = null,
        Func<Task>? accion = null, Exception? lanza = null)
    {
        var http = new DefaultHttpContext();
        http.Request.Path = Ruta;
        if (clave is not null) http.Request.Headers[IdempotenciaFilter.Cabecera] = clave;
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("sub", (cliente ?? _ana).ToString()) }, "prueba"));

        var ctx = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var argumentos = new Dictionary<string, object?>
        {
            ["request"] = new TransferenciaRequest(Guid.Empty, "1900000002", monto, null)
        };
        var antes = new ActionExecutingContext(ctx, new List<IFilterMetadata>(), argumentos, new object());
        ActionExecutedContext? despues = null;

        await NuevoFiltro().OnActionExecutionAsync(antes, async () =>
        {
            Interlocked.Increment(ref _ejecuciones);          // "se movió dinero"
            if (accion is not null) await accion();
            despues = new ActionExecutedContext(ctx, new List<IFilterMetadata>(), new object());
            if (lanza is not null) despues.Exception = lanza;
            else despues.Result = new ObjectResult(new { referencia = "ref-1", monto }) { StatusCode = 201 };
            return despues;
        });

        var repetida = http.Response.Headers.ContainsKey(IdempotenciaFilter.CabeceraRepetida);
        var final = antes.Result ?? despues?.Result;
        return final switch
        {
            ContentResult c => new Resultado(c.StatusCode ?? 200, c.Content, repetida),
            ObjectResult o => new Resultado(o.StatusCode ?? 200, null, repetida),
            _ => new Resultado(500, null, repetida)          // excepción no manejada → middleware
        };
    }

    // --------------------------------------------------------------------- pruebas
    [Fact]
    public async Task La_misma_clave_no_repite_la_transferencia_y_devuelve_la_respuesta_original()
    {
        var primera = await Enviar("clave-0001", 500m);
        var segunda = await Enviar("clave-0001", 500m);

        Assert.Equal(1, _ejecuciones);
        Assert.Equal(201, primera.Codigo);
        Assert.Equal(201, segunda.Codigo);
        Assert.True(segunda.Repetida);
        Assert.Contains("ref-1", segunda.Cuerpo);
    }

    [Fact]
    public async Task La_misma_clave_con_otro_monto_es_rechazada_con_422()
    {
        await Enviar("clave-0001", 500m);
        var otra = await Enviar("clave-0001", 600m);

        Assert.Equal(422, otra.Codigo);
        Assert.Equal(1, _ejecuciones);
    }

    [Fact]
    public async Task Sin_cabecera_o_con_formato_invalido_responde_400()
    {
        Assert.Equal(400, (await Enviar(null, 500m)).Codigo);
        Assert.Equal(400, (await Enviar("corta", 500m)).Codigo);
        Assert.Equal(0, _ejecuciones);
    }

    [Fact]
    public async Task Un_error_de_negocio_se_guarda_y_se_repite_igual()
    {
        var primera = await Enviar("clave-0002", 9000m, lanza: new SaldoInsuficienteException(100m, 9000m));
        var segunda = await Enviar("clave-0002", 9000m);

        Assert.Equal(422, primera.Codigo);
        Assert.Equal(422, segunda.Codigo);
        Assert.True(segunda.Repetida);
        Assert.Equal(1, _ejecuciones);
    }

    [Fact]
    public async Task Un_error_inesperado_libera_la_clave_y_permite_reintentar()
    {
        var primera = await Enviar("clave-0003", 500m, lanza: new InvalidOperationException("BD caída"));
        var reintento = await Enviar("clave-0003", 500m);

        Assert.Equal(500, primera.Codigo);
        Assert.Equal(201, reintento.Codigo);
        Assert.False(reintento.Repetida);
        Assert.Equal(2, _ejecuciones);
    }

    [Fact]
    public async Task Diez_peticiones_simultaneas_con_la_misma_clave_mueven_dinero_una_sola_vez()
    {
        var liberar = new TaskCompletionSource();
        var primera = Enviar("clave-0004", 500m, accion: () => liberar.Task); // queda "procesando"

        var simultaneas = await Task.WhenAll(Enumerable.Range(0, 9).Select(_ => Enviar("clave-0004", 500m)));
        liberar.SetResult();
        var resultadoPrimera = await primera;

        Assert.Equal(1, _ejecuciones);
        Assert.Equal(201, resultadoPrimera.Codigo);
        Assert.All(simultaneas, r => Assert.Equal(409, r.Codigo));
    }

    [Fact]
    public async Task Una_reserva_abandonada_se_vuelve_a_procesar()
    {
        await _almacen.IntentarReservarAsync(_ana, "clave-0005", Ruta,
            IdempotenciaFilter.CalcularHash(new Dictionary<string, object?>
            {
                ["request"] = new TransferenciaRequest(Guid.Empty, "1900000002", 500m, null)
            }),
            _reloj.Ahora.UtcDateTime);
        _reloj.Ahora = _reloj.Ahora.AddMinutes(3);   // el servidor "se cayó" hace 3 minutos

        var r = await Enviar("clave-0005", 500m);

        Assert.Equal(201, r.Codigo);
        Assert.Equal(1, _ejecuciones);
    }

    [Fact]
    public async Task La_misma_clave_de_dos_clientes_distintos_son_operaciones_independientes()
    {
        await Enviar("clave-0006", 500m, cliente: Guid.NewGuid());
        await Enviar("clave-0006", 500m, cliente: Guid.NewGuid());

        Assert.Equal(2, _ejecuciones);
    }
}
