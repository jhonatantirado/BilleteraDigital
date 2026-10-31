import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../controllers/historial_controller.dart';
import '../controllers/sesion_controller.dart';
import '../models/transaccion.dart';
import 'widgets/transaccion_tile.dart';

class HistorialView extends StatelessWidget {
  const HistorialView({super.key});

  @override
  Widget build(BuildContext context) {
    final id = context.read<SesionController>().cliente!.billetera.id;
    return ChangeNotifierProvider(
      create: (_) => HistorialController(id)..cargar(),
      child: const _HistorialContenido(),
    );
  }
}

class _HistorialContenido extends StatelessWidget {
  const _HistorialContenido();

  @override
  Widget build(BuildContext context) {
    final h = context.watch<HistorialController>();

    return Scaffold(
      appBar: AppBar(title: const Text('Historial de transacciones')),
      body: Column(children: [
        // Filtros por tipo
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          child: Row(children: [
            _Filtro(etiqueta: 'Todos', tipo: null, h: h),
            for (final t in TipoTransaccion.values)
              _Filtro(etiqueta: t.etiqueta, tipo: t, h: h),
          ]),
        ),
        Expanded(child: _lista(h)),
      ]),
    );
  }

  Widget _lista(HistorialController h) {
    if (h.cargando) return const Center(child: CircularProgressIndicator());
    if (h.error != null && h.items.isEmpty) {
      return Center(child: Text(h.error!));
    }
    if (h.items.isEmpty) {
      return const Center(child: Text('No hay movimientos.'));
    }
    return NotificationListener<ScrollNotification>(
      // Scroll infinito: al acercarse al final se pide la siguiente página
      onNotification: (n) {
        if (n.metrics.pixels > n.metrics.maxScrollExtent - 200) h.cargarMas();
        return false;
      },
      child: RefreshIndicator(
        onRefresh: h.cargar,
        child: ListView.separated(
          physics: const AlwaysScrollableScrollPhysics(),
          itemCount: h.items.length + (h.hayMas ? 1 : 0),
          separatorBuilder: (_, __) => const Divider(height: 1),
          itemBuilder: (_, i) {
            if (i >= h.items.length) {
              return const Padding(
                padding: EdgeInsets.all(16),
                child: Center(child: CircularProgressIndicator()),
              );
            }
            return TransaccionTile(h.items[i]);
          },
        ),
      ),
    );
  }
}

class _Filtro extends StatelessWidget {
  const _Filtro({required this.etiqueta, required this.tipo, required this.h});
  final String etiqueta;
  final TipoTransaccion? tipo;
  final HistorialController h;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(right: 8),
      child: ChoiceChip(
        label: Text(etiqueta),
        selected: h.filtro == tipo,
        onSelected: (_) => h.cambiarFiltro(tipo),
      ),
    );
  }
}
