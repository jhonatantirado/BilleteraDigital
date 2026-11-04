using BilleteraDigital.Application.DTOs;
using BilleteraDigital.Application.Excepciones;
using BilleteraDigital.Application.Interfaces;
using BilleteraDigital.Application.Servicios;
using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Excepciones;
using BilleteraDigital.Domain.Interfaces;
using BilleteraDigital.Domain.ValueObjects;
using Xunit;

namespace BilleteraDigital.Tests;

/// <summary>Value Object Telefono: validación y normalización a E.164.</summary>
public class TelefonoTests
{
    [Theory]
    [InlineData("987654321")]
    [InlineData("987 654 321")]
    [InlineData("+51 987-654-321")]
    [InlineData("51987654321")]
    [InlineData("+51987654321")]
    public void Normaliza_todas_las_variantes_al_mismo_valor(string texto)
    {
        Assert.Equal("+51987654321", Telefono.Crear(texto).Valor);
    }

    [Theory]
    [InlineData("812345678")]     // no empieza con 9
    [InlineData("98765432")]      // 8 dígitos
    [InlineData("9876543210")]    // 10 dígitos
    [InlineData("")]
    [InlineData(null)]
    public void Rechaza_celulares_invalidos(string? texto)
    {
        Assert.Throws<ReglaDeNegocioException>(() => Telefono.Crear(texto));
    }

    [Fact]
    public void Dos_telefonos_con_el_mismo_valor_son_iguales_y_se_enmascaran()
    {
        var a = Telefono.Crear("987 654 321");
        Assert.Equal(a, Telefono.Crear("+51987654321"));     // se compara por valor (record)
        Assert.Equal("*** *** 321", a.Enmascarado);
    }

    [Fact]
    public void El_cliente_guarda_el_celular_normalizado_y_sin_verificar()
    {
        var c = Cliente.Crear("Ana", "Quispe", "70123456", "ana@demo.pe", "987 654 321", "hash");
        Assert.Equal("+51987654321", c.Telefono);
        Assert.False(c.TelefonoVerificado);
    }
}

/// <summary>Reglas del código de verificación (entidad VerificacionTelefono).</summary>
public class VerificacionTelefonoTests
{
    private static readonly DateTime T0 = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _cliente = Guid.NewGuid();

    [Fact]
    public void El_codigo_correcto_verifica_y_el_incorrecto_cuenta_un_intento()
    {
        var v = VerificacionTelefono.Generar(_cliente, "123456", T0);

        Assert.False(v.Verificar("000000", T0.AddMinutes(1)));
        Assert.Equal(1, v.Intentos);
        Assert.True(v.Verificar("123456", T0.AddMinutes(2)));
    }

    [Fact]
    public void No_se_guarda_el_codigo_sino_su_hash()
    {
        var v = VerificacionTelefono.Generar(_cliente, "123456", T0);
        Assert.DoesNotContain("123456", v.CodigoHash);
        Assert.Equal(64, v.CodigoHash.Length);
    }

    [Fact]
    public void El_codigo_vence_a_los_10_minutos()
    {
        var v = VerificacionTelefono.Generar(_cliente, "123456", T0);
        Assert.Throws<ReglaDeNegocioException>(() => v.Verificar("123456", T0.AddMinutes(11)));
    }

    [Fact]
    public void Tras_5_intentos_fallidos_se_bloquea_aunque_el_codigo_sea_correcto()
    {
        var v = VerificacionTelefono.Generar(_cliente, "123456", T0);
        for (var i = 0; i < VerificacionTelefono.MaximoIntentos; i++) v.Verificar("000000", T0);

        Assert.Throws<ReglaDeNegocioException>(() => v.Verificar("123456", T0));
    }

    [Fact]
    public void Solo_se_puede_pedir_otro_codigo_cada_60_segundos_y_reinicia_los_intentos()
    {
        var v = VerificacionTelefono.Generar(_cliente, "123456", T0);
        v.Verificar("000000", T0);

        Assert.Throws<ReglaDeNegocioException>(() => v.Renovar("654321", T0.AddSeconds(30)));
        v.Renovar("654321", T0.AddSeconds(61));

        Assert.Equal(0, v.Intentos);
        Assert.False(v.Verificar("123456", T0.AddSeconds(62)));   // el código anterior ya no sirve
        Assert.True(v.Verificar("654321", T0.AddSeconds(63)));
    }
}

/// <summary>Caso de uso completo con dobles en memoria (sin base de datos).</summary>
public class VerificacionTelefonoServiceTests
{
    private class RelojFijo : TimeProvider
    {
        public DateTimeOffset Ahora { get; set; } = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Ahora;
    }

    private class SmsEspia : IEnviadorSms
    {
        public List<(string Telefono, string Mensaje)> Enviados { get; } = new();
        public Task EnviarAsync(string t, string m) { Enviados.Add((t, m)); return Task.CompletedTask; }
        public string UltimoCodigo => System.Text.RegularExpressions.Regex.Match(Enviados[^1].Mensaje, @"\b\d{6}\b").Value;
    }

