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
    [ProducesResponseType(typeof(TransaccionResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<TransaccionResponse>> Recargar([FromBody] RecargaRequest request)
    {
        var resultado = await _servicio.RecargarAsync(request);
        return StatusCode(StatusCodes.Status201Created, resultado);
    }

    /// <summary>Transfiere dinero a otra billetera por número de cuenta.</summary>
    [HttpPost("transferencias")]
    [ProducesResponseType(typeof(TransferenciaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransferenciaResponse>> Transferir([FromBody] TransferenciaRequest request)
    {
        var resultado = await _servicio.TransferirAsync(request);
        return StatusCode(StatusCodes.Status201Created, resultado);
    }

    /// <summary>Previsualiza la comisión que se cobraría (muestra el patrón Strategy en acción).</summary>
    [HttpGet("comision")]
    [ProducesResponseType(typeof(ComisionResponse), StatusCodes.Status200OK)]
    public ActionResult<ComisionResponse> Comision([FromQuery] decimal monto) =>
        Ok(_servicio.CalcularComision(monto));
}
