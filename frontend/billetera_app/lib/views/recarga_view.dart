import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../controllers/operaciones_controller.dart';
import '../controllers/sesion_controller.dart';
import '../utils/formato.dart';
import '../utils/validadores.dart';
import 'widgets/boton_cargando.dart';
import 'widgets/mensaje_error.dart';

class RecargaView extends StatefulWidget {
  const RecargaView({super.key});

  @override
  State<RecargaView> createState() => _RecargaViewState();
}

class _RecargaViewState extends State<RecargaView> {
  final _form = GlobalKey<FormState>();
  final _monto = TextEditingController();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback(
        (_) => context.read<OperacionesController>().reiniciar());
  }

  @override
  void dispose() {
    _monto.dispose();
    super.dispose();
  }

  Future<void> _recargar() async {
    if (!_form.currentState!.validate()) return;
    final monto = Validadores.aMonto(_monto.text);
    final ok = await context.read<OperacionesController>().recargar(monto);
    if (ok && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Recarga de ${Formato.soles(monto)} exitosa')));
      Navigator.of(context).pop();
    }
  }

  @override
  Widget build(BuildContext context) {
    final op = context.watch<OperacionesController>();
    final saldo = context.watch<SesionController>().cliente?.billetera.saldo ?? 0;

    return Scaffold(
      appBar: AppBar(title: const Text('Recargar saldo')),
      body: SafeArea(
        child: Form(
          key: _form,
          child: ListView(padding: const EdgeInsets.all(24), children: [
            Text('Saldo actual: ${Formato.soles(saldo)}'),
            const SizedBox(height: 16),
            TextFormField(
              controller: _monto,
              autofocus: true,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: const InputDecoration(
                  labelText: 'Monto a recargar', prefixText: 'S/ '),
              validator: (v) => Validadores.monto(v, maximo: 5000),
            ),
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              children: [20, 50, 100, 200, 500]
                  .map((m) => ActionChip(
                        label: Text('S/ $m'),
                        onPressed: () => _monto.text = '$m',
                      ))
                  .toList(),
            ),
            const SizedBox(height: 8),
            const Text(
              'Simula un abono desde tarjeta o banco. Máximo S/ 5,000 por operación.',
              style: TextStyle(fontSize: 12),
            ),
            MensajeError(op.error),
            const SizedBox(height: 16),
            BotonCargando(
              texto: 'Recargar',
              icono: Icons.add_card,
              cargando: op.procesando,
              onPressed: _recargar,
            ),
          ]),
        ),
      ),
    );
  }
}
