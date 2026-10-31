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
    public ClientesController(IClienteService servicio) => _servicio = servicio;

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
}
