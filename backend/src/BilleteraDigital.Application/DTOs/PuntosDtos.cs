namespace BilleteraDigital.Application.DTOs;

public record MovimientoPuntosResponse(string TipoOperacion, decimal Monto, int Puntos, Guid Referencia, DateTime Fecha);

/// <summary>Total de puntos del cliente y sus últimos movimientos.</summary>
public record PuntosResponse(int Total, IReadOnlyList<MovimientoPuntosResponse> Ultimos);
