import 'dart:async';

import 'package:flutter/foundation.dart';

import '../core/api_exception.dart';
import '../core/clave_idempotencia.dart';
import '../models/operaciones.dart';
import '../services/billetera_service.dart';
import '../utils/validadores.dart';
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

  /// Celular (9 dígitos) con el que se encontró al destinatario, si se buscó por celular.
  String? _celularDestino;

  // Una clave de idempotencia por tipo de operación (ver ClaveIdempotencia)
  final _claveRecarga = ClaveIdempotencia();
  final _claveRetiro = ClaveIdempotencia();
  final _claveTransferencia = ClaveIdempotencia();

  String get _billeteraId => _sesion.cliente!.billetera.id;

  // -------------------------------------------------------- Transferencia
  /// Busca al destinatario por número de cuenta o por celular.
  Future<void> buscarDestinatario(String valor,
      {ModoBusqueda modo = ModoBusqueda.cuenta}) async {
    buscandoDestinatario = true;
    destinatario = null;
    _celularDestino = null;
    error = null;
    notifyListeners();
    try {
      if (modo == ModoBusqueda.cuenta) {
        if (valor == _sesion.cliente?.billetera.numeroCuenta) {
          throw const ApiException(422, 'Esa es tu propia cuenta.');
        }
        destinatario = await _servicio.buscarDestinatario(valor);
      } else {
        final celular = Validadores.normalizarCelular(valor);
        if (celular == null) {
          throw const ApiException(422, 'Ingresa un celular de 9 dígitos que empiece con 9.');
        }
        if (celular == Validadores.normalizarCelular(_sesion.cliente?.telefono ?? '')) {
          throw const ApiException(422, 'Ese es tu propio celular.');
        }
        destinatario = await _servicio.buscarDestinatarioPorCelular(celular);
        _celularDestino = celular;
      }
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
    final desc = (descripcion?.trim().isEmpty ?? true) ? null : descripcion!.trim();
    final celular = destino.porCelular ? _celularDestino : null;
    final clave = _claveTransferencia.para(
        '${destino.numeroCuenta ?? 'cel:$celular'}|${monto.toStringAsFixed(2)}|${desc ?? ''}');
    return _procesar(() async {
      final r = await _servicio.transferir(
        billeteraOrigenId: _billeteraId,
        numeroCuentaDestino: destino.numeroCuenta,
        telefonoDestino: celular,
        monto: monto,
        descripcion: desc,
        idempotencyKey: clave,
      );
      _sesion.actualizarSaldo(r.saldoActual);
      return r;
    }, clave: _claveTransferencia);
  }

  // -------------------------------------------------------------- Recarga
  Future<bool> recargar(double monto) async {
    final clave = _claveRecarga.para(monto.toStringAsFixed(2));
    final r = await _procesar(() async {
      final t = await _servicio.recargar(_billeteraId, monto, idempotencyKey: clave);
      _sesion.actualizarSaldo(t.saldoResultante);
      return t;
    }, clave: _claveRecarga);
    return r != null;
  }

  // --------------------------------------------------------------- Retiro
  Future<bool> retirar(double monto, String? descripcion) async {
    final desc = (descripcion?.trim().isEmpty ?? true) ? null : descripcion!.trim();
    final clave = _claveRetiro.para('${monto.toStringAsFixed(2)}|${desc ?? ''}');
    final r = await _procesar(() async {
      final t = await _servicio.retirar(_billeteraId, monto,
          descripcion: desc, idempotencyKey: clave);
      _sesion.actualizarSaldo(t.saldoResultante);
      return t;
    }, clave: _claveRetiro);
    return r != null;
  }

  void reiniciar() {
    _debounce?.cancel();
    // Nueva pantalla = nueva intención: las claves anteriores ya no aplican
    _claveRecarga.descartar();
    _claveRetiro.descartar();
    _claveTransferencia.descartar();
    destinatario = null;
    _celularDestino = null;
    comision = null;
    error = null;
    procesando = false;
    notifyListeners();
  }

  /// Ejecuta una operación. Si recibe [clave], decide qué hacer con ella:
  /// se descarta tras una respuesta definitiva y se conserva cuando no se sabe
  /// si la operación se hizo (así el reintento no la duplica).
  Future<T?> _procesar<T>(Future<T> Function() accion,
      {ClaveIdempotencia? clave}) async {
    procesando = true;
    error = null;
    notifyListeners();
    try {
      final resultado = await accion();
      clave?.descartar();
      return resultado;
    } on ApiException catch (e) {
      if (clave != null && ClaveIdempotencia.conservarTras(e)) {
        error = e.statusCode == 409
            ? e.mensaje
            : '${e.mensaje}\nPuedes reintentar sin riesgo: la operación no se duplicará.';
      } else {
        clave?.descartar();
        error = e.mensaje;
      }
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
