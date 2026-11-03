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

public interface IBilleteraService
{
    Task<BilleteraResponse> ObtenerAsync(Guid billeteraId);
    Task<DestinatarioResponse> BuscarDestinatarioAsync(string numeroCuenta);
    Task<TransaccionResponse> RecargarAsync(RecargaRequest request);
    Task<TransaccionResponse> RetirarAsync(RetiroRequest request);
    Task<TransferenciaResponse> TransferirAsync(TransferenciaRequest request);
    Task<ComisionResponse> CalcularComisionAsync(Guid billeteraOrigenId, decimal monto);
    Task<PaginaResponse<TransaccionResponse>> ObtenerHistorialAsync(
        Guid billeteraId, int pagina, int tamanoPagina, TipoTransaccion? tipo);
}
