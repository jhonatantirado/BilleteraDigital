import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../controllers/sesion_controller.dart';
import '../utils/validadores.dart';
import 'widgets/boton_cargando.dart';
import 'widgets/mensaje_error.dart';

class RegistroView extends StatefulWidget {
  const RegistroView({super.key});

  @override
  State<RegistroView> createState() => _RegistroViewState();
}

class _RegistroViewState extends State<RegistroView> {
  final _form = GlobalKey<FormState>();
  final _nombres = TextEditingController();
  final _apellidos = TextEditingController();
  final _dni = TextEditingController();
  final _email = TextEditingController();
  final _telefono = TextEditingController();
  final _password = TextEditingController();
  final _confirmar = TextEditingController();

  @override
  void dispose() {
    for (final c in [
      _nombres, _apellidos, _dni, _email, _telefono, _password, _confirmar
    ]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _registrar() async {
    if (!_form.currentState!.validate()) return;
    final sesion = context.read<SesionController>();
    final ok = await sesion.registrar(
      nombres: _nombres.text.trim(),
      apellidos: _apellidos.text.trim(),
      dni: _dni.text.trim(),
      email: _email.text.trim(),
      telefono: _telefono.text.trim(),
      password: _password.text,
    );
    if (ok && mounted) {
      // La sesión ya está iniciada: volvemos a la raíz, que mostrará InicioView
      Navigator.of(context).popUntil((r) => r.isFirst);
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(
          content: Text('¡Cuenta creada! Te enviamos un código por SMS '
              'para verificar tu celular.')));
    }
  }

  Widget _campo(TextEditingController c, String etiqueta, IconData icono,
      String? Function(String?) validador,
      {TextInputType? teclado, bool oculto = false, int? max}) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 14),
      child: TextFormField(
        controller: c,
        keyboardType: teclado,
        obscureText: oculto,
        maxLength: max,
        decoration: InputDecoration(
            labelText: etiqueta, prefixIcon: Icon(icono), counterText: ''),
        validator: validador,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final sesion = context.watch<SesionController>();
    return Scaffold(
      appBar: AppBar(title: const Text('Crear cuenta')),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 480),
              child: Form(
                key: _form,
                child: Column(children: [
                  _campo(_nombres, 'Nombres', Icons.person,
                      (v) => Validadores.requerido(v, 'El nombre')),
                  _campo(_apellidos, 'Apellidos', Icons.person_outline,
                      (v) => Validadores.requerido(v, 'El apellido')),
                  _campo(_dni, 'DNI', Icons.badge, Validadores.dni,
                      teclado: TextInputType.number, max: 8),
                  _campo(_email, 'Email', Icons.email, Validadores.email,
                      teclado: TextInputType.emailAddress),
                  _campo(_telefono, 'Celular', Icons.phone_android,
                      Validadores.celular,
                      teclado: TextInputType.phone, max: 15),
                  _campo(_password, 'Contraseña', Icons.lock,
                      Validadores.password,
                      oculto: true),
                  _campo(_confirmar, 'Confirmar contraseña', Icons.lock_outline,
                      (v) => v == _password.text
                          ? null
                          : 'Las contraseñas no coinciden',
                      oculto: true),
                  MensajeError(sesion.error),
                  const SizedBox(height: 8),
                  BotonCargando(
                    texto: 'Registrarme',
                    icono: Icons.person_add,
                    cargando: sesion.cargando,
                    onPressed: _registrar,
                  ),
                ]),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
