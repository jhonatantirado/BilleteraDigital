import 'package:billetera_app/models/puntos.dart';
import 'package:billetera_app/models/transaccion.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('TipoTransaccion', () {
    test('reconoce el tipo Retiro que envía la API', () {
      expect(TipoTransaccion.desdeApi('Retiro'), TipoTransaccion.retiro);
    });

    test('solo recargas y transferencias recibidas son ingresos', () {
      expect(TipoTransaccion.recarga.esIngreso, isTrue);
      expect(TipoTransaccion.transferenciaRecibida.esIngreso, isTrue);
      expect(TipoTransaccion.transferenciaEnviada.esIngreso, isFalse);
      expect(TipoTransaccion.retiro.esIngreso, isFalse);
    });
  });

  test('Puntos.fromJson lee el total y los movimientos', () {
    final p = Puntos.fromJson({
      'total': 150,
      'ultimos': [
        {
          'tipoOperacion': 'TransferenciaEnviada',
          'monto': 1000,
          'puntos': 100,
          'referencia': '00000000-0000-0000-0000-000000000001',
          'fecha': '2026-11-02T12:00:00Z'
        }
      ]
    });
    expect(p.total, 150);
    expect(p.ultimos.single.puntos, 100);
  });
}
