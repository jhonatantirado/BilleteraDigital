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
