/// Validaciones de formularios en el cliente (la API vuelve a validar todo:
/// nunca se confía solo en el frontend).
class Validadores {
  Validadores._();

  static String? requerido(String? v, [String campo = 'Este campo']) =>
      (v == null || v.trim().isEmpty) ? '$campo es obligatorio' : null;

  static String? dni(String? v) =>
      RegExp(r'^\d{8}$').hasMatch(v ?? '') ? null : 'El DNI debe tener 8 dígitos';

  static String? email(String? v) =>
      RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(v?.trim() ?? '')
          ? null
          : 'Email no válido';

  static String? password(String? v) =>
      (v ?? '').length >= 8 ? null : 'Mínimo 8 caracteres';

  static String? cuenta(String? v) => RegExp(r'^\d{10}$').hasMatch(v ?? '')
      ? null
      : 'La cuenta tiene 10 dígitos';

  /// Celular peruano: 9 dígitos que empiezan con 9. Acepta espacios, guiones
  /// y el prefijo +51 ("987 654 321", "+51 987654321").
  static String? celular(String? v) =>
      normalizarCelular(v ?? '') == null
          ? 'Ingresa un celular de 9 dígitos que empiece con 9'
          : null;

  /// "+51 987-654-321" → "987654321"; null si no es un celular válido.
  static String? normalizarCelular(String v) {
    var d = v.replaceAll(RegExp(r'\D'), '');
    if (d.length == 11 && d.startsWith('51')) d = d.substring(2);
    return RegExp(r'^9\d{8}$').hasMatch(d) ? d : null;
  }

  static String? codigoVerificacion(String? v) =>
      RegExp(r'^\d{6}$').hasMatch(v?.trim() ?? '') ? null : 'El código tiene 6 dígitos';

  static String? monto(String? v, {double minimo = 1, double maximo = 10000}) {
    final m = double.tryParse((v ?? '').replaceAll(',', '.'));
    if (m == null) return 'Ingrese un monto válido';
    if (m < minimo || m > maximo) {
      return 'Entre S/ ${minimo.toStringAsFixed(2)} y S/ ${maximo.toStringAsFixed(2)}';
    }
    if (!RegExp(r'^\d+([.,]\d{1,2})?$').hasMatch(v!.trim())) {
      return 'Máximo 2 decimales';
    }
    return null;
  }

  static double aMonto(String v) => double.parse(v.trim().replaceAll(',', '.'));
}
