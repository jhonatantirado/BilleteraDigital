using BilleteraDigital.Api.Configuracion;
using BilleteraDigital.Api.Filtros;
using BilleteraDigital.Application.DTOs;
using BilleteraDigital.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BilleteraDigital.Api.Controllers;

[ApiController]
[Route("api/transacciones")]
[Produces("application/json")]
public class TransaccionesController : ControllerBase
{
    private readonly IBilleteraService _servicio;
    public TransaccionesController(IBilleteraService servicio) => _servicio = servicio;

    /// <summary>Recarga saldo a una billetera (simula un abono desde tarjeta/banco).</summary>
    [HttpPost("recargas")]
    [Idempotente]
    [ProducesResponseType(typeof(TransaccionResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<TransaccionResponse>> Recargar([FromBody] RecargaRequest request)
    {
        // La billetera a recargar sale del token, no del body
        var resultado = await _servicio.RecargarAsync(request with { BilleteraId = User.BilleteraId() });
        return StatusCode(StatusCodes.Status201Created, resultado);
    }

    /// <summary>Retira saldo de la billetera (simula un abono a cuenta bancaria).</summary>
    [HttpPost("retiros")]
    [Idempotente]
    [ProducesResponseType(typeof(TransaccionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransaccionResponse>> Retirar([FromBody] RetiroRequest request)
    {
        // La billetera a debitar sale del token, no del body
        var resultado = await _servicio.RetirarAsync(request with { BilleteraId = User.BilleteraId() });
        return StatusCode(StatusCodes.Status201Created, resultado);
    }

    /// <summary>Transfiere dinero a otra billetera por número de cuenta.</summary>
    [HttpPost("transferencias")]
    [Idempotente]
    [ProducesResponseType(typeof(TransferenciaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransferenciaResponse>> Transferir([FromBody] TransferenciaRequest request)
    {
        // El origen SIEMPRE es la billetera del usuario autenticado
        var resultado = await _servicio.TransferirAsync(request with { BilleteraOrigenId = User.BilleteraId() });
        return StatusCode(StatusCodes.Status201Created, resultado);
    }

    /// <summary>
    /// Previsualiza la comisión que se cobraría al usuario autenticado
    /// (depende del monto y de su antigüedad: patrón Strategy en acción).
    /// </summary>
    [HttpGet("comision")]
    [ProducesResponseType(typeof(ComisionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ComisionResponse>> Comision([FromQuery] decimal monto) =>
        Ok(await _servicio.CalcularComisionAsync(User.BilleteraId(), monto));
}
