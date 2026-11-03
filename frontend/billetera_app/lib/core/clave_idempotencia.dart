import 'dart:math';

import 'api_exception.dart';

/// Clave para la cabecera `Idempotency-Key`: UNA por intención del usuario,
/// no una por petición.
///
/// - Mientras los datos de la operación (la "firma") no cambien, devuelve la
///   MISMA clave: si la primera petición se perdió por un timeout, el reintento
///   no duplica el movimiento (el servidor repite la respuesta original).
/// - Si cambian el monto, el destino o la descripción, genera una clave nueva.
/// - Tras una respuesta definitiva (éxito o error de negocio) se descarta.
class ClaveIdempotencia {
  String? _clave;
  String? _firma;

  /// Clave vigente para esta firma (la crea si no existe o si la firma cambió).
  String para(String firma) {
    if (_clave == null || _firma != firma) {
      _clave = generar();
      _firma = firma;
    }
    return _clave!;
  }

  String? get actual => _clave;

  void descartar() {
    _clave = null;
    _firma = null;
  }

  /// ¿Conviene reintentar con la MISMA clave después de este error?
  /// Sí cuando no sabemos si la operación se hizo: sin conexión o timeout (0),
  /// operación en curso (409) o error del servidor (5xx). Un 4xx de negocio
  /// (p. ej. saldo insuficiente) es definitivo: el próximo intento es otra intención.
  static bool conservarTras(ApiException e) =>
      e.statusCode == 0 || e.statusCode == 409 || e.statusCode >= 500;

  /// UUID versión 4 con un generador criptográficamente seguro.
  static String generar() {
    final r = Random.secure();
    final b = List<int>.generate(16, (_) => r.nextInt(256));
    b[6] = (b[6] & 0x0f) | 0x40; // versión 4
    b[8] = (b[8] & 0x3f) | 0x80; // variante RFC 4122
    final h = b.map((x) => x.toRadixString(16).padLeft(2, '0')).join();
    return '${h.substring(0, 8)}-${h.substring(8, 12)}-${h.substring(12, 16)}-'
        '${h.substring(16, 20)}-${h.substring(20)}';
  }
}
