/// "M" de MVC: representación en Dart del recurso Billetera de la API.
class Billetera {
  final String id;
  final String numeroCuenta;
  final double saldo;
  final String moneda;

  const Billetera({
    required this.id,
    required this.numeroCuenta,
    required this.saldo,
    required this.moneda,
  });

  factory Billetera.fromJson(Map<String, dynamic> json) => Billetera(
        id: json['id'] as String,
        numeroCuenta: json['numeroCuenta'] as String,
        saldo: (json['saldo'] as num).toDouble(),
        moneda: json['moneda'] as String? ?? 'PEN',
      );

  Billetera copiarConSaldo(double nuevoSaldo) => Billetera(
      id: id, numeroCuenta: numeroCuenta, saldo: nuevoSaldo, moneda: moneda);
}
