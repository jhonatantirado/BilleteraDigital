import 'package:billetera_app/core/api_client.dart';
import 'package:billetera_app/models/transaccion.dart';
import 'package:billetera_app/utils/formato.dart';
import 'package:billetera_app/utils/validadores.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('Patrón Singleton', () {
    test('ApiClient() siempre devuelve la misma instancia', () {
      final a = ApiClient();
      final b = ApiClient();
      expect(identical(a, b), isTrue);
    });
  });

  group('Modelo', () {
    test('Transaccion.fromJson interpreta el JSON de la API', () {
      final t = Transaccion.fromJson({
        'id': '1',
        'tipo': 'TransferenciaEnviada',
        'monto': 1000,
        'comision': 5.0,
        'saldoResultante': 3995.0,
        'descripcion': 'Pago',
        'cuentaContraparte': '1900000002',
        'nombreContraparte': 'Luis Torres',
        'referencia': 'abc',
        'fecha': '2026-10-28T04:47:45Z',
      });
      expect(t.tipo, TipoTransaccion.transferenciaEnviada);
      expect(t.tipo.esIngreso, isFalse);
      expect(t.comision, 5.0);
    });
  });

  group('Utilidades', () {
    test('Formato de soles con separador de miles', () {
      expect(Formato.soles(1234.5), 'S/ 1,234.50');
      expect(Formato.soles(0), 'S/ 0.00');
    });

    test('Validación de montos', () {
      expect(Validadores.monto('100'), isNull);
      expect(Validadores.monto('10.555'), isNotNull);
      expect(Validadores.monto('0.50'), isNotNull);
      expect(Validadores.monto('20000'), isNotNull);
    });
  });
}
