/// Utilidades de formato (sin dependencias externas).
class Formato {
  Formato._();

  /// 1234.5 → "S/ 1,234.50"
  static String soles(double monto) {
    final negativo = monto < 0;
    final partes = monto.abs().toStringAsFixed(2).split('.');
    final entero = partes[0].replaceAllMapped(
        RegExp(r'(\d)(?=(\d{3})+$)'), (m) => '${m[1]},');
    return '${negativo ? '-' : ''}S/ $entero.${partes[1]}';
  }

  static String _dos(int n) => n.toString().padLeft(2, '0');

  /// 27/10/2026 14:05
  static String fechaHora(DateTime f) =>
      '${_dos(f.day)}/${_dos(f.month)}/${f.year} ${_dos(f.hour)}:${_dos(f.minute)}';

  /// "+51987654321" o "987654321" → "987 654 321"
  static String celular(String n) {
    final d = n.replaceAll(RegExp(r'\D'), '');
    final nueve = d.length >= 9 ? d.substring(d.length - 9) : d;
    return nueve.length == 9
        ? '${nueve.substring(0, 3)} ${nueve.substring(3, 6)} ${nueve.substring(6)}'
        : n;
  }

  /// 1900000001 → "1900 0000 01"
  static String cuenta(String n) => n.length == 10
      ? '${n.substring(0, 4)} ${n.substring(4, 8)} ${n.substring(8)}'
      : n;
}
