using System.Security.Cryptography;
using BilleteraDigital.Application.DTOs;
using BilleteraDigital.Application.Excepciones;
using BilleteraDigital.Application.Interfaces;
using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Interfaces;
using BilleteraDigital.Domain.ValueObjects;

namespace BilleteraDigital.Application.Servicios;

/// <summary>
/// Caso de uso: registro e inicio de sesión de clientes.
/// Depende SOLO de abstracciones (repositorios, unidad de trabajo, hasher):
/// Inversión de Dependencias (la "D" de SOLID).
/// </summary>
public class ClienteService : IClienteService
{
    private readonly IClienteRepository _clientes;
    private readonly IBilleteraRepository _billeteras;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IPasswordHasher _hasher;
    private readonly IGeneradorToken _tokens;
    private readonly IPuntosFidelidadRepository _puntos;
    private readonly IVerificacionTelefonoService _verificacion;

    public ClienteService(IClienteRepository clientes, IBilleteraRepository billeteras,
                          IUnitOfWork unidadDeTrabajo, IPasswordHasher hasher, IGeneradorToken tokens,
                          IPuntosFidelidadRepository puntos, IVerificacionTelefonoService verificacion)
    {
        _verificacion = verificacion;
        _puntos = puntos;
        _tokens = tokens;
        _clientes = clientes;
        _billeteras = billeteras;
        _unidadDeTrabajo = unidadDeTrabajo;
        _hasher = hasher;
    }

    public async Task<AuthResponse> RegistrarAsync(RegistrarClienteRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Password) || r.Password.Length < 8)
            throw new Domain.Excepciones.ReglaDeNegocioException("La contraseña debe tener al menos 8 caracteres.");

        var email = r.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (await _clientes.ExisteDniAsync(r.Dni))
            throw new ConflictoException("Ya existe un cliente registrado con ese DNI.");
        if (await _clientes.ExisteEmailAsync(email))
            throw new ConflictoException("Ya existe un cliente registrado con ese email.");

        // VALUE OBJECT: celular obligatorio y normalizado (+51 9XXXXXXXX).
        // Solo choca con celulares YA VERIFICADOS: si se exigiera unicidad a los no
        // verificados, cualquiera podría "secuestrar" el número de otra persona.
        var telefono = Telefono.Crear(r.Telefono);
        if (await _clientes.ExisteTelefonoVerificadoAsync(telefono.Valor))
            throw new ConflictoException("Ese celular ya está registrado y verificado por otro cliente.");

        // 1) Entidad Cliente (el dominio valida sus propias reglas)
        var cliente = Cliente.Crear(r.Nombres, r.Apellidos, r.Dni, email, telefono.Valor,
                                    _hasher.Hash(r.Password));

        // 2) Toda persona registrada recibe automáticamente una billetera en soles
        var billetera = Billetera.Abrir(cliente.Id, await GenerarNumeroCuentaUnicoAsync());
        cliente.AsignarBilletera(billetera);

        // 3) Ambos registros se guardan en una sola transacción (Unit of Work)
        await _unidadDeTrabajo.IniciarTransaccionAsync();
        try
        {
            await _clientes.AgregarAsync(cliente);
            await _billeteras.AgregarAsync(billetera);
            await _unidadDeTrabajo.ConfirmarAsync();
        }
        catch
        {
            await _unidadDeTrabajo.RevertirAsync();
            throw;
        }

        // 4) Se envía el código por SMS (simulado). El cliente ya puede usar la app,
        //    pero no podrá recibir transferencias POR CELULAR hasta verificarlo.
        await _verificacion.EnviarCodigoAsync(cliente.Id);

        return ConToken(Mapeador.AClienteResponse(cliente, billetera));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest r)
    {
        var cliente = await _clientes.ObtenerPorEmailAsync(r.Email?.Trim().ToLowerInvariant() ?? "");
        if (cliente is null || !_hasher.Verificar(r.Password ?? "", cliente.PasswordHash))
            throw new CredencialesInvalidasException();

        var billetera = await _billeteras.ObtenerPorClienteIdAsync(cliente.Id)
                        ?? throw new NoEncontradoException("El cliente no tiene billetera.");
        return ConToken(Mapeador.AClienteResponse(cliente, billetera));
    }

    public async Task<ClienteResponse> ObtenerPorIdAsync(Guid id)
    {
        var cliente = await _clientes.ObtenerPorIdAsync(id)
                      ?? throw new NoEncontradoException("Cliente no encontrado.");
        var billetera = await _billeteras.ObtenerPorClienteIdAsync(cliente.Id)
                        ?? throw new NoEncontradoException("El cliente no tiene billetera.");
        return Mapeador.AClienteResponse(cliente, billetera);
    }

    public async Task<PuntosResponse> ObtenerPuntosAsync(Guid clienteId)
    {
        if (await _clientes.ObtenerPorIdAsync(clienteId) is null)
            throw new NoEncontradoException("Cliente no encontrado.");

        var total = await _puntos.ObtenerTotalAsync(clienteId);
        var ultimos = await _puntos.ObtenerUltimosAsync(clienteId, 10);
        return new PuntosResponse(total, ultimos
            .Select(m => new MovimientoPuntosResponse(m.TipoOperacion.ToString(), m.Monto, m.Puntos, m.Referencia, m.Fecha))
            .ToList());
    }

    /// <summary>Emite el JWT del cliente (incluye su billetera como claim).</summary>
    private AuthResponse ConToken(ClienteResponse c)
    {
        var (token, expira) = _tokens.Generar(c.Id, c.Billetera.Id, c.Email);
        return new AuthResponse(token, expira, c);
    }

    private async Task<string> GenerarNumeroCuentaUnicoAsync()
    {
        while (true)
        {
            // Prefijo "19" + 8 dígitos aleatorios criptográficamente seguros
            var numero = "19" + RandomNumberGenerator.GetInt32(0, 100_000_000).ToString("D8");
            if (!await _billeteras.ExisteNumeroCuentaAsync(numero)) return numero;
        }
    }
}
