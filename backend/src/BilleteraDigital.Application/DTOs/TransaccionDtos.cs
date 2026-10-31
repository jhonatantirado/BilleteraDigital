namespace BilleteraDigital.Application.DTOs;

public record RecargaRequest(Guid BilleteraId, decimal Monto);

public record TransferenciaRequest(
    Guid BilleteraOrigenId,
    string NumeroCuentaDestino,
    decimal Monto,
    string? Descripcion);

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
    string CuentaDestino,
    string NombreDestinatario,
    DateTime Fecha);

public record ComisionResponse(decimal Monto, decimal Comision, decimal Total, string Estrategia);

public record PaginaResponse<T>(IReadOnlyList<T> Items, int Pagina, int TamanoPagina, int Total)
{
    public int TotalPaginas => (int)Math.Ceiling(Total / (double)TamanoPagina);
}
