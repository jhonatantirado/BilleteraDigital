import '../core/api_client.dart';
import '../models/cliente.dart';
import '../models/puntos.dart';

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
    return _iniciarSesion(json);
  }

  Future<Cliente> login(String email, String password) async {
    final json = await _api
        .post('/clientes/login', {'email': email, 'password': password});
    return _iniciarSesion(json);
  }

  /// Pide un código nuevo por SMS (simulado: aparece en la consola de la API).
  Future<void> enviarCodigoTelefono(String clienteId) async =>
      _api.post('/clientes/$clienteId/telefono/codigo', {});

  /// Verifica el celular con el código de 6 dígitos; devuelve el cliente actualizado.
  Future<Cliente> verificarTelefono(String clienteId, String codigo) async =>
      Cliente.fromJson(await _api.post(
              '/clientes/$clienteId/telefono/verificar', {'codigo': codigo})
          as Map<String, dynamic>);

  Future<Puntos> puntos(String clienteId) async => Puntos.fromJson(
      await _api.get('/clientes/$clienteId/puntos') as Map<String, dynamic>);

  /// La API responde { token, expira, cliente }: se guarda el token en el
  /// ApiClient (Singleton) para que todas las peticiones siguientes lo envíen.
  Cliente _iniciarSesion(dynamic json) {
    final mapa = json as Map<String, dynamic>;
    _api.establecerToken(mapa['token'] as String);
    return Cliente.fromJson(mapa['cliente'] as Map<String, dynamic>);
  }
}
