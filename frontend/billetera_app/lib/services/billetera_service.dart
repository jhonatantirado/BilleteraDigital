import '../core/api_client.dart';
import '../models/billetera.dart';
import '../models/operaciones.dart';
import '../models/transaccion.dart';

/// Parte del MODELO: encapsula los endpoints REST de billeteras y transacciones.
class BilleteraService {
  final ApiClient _api = ApiClient(); // ← misma instancia que ClienteService

  Future<Billetera> obtener(String billeteraId) async =>
      Billetera.fromJson(
          await _api.get('/billeteras/$billeteraId') as Map<String, dynamic>);

  Future<Destinatario> buscarDestinatario(String numeroCuenta) async =>
      Destinatario.fromJson(await _api.get('/billeteras/cuenta/$numeroCuenta')
          as Map<String, dynamic>);

  /// [celular] de 9 dígitos. Solo encuentra celulares VERIFICADOS.
  Future<Destinatario> buscarDestinatarioPorCelular(String celular) async =>
      Destinatario.fromJson(await _api.get('/billeteras/telefono/$celular')
          as Map<String, dynamic>);

  Future<Comision> calcularComision(double monto) async =>
      Comision.fromJson(await _api.get('/transacciones/comision',
          query: {'monto': monto.toStringAsFixed(2)}) as Map<String, dynamic>);

  Future<Transaccion> recargar(String billeteraId, double monto,
          {required String idempotencyKey}) async =>
      Transaccion.fromJson(await _api.post('/transacciones/recargas', {
        'billeteraId': billeteraId,
        'monto': _redondear(monto),
      }, cabeceras: _idempotencia(idempotencyKey)) as Map<String, dynamic>);

  Future<Transaccion> retirar(String billeteraId, double monto,
          {String? descripcion, required String idempotencyKey}) async =>
      Transaccion.fromJson(await _api.post('/transacciones/retiros', {
        'billeteraId': billeteraId,
        'monto': _redondear(monto),
        'descripcion': descripcion,
      }, cabeceras: _idempotencia(idempotencyKey)) as Map<String, dynamic>);

  Future<ResultadoTransferencia> transferir({
    required String billeteraOrigenId,
    String? numeroCuentaDestino,
    String? telefonoDestino,          // uno de los dos: cuenta o celular
    required double monto,
    String? descripcion,
    required String idempotencyKey,
  }) async =>
      ResultadoTransferencia.fromJson(
          await _api.post('/transacciones/transferencias', {
        'billeteraOrigenId': billeteraOrigenId,
        'numeroCuentaDestino': numeroCuentaDestino,
        'telefonoDestino': telefonoDestino,
        'monto': _redondear(monto),
        'descripcion': descripcion,
      }, cabeceras: _idempotencia(idempotencyKey)) as Map<String, dynamic>);

  Future<PaginaTransacciones> historial(
    String billeteraId, {
    int pagina = 1,
    int tamano = 10,
    TipoTransaccion? tipo,
  }) async {
    final query = {
      'pagina': '$pagina',
      'tamano': '$tamano',
      if (tipo != null) 'tipo': tipo.valorApi,
    };
    final json =
        await _api.get('/billeteras/$billeteraId/transacciones', query: query);
    return PaginaTransacciones.fromJson(json as Map<String, dynamic>);
  }

  static Map<String, String> _idempotencia(String clave) => {'Idempotency-Key': clave};

  // Evita enviar 10.000000001 por errores de coma flotante
  static double _redondear(double v) => double.parse(v.toStringAsFixed(2));
}
