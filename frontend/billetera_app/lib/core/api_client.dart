import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';
import 'api_exception.dart';

/// PATRÓN SINGLETON (creacional - GoF)
///
/// Problema: toda la app necesita hablar con la API REST. Si cada pantalla
/// creara su propio cliente HTTP se duplicarían conexiones, configuración
/// (URL base, cabeceras, timeout) y manejo de errores.
///
/// Solución: una ÚNICA instancia global de [ApiClient]:
///   1. El constructor real es privado (`ApiClient._interno`).
///   2. La instancia se guarda en un campo `static final`.
///   3. Un constructor `factory` devuelve SIEMPRE esa misma instancia.
///
///   final a = ApiClient();
///   final b = ApiClient();
///   identical(a, b) == true   // ✔ es el mismo objeto
class ApiClient {
  // (2) única instancia, creada de forma perezosa la primera vez que se usa
  static final ApiClient _instancia = ApiClient._interno();

  // (1) constructor privado: nadie fuera de esta clase puede hacer "new"
  ApiClient._interno() : _http = http.Client();

  // (3) punto de acceso global
  factory ApiClient() => _instancia;

  final http.Client _http;
  final String _baseUrl = ApiConfig.baseUrl;

  static const Map<String, String> _cabeceras = {
    'Content-Type': 'application/json; charset=utf-8',
    'Accept': 'application/json',
  };

  Future<dynamic> get(String ruta, {Map<String, String>? query}) {
    final uri = Uri.parse('$_baseUrl$ruta').replace(queryParameters: query);
    return _ejecutar(() => _http.get(uri, headers: _cabeceras));
  }

  Future<dynamic> post(String ruta, Map<String, dynamic> cuerpo) {
    final uri = Uri.parse('$_baseUrl$ruta');
    return _ejecutar(
        () => _http.post(uri, headers: _cabeceras, body: jsonEncode(cuerpo)));
  }

  /// Centraliza timeout, decodificación JSON y traducción de errores HTTP.
  Future<dynamic> _ejecutar(Future<http.Response> Function() peticion) async {
    final http.Response respuesta;
    try {
      respuesta = await peticion().timeout(ApiConfig.timeout);
    } on TimeoutException {
      throw const ApiException(0, 'El servidor tardó demasiado en responder.');
    } on http.ClientException {
      throw const ApiException(0,
          'No se pudo conectar con el servidor. ¿Está corriendo la API?');
    }

    final texto = utf8.decode(respuesta.bodyBytes);
    final dynamic json = texto.isEmpty ? null : jsonDecode(texto);

    if (respuesta.statusCode >= 200 && respuesta.statusCode < 300) {
      return json;
    }

    // La API devuelve errores con formato ProblemDetails: { title, status, detail }
    String mensaje = 'Error ${respuesta.statusCode}';
    if (json is Map<String, dynamic>) {
      if (json['detail'] != null) {
        mensaje = json['detail'].toString();
      } else if (json['errors'] is Map) {
        // Errores de validación automática de ASP.NET ([ApiController])
        mensaje = (json['errors'] as Map)
            .values
            .expand((e) => e is List ? e : [e])
            .join('\n');
      } else if (json['title'] != null) {
        mensaje = json['title'].toString();
      }
    }
    throw ApiException(respuesta.statusCode, mensaje);
  }
}
