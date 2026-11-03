/// Puntos de fidelidad: total y últimos movimientos (GET /clientes/{id}/puntos).
class Puntos {
  final int total;
  final List<MovimientoPuntos> ultimos;

  const Puntos(this.total, this.ultimos);

  factory Puntos.fromJson(Map<String, dynamic> json) => Puntos(
        json['total'] as int,
        (json['ultimos'] as List)
            .map((e) => MovimientoPuntos.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class MovimientoPuntos {
  final String tipoOperacion;
  final double monto;
  final int puntos;
  final DateTime fecha;

  const MovimientoPuntos(this.tipoOperacion, this.monto, this.puntos, this.fecha);

  factory MovimientoPuntos.fromJson(Map<String, dynamic> json) => MovimientoPuntos(
        json['tipoOperacion'] as String,
        (json['monto'] as num).toDouble(),
        json['puntos'] as int,
        DateTime.parse(json['fecha'] as String).toLocal(),
      );
}
