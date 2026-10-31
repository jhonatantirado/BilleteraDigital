import '../core/api_client.dart';
import '../models/cliente.dart';

/// Parte del MODELO: encapsula los endpoints REST de /clientes.
/// Los controladores no conocen URLs ni JSON, solo objetos Dart.
class ClienteService {
  final ApiClient _api = ApiClient(); // ← siempre la misma instancia (Singleton)

  Future<Cliente> registrar({
    required String nombres,
    required String apellidos,
    required String dni,
    required String email,
    required String telefono,
    required String password,
  }) async {
    final json = await _api.post('/clientes', {
      'nombres': nombres,
      'apellidos': apellidos,
      'dni': dni,
      'email': email,
      'telefono': telefono,
      'password': password,
    });
    return Cliente.fromJson(json as Map<String, dynamic>);
  }

  Future<Cliente> login(String email, String password) async {
    final json = await _api
        .post('/clientes/login', {'email': email, 'password': password});
    return Cliente.fromJson(json as Map<String, dynamic>);
  }
}
