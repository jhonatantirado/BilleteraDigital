import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../controllers/historial_controller.dart';
import '../controllers/sesion_controller.dart';
import 'historial_view.dart';
import 'recarga_view.dart';
import 'retiro_view.dart';
import 'transferencia_view.dart';
import 'widgets/tarjeta_saldo.dart';
import 'widgets/transaccion_tile.dart';

class InicioView extends StatelessWidget {
  const InicioView({super.key});

  @override
  Widget build(BuildContext context) {
    final billeteraId = context.read<SesionController>().cliente!.billetera.id;
    // Controller local para los últimos 5 movimientos
    return ChangeNotifierProvider(
      create: (_) =>
          HistorialController(billeteraId, tamanoPagina: 5)..cargar(),
      child: const _InicioContenido(),
    );
  }
}

class _InicioContenido extends StatelessWidget {
  const _InicioContenido();

  Future<void> _refrescar(BuildContext context) async {
    await Future.wait([
      context.read<SesionController>().refrescarBilletera(),
      context.read<HistorialController>().cargar(),
    ]);
  }

  Future<void> _abrir(BuildContext context, Widget vista) async {
    await Navigator.of(context).push(MaterialPageRoute(builder: (_) => vista));
    if (context.mounted) await _refrescar(context);
  }

  @override
  Widget build(BuildContext context) {
    final sesion = context.watch<SesionController>();
    final historial = context.watch<HistorialController>();
    final cliente = sesion.cliente;
    if (cliente == null) return const SizedBox.shrink();

    return Scaffold(
      appBar: AppBar(
        title: const Text('Mi billetera'),
        actions: [
          IconButton(
            tooltip: 'Cerrar sesión',
            icon: const Icon(Icons.logout),
            onPressed: sesion.cerrarSesion,
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () => _refrescar(context),
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            TarjetaSaldo(cliente: cliente),
            const SizedBox(height: 12),
            _TarjetaPuntos(total: sesion.puntos?.total),
            const SizedBox(height: 20),
            Row(children: [
              _Accion(
                  icono: Icons.add_card,
                  texto: 'Recargar',
                  onTap: () => _abrir(context, const RecargaView())),
              _Accion(
                  icono: Icons.account_balance,
                  texto: 'Retirar',
                  onTap: () => _abrir(context, const RetiroView())),
              _Accion(
                  icono: Icons.send_rounded,
                  texto: 'Transferir',
                  onTap: () => _abrir(context, const TransferenciaView())),
              _Accion(
                  icono: Icons.receipt_long,
                  texto: 'Historial',
                  onTap: () => _abrir(context, const HistorialView())),
            ]),
            const SizedBox(height: 24),
            Text('Últimos movimientos',
                style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            if (historial.cargando)
              const Padding(
                padding: EdgeInsets.all(24),
                child: Center(child: CircularProgressIndicator()),
              )
            else if (historial.error != null)
              Text(historial.error!)
            else if (historial.items.isEmpty)
              const Padding(
                padding: EdgeInsets.all(24),
                child: Center(
                    child: Text('Aún no tienes movimientos.\n¡Haz tu primera recarga!',
                        textAlign: TextAlign.center)),
              )
            else
              Card(
                child: Column(
                  children: historial.items.map(TransaccionTile.new).toList(),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

/// Muestra los puntos de fidelidad (los otorga el PuntosFidelidadObserver del backend).
class _TarjetaPuntos extends StatelessWidget {
  const _TarjetaPuntos({required this.total});
  final int? total;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        leading: const Icon(Icons.stars_rounded, color: Colors.amber),
        title: const Text('Puntos de fidelidad'),
        subtitle: const Text('1 punto por cada S/ 10 en recargas y transferencias'),
        trailing: Text(
          total?.toString() ?? '—',
          style: Theme.of(context).textTheme.titleLarge,
        ),
      ),
    );
  }
}

class _Accion extends StatelessWidget {
  const _Accion({required this.icono, required this.texto, required this.onTap});
  final IconData icono;
  final String texto;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final c = Theme.of(context).colorScheme;
    return Expanded(
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 8),
          child: Column(children: [
            CircleAvatar(
              radius: 28,
              backgroundColor: c.primaryContainer,
              child: Icon(icono, color: c.onPrimaryContainer),
            ),
            const SizedBox(height: 6),
            Text(texto),
          ]),
        ),
      ),
    );
  }
}
