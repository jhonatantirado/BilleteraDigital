using BilleteraDigital.Application.DTOs;
using BilleteraDigital.Domain.Enums;

namespace BilleteraDigital.Application.Interfaces;

public interface IClienteService
{
    Task<AuthResponse> RegistrarAsync(RegistrarClienteRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<ClienteResponse> ObtenerPorIdAsync(Guid id);
    Task<PuntosResponse> ObtenerPuntosAsync(Guid clienteId);
}

/// <summary>Verificación del celular con un código de 6 dígitos enviado por SMS.</summary>
public interface IVerificacionTelefonoService
{
    /// <summary>Genera un código nuevo y lo envía por SMS (máximo uno cada 60 s).</summary>
    Task EnviarCodigoAsync(Guid clienteId);

    /// <summary>Comprueba el código; si es correcto, el celular queda verificado.</summary>
    Task<ClienteResponse> VerificarAsync(Guid clienteId, string codigo);
}

public interface IBilleteraService
{
    Task<BilleteraResponse> ObtenerAsync(Guid billeteraId);
    Task<DestinatarioResponse> BuscarDestinatarioAsync(string numeroCuenta);
    Task<DestinatarioResponse> BuscarDestinatarioPorTelefonoAsync(string telefono);
    Task<TransaccionResponse> RecargarAsync(RecargaRequest request);
    Task<TransaccionResponse> RetirarAsync(RetiroRequest request);
    Task<TransferenciaResponse> TransferirAsync(TransferenciaRequest request);
    Task<ComisionResponse> CalcularComisionAsync(Guid billeteraOrigenId, decimal monto);
    Task<PaginaResponse<TransaccionResponse>> ObtenerHistorialAsync(
        Guid billeteraId, int pagina, int tamanoPagina, TipoTransaccion? tipo);
}
