import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../controllers/operaciones_controller.dart';
import '../controllers/sesion_controller.dart';
import '../utils/formato.dart';
import '../utils/validadores.dart';
import 'widgets/boton_cargando.dart';
import 'widgets/mensaje_error.dart';

/// Retiro de saldo hacia una cuenta bancaria (simulado).
class RetiroView extends StatefulWidget {
  const RetiroView({super.key});

  /// Debe coincidir con BilleteraService.MontoMaximoRetiro en el backend
  static const double montoMaximo = 2000;

  @override
  State<RetiroView> createState() => _RetiroViewState();
}

class _RetiroViewState extends State<RetiroView> {
  final _form = GlobalKey<FormState>();
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
    _monto.dispose();
    _descripcion.dispose();
    super.dispose();
  }

  Future<void> _retirar() async {
    if (!_form.currentState!.validate()) return;
    final monto = Validadores.aMonto(_monto.text);
    final ok = await context
        .read<OperacionesController>()
        .retirar(monto, _descripcion.text);
    if (ok && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Retiro de ${Formato.soles(monto)} exitoso')));
      Navigator.of(context).pop();
    }
  }

  @override
  Widget build(BuildContext context) {
    final op = context.watch<OperacionesController>();
    final saldo = context.watch<SesionController>().cliente?.billetera.saldo ?? 0;

    return Scaffold(
      appBar: AppBar(title: const Text('Retirar saldo')),
      body: SafeArea(
        child: Form(
          key: _form,
          child: ListView(padding: const EdgeInsets.all(24), children: [
            Text('Saldo disponible: ${Formato.soles(saldo)}'),
            const SizedBox(height: 16),
            TextFormField(
              controller: _monto,
              autofocus: true,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: const InputDecoration(
                  labelText: 'Monto a retirar', prefixText: 'S/ '),
              validator: (v) {
                final error = Validadores.monto(v, maximo: RetiroView.montoMaximo);
                if (error != null) return error;
                // Validación de cortesía: la regla real la aplica el Dominio en el backend
                return Validadores.aMonto(v!) > saldo ? 'Saldo insuficiente' : null;
              },
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _descripcion,
              maxLength: 140,
              decoration: const InputDecoration(
                  labelText: 'Descripción (opcional)',
                  hintText: 'Ej. A mi cuenta del banco'),
            ),
            const Text(
              'Simula un abono a tu cuenta bancaria. Máximo S/ 2,000 por operación. '
              'Los retiros no suman puntos de fidelidad.',
              style: TextStyle(fontSize: 12),
            ),
            MensajeError(op.error),
            const SizedBox(height: 16),
            BotonCargando(
              texto: 'Retirar',
              icono: Icons.account_balance,
              cargando: op.procesando,
              onPressed: _retirar,
            ),
          ]),
        ),
      ),
    );
  }
}
