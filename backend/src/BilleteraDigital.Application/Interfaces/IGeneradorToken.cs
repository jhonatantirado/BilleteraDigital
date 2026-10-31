namespace BilleteraDigital.Application.Interfaces;

/// <summary>
/// Contrato para emitir tokens de acceso (JWT). La capa de Aplicación solo conoce
/// esta abstracción; la implementación concreta vive en Infraestructura.
/// </summary>
public interface IGeneradorToken
{
    (string Token, DateTime Expira) Generar(Guid clienteId, Guid billeteraId, string email);
}
