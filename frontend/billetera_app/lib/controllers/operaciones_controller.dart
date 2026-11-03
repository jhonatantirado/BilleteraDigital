import 'dart:async';

import 'package:flutter/foundation.dart';

import '../core/api_exception.dart';
import '../models/operaciones.dart';
import '../services/billetera_service.dart';
import 'sesion_controller.dart';

/// Controlador de las operaciones que mueven dinero: recarga y transferencia.
class OperacionesController extends ChangeNotifier {
  OperacionesController(this._sesion, {BilleteraService? servicio})
      : _servicio = servicio ?? BilleteraService();

  final SesionController _sesion;
  final BilleteraService _servicio;

  Destinatario? destinatario;
  Comision? comision;
  bool buscandoDestinatario = false;
  bool calculandoComision = false;
  bool procesando = false;
  String? error;

  Timer? _debounce;

  String get _billeteraId => _sesion.cliente!.billetera.id;

  // -------------------------------------------------------- Transferencia
  Future<void> buscarDestinatario(String numeroCuenta) async {
    buscandoDestinatario = true;
    destinatario = null;
    error = null;
    notifyListeners();
    try {
      if (numeroCuenta == _sesion.cliente?.billetera.numeroCuenta) {
        throw const ApiException(422, 'Esa es tu propia cuenta.');
      }
      destinatario = await _servicio.buscarDestinatario(numeroCuenta);
    } on ApiException catch (e) {
      error = e.mensaje;
    } finally {
      buscandoDestinatario = false;
      notifyListeners();
    }
  }

  /// Consulta la comisión al backend (que aplica el patrón Strategy).
  /// Se usa "debounce" para no llamar a la API en cada tecla.
  void previsualizarComision(double? monto) {
    _debounce?.cancel();
    if (monto == null || monto < 1 || monto > 10000) {
      comision = null;
      notifyListeners();
      return;
    }
    _debounce = Timer(const Duration(milliseconds: 450), () async {
      calculandoComision = true;
      notifyListeners();
      try {
        comision = await _servicio.calcularComision(monto);
      } on ApiException {
        comision = null;
      } finally {
        calculandoComision = false;
        notifyListeners();
      }
    });
  }

  Future<ResultadoTransferencia?> transferir(double monto, String? descripcion) async {
    final destino = destinatario;
    if (destino == null) return null;
    return _procesar(() async {
      final r = await _servicio.transferir(
        billeteraOrigenId: _billeteraId,
        numeroCuentaDestino: destino.numeroCuenta,
        monto: monto,
        descripcion: (descripcion?.trim().isEmpty ?? true) ? null : descripcion!.trim(),
      );
      _sesion.actualizarSaldo(r.saldoActual);
      return r;
    });
  }

  // -------------------------------------------------------------- Recarga
  Future<bool> recargar(double monto) async {
    final r = await _procesar(() async {
      final t = await _servicio.recargar(_billeteraId, monto);
      _sesion.actualizarSaldo(t.saldoResultante);
      return t;
    });
    return r != null;
  }

  // --------------------------------------------------------------- Retiro
  Future<bool> retirar(double monto, String? descripcion) async {
    final r = await _procesar(() async {
      final t = await _servicio.retirar(_billeteraId, monto,
          descripcion: (descripcion?.trim().isEmpty ?? true) ? null : descripcion!.trim());
      _sesion.actualizarSaldo(t.saldoResultante);
      return t;
    });
    return r != null;
  }

  void reiniciar() {
    _debounce?.cancel();
    destinatario = null;
    comision = null;
    error = null;
    procesando = false;
    notifyListeners();
  }

  Future<T?> _procesar<T>(Future<T> Function() accion) async {
    procesando = true;
    error = null;
    notifyListeners();
    try {
      return await accion();
    } on ApiException catch (e) {
      error = e.mensaje;
      return null;
    } finally {
      procesando = false;
      notifyListeners();
    }
  }

  @override
  void dispose() {
    _debounce?.cancel();
    super.dispose();
  }
}
