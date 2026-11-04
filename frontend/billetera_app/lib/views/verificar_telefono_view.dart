import 'dart:async';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../controllers/sesion_controller.dart';
import '../utils/formato.dart';
import '../utils/validadores.dart';
import 'widgets/boton_cargando.dart';
import 'widgets/mensaje_error.dart';

/// Verificación del celular con el código de 6 dígitos enviado por SMS.
/// Hasta verificarlo, nadie puede encontrarte ni transferirte POR CELULAR
/// (por número de cuenta, sí).
class VerificarTelefonoView extends StatefulWidget {
  const VerificarTelefonoView({super.key});

  /// Debe coincidir con VerificacionTelefono.EsperaReenvio en el backend
  static const esperaReenvio = Duration(seconds: 60);

  @override
  State<VerificarTelefonoView> createState() => _VerificarTelefonoViewState();
}

class _VerificarTelefonoViewState extends State<VerificarTelefonoView> {
  final _form = GlobalKey<FormState>();
  final _codigo = TextEditingController();
  Timer? _temporizador;
  int _segundos = 0;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance
        .addPostFrameCallback((_) => context.read<SesionController>().limpiarError());
  }

  @override
  void dispose() {
    _temporizador?.cancel();
    _codigo.dispose();
    super.dispose();
  }

  Future<void> _verificar() async {
    if (!_form.currentState!.validate()) return;
    final ok = await context.read<SesionController>().verificarTelefono(_codigo.text);
    if (ok && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(
          content: Text('¡Celular verificado! Ya pueden transferirte a tu celular.')));
      Navigator.of(context).pop();
    }
  }

  Future<void> _reenviar() async {
    final ok = await context.read<SesionController>().reenviarCodigoTelefono();
    if (!mounted) return;
    if (ok) {
      ScaffoldMessenger.of(context)
          .showSnackBar(const SnackBar(content: Text('Te enviamos un código nuevo.')));
    }
    _iniciarEspera();
  }

  void _iniciarEspera() {
    _temporizador?.cancel();
    setState(() => _segundos = VerificarTelefonoView.esperaReenvio.inSeconds);
    _temporizador = Timer.periodic(const Duration(seconds: 1), (t) {
      if (!mounted) {
        t.cancel();
        return;
      }
      setState(() => _segundos--);
      if (_segundos <= 0) t.cancel();
    });
  }

  @override
  Widget build(BuildContext context) {
    final sesion = context.watch<SesionController>();
    final telefono = sesion.cliente?.telefono ?? '';

    return Scaffold(
      appBar: AppBar(title: const Text('Verificar celular')),
      body: SafeArea(
        child: Form(
          key: _form,
          child: ListView(padding: const EdgeInsets.all(24), children: [
            Text('Enviamos un código de 6 dígitos al ${Formato.celular(telefono)}.',
                style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            const Text(
              'En este proyecto el SMS es simulado: el código aparece en la consola '
              'de la API y en logs/sms_simulados.log.',
              style: TextStyle(fontSize: 12),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _codigo,
              autofocus: true,
              keyboardType: TextInputType.number,
              maxLength: 6,
              textAlign: TextAlign.center,
              style: const TextStyle(fontSize: 24, letterSpacing: 8),
              decoration: const InputDecoration(labelText: 'Código', counterText: ''),
              validator: Validadores.codigoVerificacion,
              onChanged: (v) {
                if (v.length == 6 && !sesion.cargando) _verificar();
              },
            ),
            MensajeError(sesion.error),
            const SizedBox(height: 16),
            BotonCargando(
              texto: 'Verificar',
              icono: Icons.verified,
              cargando: sesion.cargando,
              onPressed: _verificar,
            ),
            const SizedBox(height: 8),
            TextButton(
              onPressed: _segundos > 0 || sesion.cargando ? null : _reenviar,
              child: Text(_segundos > 0
                  ? 'Reenviar código en $_segundos s'
                  : 'Reenviar código'),
            ),
          ]),
        ),
      ),
    );
  }
}
