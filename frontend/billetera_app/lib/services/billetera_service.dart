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

  Future<Comision> calcularComision(double monto) async =>
      Comision.fromJson(await _api.get('/transacciones/comision',
          query: {'monto': monto.toStringAsFixed(2)}) as Map<String, dynamic>);

  Future<Transaccion> recargar(String billeteraId, double monto) async =>
      Transaccion.fromJson(await _api.post('/transacciones/recargas', {
        'billeteraId': billeteraId,
        'monto': _redondear(monto),
      }) as Map<String, dynamic>);

  Future<Transaccion> retirar(String billeteraId, double monto,
          {String? descripcion}) async =>
      Transaccion.fromJson(await _api.post('/transacciones/retiros', {
        'billeteraId': billeteraId,
        'monto': _redondear(monto),
        'descripcion': descripcion,
      }) as Map<String, dynamic>);

  Future<ResultadoTransferencia> transferir({
    required String billeteraOrigenId,
    required String numeroCuentaDestino,
    required double monto,
    String? descripcion,
  }) async =>
      ResultadoTransferencia.fromJson(
          await _api.post('/transacciones/transferencias', {
        'billeteraOrigenId': billeteraOrigenId,
        'numeroCuentaDestino': numeroCuentaDestino,
        'monto': _redondear(monto),
        'descripcion': descripcion,
      }) as Map<String, dynamic>);

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

  // Evita enviar 10.000000001 por errores de coma flotante
  static double _redondear(double v) => double.parse(v.toStringAsFixed(2));
}
