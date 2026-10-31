/// Respuesta de GET /transacciones/comision (patrón Strategy del backend)
class Comision {
  final double monto;
  final double comision;
  final double total;
  final String estrategia;

  const Comision(this.monto, this.comision, this.total, this.estrategia);

  factory Comision.fromJson(Map<String, dynamic> j) => Comision(
        (j['monto'] as num).toDouble(),
        (j['comision'] as num).toDouble(),
        (j['total'] as num).toDouble(),
        j['estrategia'] as String,
      );
}

class Destinatario {
  final String numeroCuenta;
  final String nombreTitular;

  const Destinatario(this.numeroCuenta, this.nombreTitular);

  factory Destinatario.fromJson(Map<String, dynamic> j) =>
      Destinatario(j['numeroCuenta'] as String, j['nombreTitular'] as String);
}

class ResultadoTransferencia {
  final String referencia;
  final double monto;
  final double comision;
  final String estrategiaComision;
  final double saldoActual;
  final String cuentaDestino;
  final String nombreDestinatario;
  final DateTime fecha;

  const ResultadoTransferencia({
    required this.referencia,
    required this.monto,
    required this.comision,
    required this.estrategiaComision,
    required this.saldoActual,
    required this.cuentaDestino,
    required this.nombreDestinatario,
    required this.fecha,
  });

  factory ResultadoTransferencia.fromJson(Map<String, dynamic> j) =>
      ResultadoTransferencia(
        referencia: j['referencia'] as String,
        monto: (j['monto'] as num).toDouble(),
        comision: (j['comision'] as num).toDouble(),
        estrategiaComision: j['estrategiaComision'] as String,
        saldoActual: (j['saldoActual'] as num).toDouble(),
        cuentaDestino: j['cuentaDestino'] as String,
        nombreDestinatario: j['nombreDestinatario'] as String,
        fecha: DateTime.parse(j['fecha'] as String).toLocal(),
      );
}
