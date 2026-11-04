namespace BilleteraDigital.Application.DTOs;

// DTO (Data Transfer Object): lo que viaja por la API REST.
// Nunca exponemos las entidades de dominio directamente (p. ej. PasswordHash).

public record RegistrarClienteRequest(
    string Nombres,
    string Apellidos,
    string Dni,
    string Email,
    string Telefono,
    string Password);

public record LoginRequest(string Email, string Password);

/// <summary>Respuesta de login/registro: token JWT + datos del cliente.</summary>
public record AuthResponse(string Token, DateTime Expira, ClienteResponse Cliente);

public record ClienteResponse(
    Guid Id,
    string Nombres,
    string Apellidos,
    string Dni,
    string Email,
    string Telefono,
    DateTime FechaRegistro,
    BilleteraResponse Billetera,
    bool TelefonoVerificado);

/// <summary>Código de 6 dígitos recibido por SMS.</summary>
public record VerificarTelefonoRequest(string Codigo);
