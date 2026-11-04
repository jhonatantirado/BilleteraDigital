import 'package:flutter/foundation.dart';

import '../core/api_client.dart';
import '../core/api_exception.dart';
import '../models/cliente.dart';
import '../models/puntos.dart';
import '../services/billetera_service.dart';
import '../services/cliente_service.dart';

/// "C" de MVC: coordina las Vistas de login/registro con el Modelo.
/// - Recibe las acciones del usuario (login, registrar, salir).
/// - Llama al Modelo (servicios REST).
/// - Actualiza su estado y avisa a las Vistas con notifyListeners().
/// Las Vistas NUNCA llaman directamente a la API.
class SesionController extends ChangeNotifier {
  SesionController({ClienteService? clientes, BilleteraService? billeteras})
      : _clientes = clientes ?? ClienteService(),
        _billeteras = billeteras ?? BilleteraService() {
    // Si el servidor rechaza el token (401), se vuelve a la pantalla de login
    ApiClient().alNoAutorizado = cerrarSesion;
  }

  final ClienteService _clientes;
  final BilleteraService _billeteras;

  Cliente? _cliente;
  Puntos? _puntos;
  bool _cargando = false;
  String? _error;

  Cliente? get cliente => _cliente;
  Puntos? get puntos => _puntos;
  bool get autenticado => _cliente != null;
  bool get cargando => _cargando;
  String? get error => _error;

  Future<bool> login(String email, String password) async {
    final ok = await _ejecutar(
        () async => _cliente = await _clientes.login(email, password));
    if (ok) await refrescarPuntos();
    return ok;
  }

  Future<bool> registrar({
    required String nombres,
    required String apellidos,
    required String dni,
    required String email,
    required String telefono,
    required String password,
  }) async {
    final ok = await _ejecutar(() async => _cliente = await _clientes.registrar(
          nombres: nombres,
          apellidos: apellidos,
          dni: dni,
          email: email,
          telefono: telefono,
          password: password,
        ));
    if (ok) await refrescarPuntos();
    return ok;
  }

  /// Verifica el celular con el código recibido por SMS.
  Future<bool> verificarTelefono(String codigo) {
    final actual = _cliente;
    if (actual == null) return Future.value(false);
    return _ejecutar(() async =>
        _cliente = await _clientes.verificarTelefono(actual.id, codigo.trim()));
  }

  /// Pide un código nuevo (la API exige 60 s entre envíos).
  Future<bool> reenviarCodigoTelefono() {
    final actual = _cliente;
    if (actual == null) return Future.value(false);
    return _ejecutar(() => _clientes.enviarCodigoTelefono(actual.id));
  }

  /// Consulta los puntos de fidelidad (los otorga el backend con un Observer).
  /// Si falla, no se muestra error: los puntos son información secundaria.
  Future<void> refrescarPuntos() async {
    final actual = _cliente;
    if (actual == null) return;
    try {
      _puntos = await _clientes.puntos(actual.id);
      notifyListeners();
    } on ApiException {
      // se conserva el valor anterior
    }
  }

  /// Vuelve a consultar el saldo en el servidor.
  Future<void> refrescarBilletera() async {
    final actual = _cliente;
    if (actual == null) return;
    try {
      final billetera = await _billeteras.obtener(actual.billetera.id);
      _cliente = actual.conBilletera(billetera);
      notifyListeners();
      await refrescarPuntos();
    } on ApiException catch (e) {
      _error = e.mensaje;
      notifyListeners();
    }
  }

  /// Actualiza el saldo localmente tras una operación exitosa.
  void actualizarSaldo(double nuevoSaldo) {
    final actual = _cliente;
    if (actual == null) return;
    _cliente = actual.conBilletera(actual.billetera.copiarConSaldo(nuevoSaldo));
    notifyListeners();
  }

  void cerrarSesion() {
    ApiClient().establecerToken(null);
    _cliente = null;
    _puntos = null;
    _error = null;
    notifyListeners();
  }

  void limpiarError() {
    _error = null;
    notifyListeners();
  }

  Future<bool> _ejecutar(Future<void> Function() accion) async {
    _cargando = true;
    _error = null;
    notifyListeners();
    try {
      await accion();
      return true;
    } on ApiException catch (e) {
      _error = e.mensaje;
      return false;
    } finally {
      _cargando = false;
      notifyListeners();
    }
  }
}
