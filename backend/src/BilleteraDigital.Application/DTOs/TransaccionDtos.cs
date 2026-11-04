namespace BilleteraDigital.Application.DTOs;

public record RecargaRequest(Guid BilleteraId, decimal Monto);

/// <summary>Retiro de saldo hacia fuera del sistema (la billetera se toma del token).</summary>
public record RetiroRequest(Guid BilleteraId, decimal Monto, string? Descripcion);

/// <summary>
/// El destino se indica con UNO de los dos: número de cuenta o celular.
/// TelefonoDestino va al final y con valor por defecto para no romper a quien ya usa el DTO.
/// </summary>
public record TransferenciaRequest(
    Guid BilleteraOrigenId,
    string? NumeroCuentaDestino,
    decimal Monto,
    string? Descripcion,
    string? TelefonoDestino = null);

public record TransaccionResponse(
    Guid Id,
    string Tipo,
    decimal Monto,
    decimal Comision,
    decimal SaldoResultante,
    string Descripcion,
    string? CuentaContraparte,
    string? NombreContraparte,
    Guid Referencia,
    DateTime Fecha);

public record TransferenciaResponse(
    Guid Referencia,
    decimal Monto,
    decimal Comision,
    string EstrategiaComision,
    decimal SaldoActual,
    string? CuentaDestino,          // null si se transfirió por celular
    string NombreDestinatario,
    DateTime Fecha,
    string? TelefonoDestino = null); // celular enmascarado, si se transfirió por celular

public record ComisionResponse(decimal Monto, decimal Comision, decimal Total, string Estrategia);

public record PaginaResponse<T>(IReadOnlyList<T> Items, int Pagina, int TamanoPagina, int Total)
{
    public int TotalPaginas => (int)Math.Ceiling(Total / (double)TamanoPagina);
}
