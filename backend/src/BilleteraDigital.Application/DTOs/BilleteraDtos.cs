namespace BilleteraDigital.Application.DTOs;

public record BilleteraResponse(
    Guid Id,
    string NumeroCuenta,
    decimal Saldo,
    string Moneda,
    DateTime FechaCreacion);

/// <summary>
/// Datos públicos mínimos del destinatario (para confirmar antes de transferir).
/// Si se buscó por celular, NO se devuelve el número de cuenta, solo el celular enmascarado.
/// </summary>
public record DestinatarioResponse(string? NumeroCuenta, string NombreTitular, string? Telefono = null);
