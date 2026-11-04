using System.Security.Cryptography;
using BilleteraDigital.Application.DTOs;
using BilleteraDigital.Application.Excepciones;
using BilleteraDigital.Application.Interfaces;
using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Excepciones;
using BilleteraDigital.Domain.Interfaces;

namespace BilleteraDigital.Application.Servicios;

/// <summary>
/// Caso de uso: demostrar que el celular pertenece a quien se registró.
/// Solo un celular VERIFICADO puede ser encontrado para recibir transferencias.
/// Las reglas del código (vencimiento, intentos, reenvío) viven en la entidad
/// <see cref="VerificacionTelefono"/>; este servicio solo las orquesta.
/// </summary>
public class VerificacionTelefonoService : IVerificacionTelefonoService
{
    private readonly IClienteRepository _clientes;
    private readonly IBilleteraRepository _billeteras;
    private readonly IVerificacionTelefonoRepository _verificaciones;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IEnviadorSms _sms;
    private readonly TimeProvider _reloj;

    public VerificacionTelefonoService(IClienteRepository clientes, IBilleteraRepository billeteras,
        IVerificacionTelefonoRepository verificaciones, IUnitOfWork unidadDeTrabajo,
        IEnviadorSms sms, TimeProvider reloj)
    {
        _clientes = clientes;
        _billeteras = billeteras;
        _verificaciones = verificaciones;
        _unidadDeTrabajo = unidadDeTrabajo;
        _sms = sms;
        _reloj = reloj;
    }

    public async Task EnviarCodigoAsync(Guid clienteId)
    {
        var cliente = await _clientes.ObtenerPorIdAsync(clienteId)
                      ?? throw new NoEncontradoException("Cliente no encontrado.");
        if (cliente.TelefonoVerificado)
            throw new ReglaDeNegocioException("Tu celular ya está verificado.");
        // Clientes antiguos con un teléfono que no se pudo normalizar: no se envía nada
        var celular = Domain.ValueObjects.Telefono.Crear(cliente.Telefono);

        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6"); // criptográficamente seguro
        var ahora = Ahora();

        var verificacion = await _verificaciones.ObtenerAsync(clienteId);
        if (verificacion is null)
            await _verificaciones.AgregarAsync(VerificacionTelefono.Generar(clienteId, codigo, ahora));
        else
            verificacion.Renovar(codigo, ahora);    // la entidad exige 60 s entre envíos

        await _unidadDeTrabajo.GuardarCambiosAsync();

        await _sms.EnviarAsync(celular.Valor,
            $"Billetera Digital: tu código de verificación es {codigo}. " +
            "Vence en 10 minutos. No lo compartas con nadie.");
    }

    public async Task<ClienteResponse> VerificarAsync(Guid clienteId, string codigo)
    {
        var cliente = await _clientes.ObtenerPorIdParaActualizarAsync(clienteId)
                      ?? throw new NoEncontradoException("Cliente no encontrado.");

        if (!cliente.TelefonoVerificado)
        {
            var verificacion = await _verificaciones.ObtenerAsync(clienteId)
                ?? throw new ReglaDeNegocioException("No hay un código pendiente. Solicita uno nuevo.");

            if (!verificacion.Verificar(codigo, Ahora()))
            {
                await _unidadDeTrabajo.GuardarCambiosAsync();   // el intento fallido cuenta
                var restantes = VerificacionTelefono.MaximoIntentos - verificacion.Intentos;
                throw new ReglaDeNegocioException(restantes > 0
                    ? $"Código incorrecto. Te quedan {restantes} intentos."
                    : "Código incorrecto. Solicita un código nuevo.");
            }

            // Otro cliente pudo verificar el mismo número antes (el índice único de la BD
            // es la última línea de defensa si ambos llegan a la vez).
            if (await _clientes.ExisteTelefonoVerificadoAsync(cliente.Telefono, cliente.Id))
                throw new ConflictoException("Este celular ya está verificado por otra cuenta.");

            cliente.MarcarTelefonoVerificado();
            _verificaciones.Eliminar(verificacion);
            await _unidadDeTrabajo.GuardarCambiosAsync();
        }

        var billetera = await _billeteras.ObtenerPorClienteIdAsync(clienteId)
                        ?? throw new NoEncontradoException("El cliente no tiene billetera.");
        return Mapeador.AClienteResponse(cliente, billetera);
    }

    private DateTime Ahora() => _reloj.GetUtcNow().UtcDateTime;
}
