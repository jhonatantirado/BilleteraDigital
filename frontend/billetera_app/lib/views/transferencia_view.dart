import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../controllers/operaciones_controller.dart';
import '../controllers/sesion_controller.dart';
import '../models/operaciones.dart';
import '../utils/formato.dart';
import '../utils/validadores.dart';
import 'widgets/boton_cargando.dart';
import 'widgets/mensaje_error.dart';

class TransferenciaView extends StatefulWidget {
  const TransferenciaView({super.key});

  @override
  State<TransferenciaView> createState() => _TransferenciaViewState();
}

class _TransferenciaViewState extends State<TransferenciaView> {
  final _form = GlobalKey<FormState>();
  final _cuenta = TextEditingController();   // número de cuenta o celular, según el modo
  ModoBusqueda _modo = ModoBusqueda.celular;  // como Yape/Plin: por celular por defecto
  final _monto = TextEditingController();
  final _descripcion = TextEditingController();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback(
        (_) => context.read<OperacionesController>().reiniciar());
  }

  @override
  void dispose() {
    _cuenta.dispose();
    _monto.dispose();
    _descripcion.dispose();
    super.dispose();
  }

  bool get _porCelular => _modo == ModoBusqueda.celular;

  String? _validarDestino(String? v) =>
      _porCelular ? Validadores.celular(v) : Validadores.cuenta(v);

  Future<void> _verificarCuenta() async {
    if (_validarDestino(_cuenta.text) != null) {
      _form.currentState!.validate();
      return;
    }
    FocusScope.of(context).unfocus();
    await context
        .read<OperacionesController>()
        .buscarDestinatario(_cuenta.text, modo: _modo);
  }

  void _cambiarModo(ModoBusqueda modo) {
    setState(() => _modo = modo);
    _cuenta.clear();
    context.read<OperacionesController>().reiniciar();
  }

  /// "Cuenta 1900 0000 02" o "Celular *** *** 678"
  static String _detalleDestino(Destinatario d) => d.porCelular
      ? 'Celular ${d.telefono ?? ''}'
      : 'Cuenta ${Formato.cuenta(d.numeroCuenta!)}';

  Future<void> _transferir() async {
    final op = context.read<OperacionesController>();
    if (!_form.currentState!.validate() || op.destinatario == null) return;

    final monto = Validadores.aMonto(_monto.text);
    final confirmado = await _confirmar(op.destinatario!, monto, op.comision);
    if (confirmado != true || !mounted) return;

    final resultado = await op.transferir(monto, _descripcion.text);
    if (resultado != null && mounted) {
      await _mostrarComprobante(resultado);
      if (mounted) Navigator.of(context).pop();
    }
  }

  Future<bool?> _confirmar(Destinatario d, double monto, Comision? c) {
    return showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Confirmar transferencia'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Para: ${d.nombreTitular}'),
            Text(_detalleDestino(d)),
            const Divider(),
            Text('Monto: ${Formato.soles(monto)}'),
            if (c != null) Text('Comisión: ${Formato.soles(c.comision)}'),
            if (c != null)
              Text('Total a debitar: ${Formato.soles(c.total)}',
                  style: const TextStyle(fontWeight: FontWeight.bold)),
          ],
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: const Text('Cancelar')),
          FilledButton(
              onPressed: () => Navigator.pop(ctx, true),
              child: const Text('Transferir')),
        ],
      ),
    );
  }

  Future<void> _mostrarComprobante(ResultadoTransferencia r) {
    return showDialog<void>(
      context: context,
      builder: (ctx) => AlertDialog(
        icon: const Icon(Icons.check_circle, color: Colors.green, size: 56),
        title: const Text('¡Transferencia exitosa!'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('${Formato.soles(r.monto)} a ${r.nombreDestinatario}'),
            if (r.telefonoDestino != null) Text('Celular ${r.telefonoDestino}'),
            Text('Comisión: ${Formato.soles(r.comision)} (${r.estrategiaComision})'),
            Text('Saldo actual: ${Formato.soles(r.saldoActual)}'),
            Text(Formato.fechaHora(r.fecha)),
            const SizedBox(height: 8),
            SelectableText('Ref: ${r.referencia}',
                style: const TextStyle(fontSize: 11)),
          ],
        ),
        actions: [
          FilledButton(
              onPressed: () => Navigator.pop(ctx), child: const Text('Listo')),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final op = context.watch<OperacionesController>();
    final saldo = context.watch<SesionController>().cliente?.billetera.saldo ?? 0;
    final destino = op.destinatario;

    return Scaffold(
      appBar: AppBar(title: const Text('Transferir')),
      body: SafeArea(
        child: Form(
          key: _form,
          child: ListView(padding: const EdgeInsets.all(24), children: [
            Text('Saldo disponible: ${Formato.soles(saldo)}'),
            const SizedBox(height: 16),

            // ---------- Paso 1: destinatario (por celular o por número de cuenta)
            SegmentedButton<ModoBusqueda>(
              segments: const [
                ButtonSegment(
                    value: ModoBusqueda.celular,
                    icon: Icon(Icons.phone_android),
                    label: Text('Celular')),
                ButtonSegment(
                    value: ModoBusqueda.cuenta,
                    icon: Icon(Icons.account_balance_wallet),
                    label: Text('N.º de cuenta')),
              ],
              selected: {_modo},
              onSelectionChanged: (s) => _cambiarModo(s.first),
            ),
            const SizedBox(height: 12),
            TextFormField(
              key: ValueKey(_modo), // reinicia el campo al cambiar de modo
              controller: _cuenta,
              keyboardType: _porCelular ? TextInputType.phone : TextInputType.number,
              maxLength: _porCelular ? 15 : 10,
              decoration: InputDecoration(
                labelText: _porCelular ? 'Celular del destinatario' : 'Número de cuenta destino',
                hintText: _porCelular ? '987 654 321' : null,
                counterText: '',
                suffixIcon: op.buscandoDestinatario
                    ? const Padding(
                        padding: EdgeInsets.all(12),
                        child: CircularProgressIndicator(strokeWidth: 2))
                    : IconButton(
                        icon: const Icon(Icons.search),
                        onPressed: _verificarCuenta),
              ),
              validator: _validarDestino,
              onChanged: (v) {
                if (op.destinatario != null) op.reiniciar();
                // Busca solo cuando el dato ya es válido (10 dígitos de cuenta / celular completo)
                if (_validarDestino(v) == null) _verificarCuenta();
              },
            ),
            if (destino != null)
              Card(
                margin: const EdgeInsets.only(top: 8),
                child: ListTile(
                  leading: const Icon(Icons.verified_user, color: Colors.green),
                  title: Text(destino.nombreTitular),
                  subtitle: Text(_detalleDestino(destino)),
                ),
              ),

            // ---------- Paso 2: monto y descripción
            if (destino != null) ...[
              const SizedBox(height: 16),
              TextFormField(
                controller: _monto,
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
                decoration: const InputDecoration(
                    labelText: 'Monto', prefixText: 'S/ '),
                validator: (v) => Validadores.monto(v),
                onChanged: (v) => op.previsualizarComision(
                    double.tryParse(v.replaceAll(',', '.'))),
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _descripcion,
                maxLength: 140,
                decoration: const InputDecoration(
                    labelText: 'Descripción (opcional)'),
              ),
              _PanelComision(op: op),
            ],

            MensajeError(op.error),
            const SizedBox(height: 16),
            BotonCargando(
              texto: 'Continuar',
              icono: Icons.send_rounded,
              cargando: op.procesando,
              onPressed: destino == null ? null : _transferir,
            ),
          ]),
        ),
      ),
    );
  }
}

/// Muestra la comisión calculada por el backend (patrón Strategy).
class _PanelComision extends StatelessWidget {
  const _PanelComision({required this.op});
  final OperacionesController op;

  @override
  Widget build(BuildContext context) {
    if (op.calculandoComision) {
      return const Padding(
        padding: EdgeInsets.all(8),
        child: LinearProgressIndicator(),
      );
    }
    final c = op.comision;
    if (c == null) return const SizedBox.shrink();
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.secondaryContainer,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text('Política aplicada: ${c.estrategia}',
            style: const TextStyle(fontWeight: FontWeight.w600)),
        Text('Comisión: ${Formato.soles(c.comision)}'),
        Text('Total a debitar: ${Formato.soles(c.total)}'),
      ]),
    );
  }
}
