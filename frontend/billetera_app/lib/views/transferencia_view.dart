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
  final _cuenta = TextEditingController();
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

  Future<void> _verificarCuenta() async {
    if (Validadores.cuenta(_cuenta.text) != null) {
      _form.currentState!.validate();
      return;
    }
    FocusScope.of(context).unfocus();
    await context.read<OperacionesController>().buscarDestinatario(_cuenta.text);
  }

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
            Text('Cuenta: ${Formato.cuenta(d.numeroCuenta)}'),
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

            // ---------- Paso 1: cuenta destino
            TextFormField(
              controller: _cuenta,
              keyboardType: TextInputType.number,
              maxLength: 10,
              decoration: InputDecoration(
                labelText: 'Número de cuenta destino',
                counterText: '',
                suffixIcon: op.buscandoDestinatario
                    ? const Padding(
                        padding: EdgeInsets.all(12),
                        child: CircularProgressIndicator(strokeWidth: 2))
                    : IconButton(
                        icon: const Icon(Icons.search),
                        onPressed: _verificarCuenta),
              ),
              validator: Validadores.cuenta,
              onChanged: (v) {
                if (op.destinatario != null) op.reiniciar();
                if (v.length == 10) _verificarCuenta();
              },
            ),
            if (destino != null)
              Card(
                margin: const EdgeInsets.only(top: 8),
                child: ListTile(
                  leading: const Icon(Icons.verified_user, color: Colors.green),
                  title: Text(destino.nombreTitular),
                  subtitle: Text('Cuenta ${Formato.cuenta(destino.numeroCuenta)}'),
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
