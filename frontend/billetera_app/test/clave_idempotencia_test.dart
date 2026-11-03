import 'package:billetera_app/core/api_exception.dart';
import 'package:billetera_app/core/clave_idempotencia.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('genera UUID versión 4 distintos', () {
    final a = ClaveIdempotencia.generar();
    final b = ClaveIdempotencia.generar();
    expect(a, isNot(b));
    expect(
        RegExp(r'^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$')
            .hasMatch(a),
        isTrue);
  });

  test('misma firma = misma clave (el reintento no duplica)', () {
    final c = ClaveIdempotencia();
    expect(c.para('1900000002|500.00|'), c.para('1900000002|500.00|'));
  });

  test('si cambian los datos, la clave es nueva', () {
    final c = ClaveIdempotencia();
    final primera = c.para('1900000002|500.00|');
    expect(c.para('1900000002|600.00|'), isNot(primera));
  });

  test('tras descartar se genera una clave nueva', () {
    final c = ClaveIdempotencia();
    final primera = c.para('x');
    c.descartar();
    expect(c.para('x'), isNot(primera));
  });

  test('solo se conserva la clave cuando no se sabe si la operación se hizo', () {
    expect(ClaveIdempotencia.conservarTras(const ApiException(0, 'timeout')), isTrue);
    expect(ClaveIdempotencia.conservarTras(const ApiException(409, 'en curso')), isTrue);
    expect(ClaveIdempotencia.conservarTras(const ApiException(503, 'caído')), isTrue);
    expect(ClaveIdempotencia.conservarTras(const ApiException(422, 'saldo')), isFalse);
  });
}
