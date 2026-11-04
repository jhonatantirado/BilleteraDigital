using BilleteraDigital.Domain.Entidades;

namespace BilleteraDigital.Domain.Interfaces;

public interface IVerificacionTelefonoRepository
{
    /// <summary>Verificación pendiente del cliente, con seguimiento de cambios.</summary>
    Task<VerificacionTelefono?> ObtenerAsync(Guid clienteId);

    Task AgregarAsync(VerificacionTelefono verificacion);

    void Eliminar(VerificacionTelefono verificacion);
}
