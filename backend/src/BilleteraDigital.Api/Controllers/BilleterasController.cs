using BilleteraDigital.Api.Configuracion;
using BilleteraDigital.Application.DTOs;
using BilleteraDigital.Application.Interfaces;
using BilleteraDigital.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BilleteraDigital.Api.Controllers;

[ApiController]
[Route("api/billeteras")]
[Produces("application/json")]
public class BilleterasController : ControllerBase
{
    private readonly IBilleteraService _servicio;
    public BilleterasController(IBilleteraService servicio) => _servicio = servicio;

    /// <summary>Consulta el saldo de la billetera del usuario autenticado.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BilleteraResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BilleteraResponse>> Obtener(Guid id)
    {
        User.ExigirBilletera(id);
        return Ok(await _servicio.ObtenerAsync(id));
    }

    /// <summary>Busca al titular de una cuenta (para confirmar antes de transferir).</summary>
    [HttpGet("cuenta/{numeroCuenta}")]
    [EnableRateLimiting(LimitesDeUso.BusquedaDestinatario)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(DestinatarioResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DestinatarioResponse>> BuscarPorCuenta(string numeroCuenta) =>
        Ok(await _servicio.BuscarDestinatarioAsync(numeroCuenta));

    /// <summary>
    /// Busca al titular de un celular VERIFICADO (para confirmar antes de transferir).
    /// Acepta "987654321", "987 654 321" o "+51987654321". No revela el número de cuenta.
    /// </summary>
    [HttpGet("telefono/{telefono}")]
    [EnableRateLimiting(LimitesDeUso.BusquedaDestinatario)]
    [ProducesResponseType(typeof(DestinatarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<DestinatarioResponse>> BuscarPorTelefono(string telefono) =>
        Ok(await _servicio.BuscarDestinatarioPorTelefonoAsync(telefono));

    /// <summary>Historial de transacciones paginado. Filtro opcional por tipo.</summary>
    [HttpGet("{id:guid}/transacciones")]
    [ProducesResponseType(typeof(PaginaResponse<TransaccionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginaResponse<TransaccionResponse>>> Historial(
        Guid id,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 10,
        [FromQuery] TipoTransaccion? tipo = null)
    {
        User.ExigirBilletera(id);
        return Ok(await _servicio.ObtenerHistorialAsync(id, pagina, tamano, tipo));
    }
}
