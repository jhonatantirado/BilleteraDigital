using System.Security.Cryptography;
using BilleteraDigital.Application.DTOs;
using BilleteraDigital.Application.Excepciones;
using BilleteraDigital.Application.Interfaces;
using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Interfaces;

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

    public ClienteService(IClienteRepository clientes, IBilleteraRepository billeteras,
                          IUnitOfWork unidadDeTrabajo, IPasswordHasher hasher)
    {
        _clientes = clientes;
        _billeteras = billeteras;
        _unidadDeTrabajo = unidadDeTrabajo;
        _hasher = hasher;
    }

    public async Task<ClienteResponse> RegistrarAsync(RegistrarClienteRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Password) || r.Password.Length < 8)
            throw new Domain.Excepciones.ReglaDeNegocioException("La contraseña debe tener al menos 8 caracteres.");

        var email = r.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (await _clientes.ExisteDniAsync(r.Dni))
            throw new ConflictoException("Ya existe un cliente registrado con ese DNI.");
        if (await _clientes.ExisteEmailAsync(email))
            throw new ConflictoException("Ya existe un cliente registrado con ese email.");

        // 1) Entidad Cliente (el dominio valida sus propias reglas)
        var cliente = Cliente.Crear(r.Nombres, r.Apellidos, r.Dni, email, r.Telefono,
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

        return Mapeador.AClienteResponse(cliente, billetera);
    }

    public async Task<ClienteResponse> LoginAsync(LoginRequest r)
    {
        var cliente = await _clientes.ObtenerPorEmailAsync(r.Email?.Trim().ToLowerInvariant() ?? "");
        if (cliente is null || !_hasher.Verificar(r.Password ?? "", cliente.PasswordHash))
            throw new CredencialesInvalidasException();

        var billetera = await _billeteras.ObtenerPorClienteIdAsync(cliente.Id)
                        ?? throw new NoEncontradoException("El cliente no tiene billetera.");
        return Mapeador.AClienteResponse(cliente, billetera);
    }

    public async Task<ClienteResponse> ObtenerPorIdAsync(Guid id)
    {
        var cliente = await _clientes.ObtenerPorIdAsync(id)
                      ?? throw new NoEncontradoException("Cliente no encontrado.");
        var billetera = await _billeteras.ObtenerPorClienteIdAsync(cliente.Id)
                        ?? throw new NoEncontradoException("El cliente no tiene billetera.");
        return Mapeador.AClienteResponse(cliente, billetera);
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