    private class Clientes : IClienteRepository
    {
        public List<Cliente> Datos { get; } = new();
        public Task<Cliente?> ObtenerPorIdAsync(Guid id) => Task.FromResult(Datos.FirstOrDefault(c => c.Id == id));
        public Task<Cliente?> ObtenerPorIdParaActualizarAsync(Guid id) => ObtenerPorIdAsync(id);
        public Task<Cliente?> ObtenerPorEmailAsync(string e) => Task.FromResult(Datos.FirstOrDefault(c => c.Email == e));
        public Task<bool> ExisteDniAsync(string d) => Task.FromResult(Datos.Any(c => c.Dni == d));
        public Task<bool> ExisteEmailAsync(string e) => Task.FromResult(Datos.Any(c => c.Email == e));
        public Task<bool> ExisteTelefonoVerificadoAsync(string t, Guid? excepto = null) =>
            Task.FromResult(Datos.Any(c => c.Telefono == t && c.TelefonoVerificado && c.Id != excepto));
        public Task AgregarAsync(Cliente c) { Datos.Add(c); return Task.CompletedTask; }
    }

    private class Verificaciones : IVerificacionTelefonoRepository
    {
        public List<VerificacionTelefono> Datos { get; } = new();
        public Task<VerificacionTelefono?> ObtenerAsync(Guid id) => Task.FromResult(Datos.FirstOrDefault(v => v.ClienteId == id));
        public Task AgregarAsync(VerificacionTelefono v) { Datos.Add(v); return Task.CompletedTask; }
        public void Eliminar(VerificacionTelefono v) => Datos.Remove(v);
    }

    private class Billeteras : IBilleteraRepository
    {
        public List<Billetera> Datos { get; } = new();
        public Task<Billetera?> ObtenerPorIdAsync(Guid id) => Task.FromResult(Datos.FirstOrDefault(b => b.Id == id));
        public Task<Billetera?> ObtenerPorClienteIdAsync(Guid c) => Task.FromResult(Datos.FirstOrDefault(b => b.ClienteId == c));
        public Task<Billetera?> ObtenerPorNumeroCuentaAsync(string n) => Task.FromResult(Datos.FirstOrDefault(b => b.NumeroCuenta == n));
        public Task<Billetera?> ObtenerPorTelefonoAsync(string t) => Task.FromResult<Billetera?>(null);
        public Task<Billetera?> ObtenerPorIdParaActualizarAsync(Guid id) => ObtenerPorIdAsync(id);
        public Task<bool> ExisteNumeroCuentaAsync(string n) => Task.FromResult(false);
        public Task AgregarAsync(Billetera b) { Datos.Add(b); return Task.CompletedTask; }
    }

    private class UnidadNula : IUnitOfWork
    {
        public Task IniciarTransaccionAsync() => Task.CompletedTask;
        public Task ConfirmarAsync() => Task.CompletedTask;
        public Task RevertirAsync() => Task.CompletedTask;
        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private readonly Clientes _clientes = new();
    private readonly Billeteras _billeteras = new();
    private readonly Verificaciones _verificaciones = new();
    private readonly SmsEspia _sms = new();
    private readonly RelojFijo _reloj = new();
    private readonly VerificacionTelefonoService _servicio;
    private readonly Cliente _ana;

    public VerificacionTelefonoServiceTests()
    {
        _servicio = new VerificacionTelefonoService(_clientes, _billeteras, _verificaciones,
            new UnidadNula(), _sms, _reloj);
        _ana = NuevoCliente("70123456", "ana@demo.pe", "987654321");
    }

    private Cliente NuevoCliente(string dni, string email, string celular)
    {
        var c = Cliente.Crear("Ana", "Quispe", dni, email, celular, "hash");
        _clientes.Datos.Add(c);
        _billeteras.Datos.Add(Billetera.Abrir(c.Id, "19" + dni));
        return c;
    }

    [Fact]
    public async Task Envia_un_SMS_con_el_codigo_y_al_ingresarlo_el_celular_queda_verificado()
    {
        await _servicio.EnviarCodigoAsync(_ana.Id);
        var respuesta = await _servicio.VerificarAsync(_ana.Id, _sms.UltimoCodigo);

        Assert.Equal("+51987654321", _sms.Enviados.Single().Telefono);
        Assert.True(respuesta.TelefonoVerificado);
        Assert.Empty(_verificaciones.Datos);          // el código pendiente se elimina
    }

    [Fact]
    public async Task Un_codigo_incorrecto_informa_los_intentos_restantes()
    {
        await _servicio.EnviarCodigoAsync(_ana.Id);
        var otro = _sms.UltimoCodigo == "000000" ? "111111" : "000000";

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _servicio.VerificarAsync(_ana.Id, otro));

        Assert.Contains("4 intentos", error.Message);
        Assert.False(_ana.TelefonoVerificado);
    }

    [Fact]
    public async Task No_se_puede_verificar_un_celular_que_otra_cuenta_ya_verifico()
    {
        var duena = NuevoCliente("70999999", "duena@demo.pe", "987654321");
        duena.MarcarTelefonoVerificado();             // la verdadera dueña verificó primero

        await _servicio.EnviarCodigoAsync(_ana.Id);
        await Assert.ThrowsAsync<ConflictoException>(() => _servicio.VerificarAsync(_ana.Id, _sms.UltimoCodigo));
        Assert.False(_ana.TelefonoVerificado);
    }

    [Fact]
    public async Task Reenviar_antes_de_60_segundos_es_rechazado()
    {
        await _servicio.EnviarCodigoAsync(_ana.Id);
        _reloj.Ahora = _reloj.Ahora.AddSeconds(10);
        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => _servicio.EnviarCodigoAsync(_ana.Id));

        _reloj.Ahora = _reloj.Ahora.AddSeconds(60);
        await _servicio.EnviarCodigoAsync(_ana.Id);
        Assert.Equal(2, _sms.Enviados.Count);
    }
}
