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
    Task AgregarAsync(Cliente cliente);
}
