import 'package:billetera_app/models/operaciones.dart';
import 'package:billetera_app/utils/formato.dart';
import 'package:billetera_app/utils/validadores.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('Validadores.celular', () {
    test('acepta las variantes habituales y las normaliza a 9 dígitos', () {
      for (final v in ['987654321', '987 654 321', '+51 987-654-321', '51987654321']) {
        expect(Validadores.celular(v), isNull, reason: v);
        expect(Validadores.normalizarCelular(v), '987654321', reason: v);
      }
    });

    test('rechaza celulares inválidos', () {
      for (final v in ['812345678', '98765432', '', 'abc']) {
        expect(Validadores.celular(v), isNotNull, reason: v);
      }
    });

    test('el código de verificación tiene 6 dígitos', () {
      expect(Validadores.codigoVerificacion('123456'), isNull);
      expect(Validadores.codigoVerificacion('12345'), isNotNull);
    });
  });

  test('Formato.celular agrupa de 3 en 3', () {
    expect(Formato.celular('+51987654321'), '987 654 321');
    expect(Formato.celular('987654321'), '987 654 321');
  });

  test('un destinatario buscado por celular no trae número de cuenta', () {
    final d = Destinatario.fromJson(
        {'numeroCuenta': null, 'nombreTitular': 'Luis T.', 'telefono': '*** *** 678'});
    expect(d.porCelular, isTrue);
    expect(d.telefono, '*** *** 678');

    final c = Destinatario.fromJson({'numeroCuenta': '1900000002', 'nombreTitular': 'Luis T.'});
    expect(c.porCelular, isFalse);
  });
}
