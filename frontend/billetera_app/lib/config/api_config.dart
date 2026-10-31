import 'package:flutter/foundation.dart';

/// Configuración de la URL base de la API REST.
///
/// Puede sobreescribirse al ejecutar:
///   flutter run --dart-define=API_URL=http://192.168.1.50:5000/api
class ApiConfig {
  ApiConfig._();

  static const String _desdeEntorno = String.fromEnvironment('API_URL');

  static String get baseUrl {
    if (_desdeEntorno.isNotEmpty) return _desdeEntorno;
    if (kIsWeb) return 'http://localhost:5000/api';
    // El emulador de Android ve al "localhost" de la PC como 10.0.2.2
    if (defaultTargetPlatform == TargetPlatform.android) {
      return 'http://10.0.2.2:5000/api';
    }
    return 'http://localhost:5000/api';
  }

  static const Duration timeout = Duration(seconds: 15);
}
