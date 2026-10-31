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

public record ClienteResponse(
    Guid Id,
    string Nombres,
    string Apellidos,
    string Dni,
    string Email,
    string Telefono,
    DateTime FechaRegistro,
    BilleteraResponse Billetera);
