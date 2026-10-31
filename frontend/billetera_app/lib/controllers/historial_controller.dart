import 'package:flutter/foundation.dart';

import '../core/api_exception.dart';
import '../models/transaccion.dart';
import '../services/billetera_service.dart';

/// Controlador del historial: paginación ("scroll infinito") y filtro por tipo.
class HistorialController extends ChangeNotifier {
  HistorialController(this.billeteraId,
      {this.tamanoPagina = 15, BilleteraService? servicio})
      : _servicio = servicio ?? BilleteraService();

  final String billeteraId;
  final int tamanoPagina;
  final BilleteraService _servicio;

  final List<Transaccion> _items = [];
  List<Transaccion> get items => List.unmodifiable(_items);

  TipoTransaccion? filtro;
  int _pagina = 0;
  int _totalPaginas = 1;
  bool cargando = false;
  bool cargandoMas = false;
  String? error;

  bool get hayMas => _pagina < _totalPaginas;

  Future<void> cargar() async {
    cargando = true;
    error = null;
    _items.clear();
    _pagina = 0;
    _totalPaginas = 1;
    notifyListeners();
    await _siguientePagina();
    cargando = false;
    notifyListeners();
  }

  Future<void> cargarMas() async {
    if (cargando || cargandoMas || !hayMas) return;
    cargandoMas = true;
    notifyListeners();
    await _siguientePagina();
    cargandoMas = false;
    notifyListeners();
  }

  Future<void> cambiarFiltro(TipoTransaccion? nuevo) async {
    if (nuevo == filtro) return;
    filtro = nuevo;
    await cargar();
  }

  Future<void> _siguientePagina() async {
    try {
      final pagina = await _servicio.historial(billeteraId,
          pagina: _pagina + 1, tamano: tamanoPagina, tipo: filtro);
      _items.addAll(pagina.items);
      _pagina = pagina.pagina;
      _totalPaginas = pagina.totalPaginas;
    } on ApiException catch (e) {
      error = e.mensaje;
    }
  }
}
