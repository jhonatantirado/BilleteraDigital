/// Error devuelto por la API (formato ProblemDetails) o de conexión.
class ApiException implements Exception {
  final int statusCode; // 0 = sin conexión
  final String mensaje;

  const ApiException(this.statusCode, this.mensaje);

  bool get sinConexion => statusCode == 0;

  @override
  String toString() => mensaje;
}
