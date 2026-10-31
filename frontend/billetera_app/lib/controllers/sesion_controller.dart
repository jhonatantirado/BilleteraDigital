import 'package:flutter/foundation.dart';

import '../core/api_client.dart';
import '../core/api_exception.dart';
import '../models/cliente.dart';
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
  bool _cargando = false;
  String? _error;

  Cliente? get cliente => _cliente;
  bool get autenticado => _cliente != null;
  bool get cargando => _cargando;
  String? get error => _error;

  Future<bool> login(String email, String password) =>
      _ejecutar(() async => _cliente = await _clientes.login(email, password));

  Future<bool> registrar({
    required String nombres,
    required String apellidos,
    required String dni,
    required String email,
    required String telefono,
    required String password,
  }) =>
      _ejecutar(() async => _cliente = await _clientes.registrar(
            nombres: nombres,
            apellidos: apellidos,
            dni: dni,
            email: email,
            telefono: telefono,
            password: password,
          ));

  /// Vuelve a consultar el saldo en el servidor.
  Future<void> refrescarBilletera() async {
    final actual = _cliente;
    if (actual == null) return;
    try {
      final billetera = await _billeteras.obtener(actual.billetera.id);
      _cliente = actual.conBilletera(billetera);
      notifyListeners();
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
