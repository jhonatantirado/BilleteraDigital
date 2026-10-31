using BilleteraDigital.Application.DTOs;
using BilleteraDigital.Domain.Enums;

namespace BilleteraDigital.Application.Interfaces;

public interface IClienteService
{
    Task<ClienteResponse> RegistrarAsync(RegistrarClienteRequest request);
    Task<ClienteResponse> LoginAsync(LoginRequest request);
    Task<ClienteResponse> ObtenerPorIdAsync(Guid id);
}

public interface IBilleteraService
{
    Task<BilleteraResponse> ObtenerAsync(Guid billeteraId);
    Task<DestinatarioResponse> BuscarDestinatarioAsync(string numeroCuenta);
    Task<TransaccionResponse> RecargarAsync(RecargaRequest request);
    Task<TransferenciaResponse> TransferirAsync(TransferenciaRequest request);
    ComisionResponse CalcularComision(decimal monto);
    Task<PaginaResponse<TransaccionResponse>> ObtenerHistorialAsync(
        Guid billeteraId, int pagina, int tamanoPagina, TipoTransaccion? tipo);
}
