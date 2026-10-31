enum TipoTransaccion {
  recarga('Recarga', 'Recarga'),
  transferenciaEnviada('TransferenciaEnviada', 'Enviada'),
  transferenciaRecibida('TransferenciaRecibida', 'Recibida');

  /// Valor exacto que usa la API
  final String valorApi;
  final String etiqueta;
  const TipoTransaccion(this.valorApi, this.etiqueta);

  static TipoTransaccion desdeApi(String valor) =>
      TipoTransaccion.values.firstWhere((t) => t.valorApi == valor);

  bool get esIngreso => this != TipoTransaccion.transferenciaEnviada;
}

class Transaccion {
  final String id;
  final TipoTransaccion tipo;
  final double monto;
  final double comision;
  final double saldoResultante;
  final String descripcion;
  final String? cuentaContraparte;
  final String? nombreContraparte;
  final String referencia;
  final DateTime fecha;

  const Transaccion({
    required this.id,
    required this.tipo,
    required this.monto,
    required this.comision,
    required this.saldoResultante,
    required this.descripcion,
    required this.cuentaContraparte,
    required this.nombreContraparte,
    required this.referencia,
    required this.fecha,
  });

  factory Transaccion.fromJson(Map<String, dynamic> json) => Transaccion(
        id: json['id'] as String,
        tipo: TipoTransaccion.desdeApi(json['tipo'] as String),
        monto: (json['monto'] as num).toDouble(),
        comision: (json['comision'] as num).toDouble(),
        saldoResultante: (json['saldoResultante'] as num).toDouble(),
        descripcion: json['descripcion'] as String? ?? '',
        cuentaContraparte: json['cuentaContraparte'] as String?,
        nombreContraparte: json['nombreContraparte'] as String?,
        referencia: json['referencia'] as String,
        fecha: DateTime.parse(json['fecha'] as String).toLocal(),
      );
}

/// Página de resultados devuelta por GET /billeteras/{id}/transacciones
class PaginaTransacciones {
  final List<Transaccion> items;
  final int pagina;
  final int totalPaginas;
  final int total;

  const PaginaTransacciones(
      this.items, this.pagina, this.totalPaginas, this.total);

  factory PaginaTransacciones.fromJson(Map<String, dynamic> json) =>
      PaginaTransacciones(
        (json['items'] as List)
            .map((e) => Transaccion.fromJson(e as Map<String, dynamic>))
            .toList(),
        json['pagina'] as int,
        json['totalPaginas'] as int,
        json['total'] as int,
      );
}
