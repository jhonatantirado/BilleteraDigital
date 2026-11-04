using BilleteraDigital.Api.Configuracion;
using BilleteraDigital.Application.DTOs;
using BilleteraDigital.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilleteraDigital.Api.Controllers;

/// <summary>
/// "C" de MVC: recibe la petición HTTP, delega al servicio de aplicación
/// y devuelve la "Vista", que en una API REST es la representación JSON del Modelo (DTO).
/// El controlador NO contiene reglas de negocio ni acceso a datos.
/// </summary>
[ApiController]
[Route("api/clientes")]
[Produces("application/json")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _servicio;
    private readonly IVerificacionTelefonoService _verificacion;

    public ClientesController(IClienteService servicio, IVerificacionTelefonoService verificacion)
    {
        _servicio = servicio;
        _verificacion = verificacion;
    }

    /// <summary>Registra un cliente, le abre una billetera en soles y devuelve su token.</summary>
    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AuthResponse>> Registrar([FromBody] RegistrarClienteRequest request)
    {
        var resultado = await _servicio.RegistrarAsync(request);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.Cliente.Id }, resultado);
    }

    /// <summary>Inicia sesión con email y contraseña; devuelve un token JWT.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request) =>
        Ok(await _servicio.LoginAsync(request));

    /// <summary>Obtiene el cliente autenticado con su billetera.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteResponse>> ObtenerPorId(Guid id)
    {
        User.ExigirCliente(id);
        return Ok(await _servicio.ObtenerPorIdAsync(id));
    }

    /// <summary>Puntos de fidelidad del cliente autenticado (total y últimos movimientos).</summary>
    [HttpGet("{id:guid}/puntos")]
    [ProducesResponseType(typeof(PuntosResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PuntosResponse>> Puntos(Guid id)
    {
        User.ExigirCliente(id);
        return Ok(await _servicio.ObtenerPuntosAsync(id));
    }

    /// <summary>
    /// Envía (o reenvía) el código de verificación por SMS. En el curso el SMS es
    /// simulado: el código aparece en la consola de la API y en logs/sms_simulados.log.
    /// </summary>
    [HttpPost("{id:guid}/telefono/codigo")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EnviarCodigo(Guid id)
    {
        User.ExigirCliente(id);
        await _verificacion.EnviarCodigoAsync(id);
        return Accepted();
    }

    /// <summary>Verifica el celular con el código de 6 dígitos recibido por SMS.</summary>
    [HttpPost("{id:guid}/telefono/verificar")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClienteResponse>> VerificarTelefono(Guid id, [FromBody] VerificarTelefonoRequest request)
    {
        User.ExigirCliente(id);
        return Ok(await _verificacion.VerificarAsync(id, request.Codigo));
    }
}
