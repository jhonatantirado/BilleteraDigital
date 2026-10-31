using System.Security.Claims;
using BilleteraDigital.Application.Excepciones;

namespace BilleteraDigital.Api.Configuracion;

/// <summary>
/// Lee la identidad del usuario autenticado desde los claims del JWT.
/// Así el origen de cada operación sale del token, nunca del body de la petición.
/// </summary>
public static class UsuarioActual
{
    public static Guid ClienteId(this ClaimsPrincipal usuario) =>
        Guid.Parse(usuario.FindFirstValue("sub")
                   ?? throw new AccesoDenegadoException("Token sin identificador de cliente."));

    public static Guid BilleteraId(this ClaimsPrincipal usuario) =>
        Guid.Parse(usuario.FindFirstValue("billetera_id")
                   ?? throw new AccesoDenegadoException("Token sin billetera asociada."));

    /// <summary>Lanza 403 si la billetera pedida no pertenece al usuario del token.</summary>
    public static void ExigirBilletera(this ClaimsPrincipal usuario, Guid billeteraId)
    {
        if (usuario.BilleteraId() != billeteraId) throw new AccesoDenegadoException();
    }

    /// <summary>Lanza 403 si el cliente pedido no es el usuario del token.</summary>
    public static void ExigirCliente(this ClaimsPrincipal usuario, Guid clienteId)
    {
        if (usuario.ClienteId() != clienteId) throw new AccesoDenegadoException();
    }
}
