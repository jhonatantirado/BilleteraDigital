using BilleteraDigital.Application.DTOs;
using BilleteraDigital.Domain.Entidades;

namespace BilleteraDigital.Application.Servicios;

/// <summary>Convierte entidades de dominio en DTOs de respuesta.</summary>
internal static class Mapeador
{
    public static BilleteraResponse ABilleteraResponse(Billetera b) =>
        new(b.Id, b.NumeroCuenta, b.Saldo, b.Moneda, b.FechaCreacion);

    public static ClienteResponse AClienteResponse(Cliente c, Billetera b) =>
        new(c.Id, c.Nombres, c.Apellidos, c.Dni, c.Email, c.Telefono, c.FechaRegistro,
            ABilleteraResponse(b), c.TelefonoVerificado);

    /// <summary>Nombre que ve quien busca a un destinatario: "Luis T." (privacidad).</summary>
    public static string NombrePublico(Cliente? c) =>
        c is null ? "—" : $"{c.Nombres} {(c.Apellidos.Length > 0 ? c.Apellidos[..1] + "." : "")}".Trim();

    public static TransaccionResponse ATransaccionResponse(Transaccion t) =>
        new(t.Id,
            t.Tipo.ToString(),
            t.Monto,
            t.Comision,
            t.SaldoResultante,
            t.Descripcion,
            t.BilleteraContraparte?.NumeroCuenta,
            t.BilleteraContraparte?.Cliente?.NombreCompleto,
            t.Referencia,
            t.Fecha);
}
