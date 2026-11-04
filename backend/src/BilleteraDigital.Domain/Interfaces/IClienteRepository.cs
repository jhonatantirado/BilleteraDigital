using BilleteraDigital.Domain.Entidades;

namespace BilleteraDigital.Domain.Interfaces;

/// <summary>
/// PATRÓN REPOSITORY: el dominio define QUÉ necesita (contrato);
/// la infraestructura decide CÓMO se obtiene (PostgreSQL + EF Core).
/// </summary>
public interface IClienteRepository
{
    Task<Cliente?> ObtenerPorIdAsync(Guid id);
    Task<Cliente?> ObtenerPorEmailAsync(string email);
    Task<bool> ExisteDniAsync(string dni);
    Task<bool> ExisteEmailAsync(string email);

    /// <summary>¿Algún OTRO cliente ya verificó este celular? (formato E.164)</summary>
    Task<bool> ExisteTelefonoVerificadoAsync(string telefono, Guid? exceptoClienteId = null);

    /// <summary>Cliente con seguimiento de cambios (para modificarlo y guardar).</summary>
    Task<Cliente?> ObtenerPorIdParaActualizarAsync(Guid id);
    Task AgregarAsync(Cliente cliente);
}
