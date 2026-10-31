import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../controllers/sesion_controller.dart';
import '../utils/validadores.dart';
import 'registro_view.dart';
import 'widgets/boton_cargando.dart';
import 'widgets/mensaje_error.dart';

/// "V" de MVC: solo dibuja y captura eventos. La lógica está en SesionController.
class LoginView extends StatefulWidget {
  const LoginView({super.key});

  @override
  State<LoginView> createState() => _LoginViewState();
}

class _LoginViewState extends State<LoginView> {
  final _form = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _password = TextEditingController();
  bool _oculto = true;

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _ingresar() async {
    if (!_form.currentState!.validate()) return;
    await context
        .read<SesionController>()
        .login(_email.text.trim(), _password.text);
    // Si el login es exitoso, main.dart cambia a InicioView automáticamente.
  }

  @override
  Widget build(BuildContext context) {
    final sesion = context.watch<SesionController>();
    final tema = Theme.of(context);

    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Form(
                key: _form,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Icon(Icons.account_balance_wallet_rounded,
                        size: 72, color: tema.colorScheme.primary),
                    const SizedBox(height: 12),
                    Text('Billetera Digital',
                        textAlign: TextAlign.center,
                        style: tema.textTheme.headlineMedium
                            ?.copyWith(fontWeight: FontWeight.bold)),
                    const SizedBox(height: 4),
                    Text('Envía y recibe dinero al instante',
                        textAlign: TextAlign.center,
                        style: tema.textTheme.bodyMedium),
                    const SizedBox(height: 32),
                    TextFormField(
                      controller: _email,
                      keyboardType: TextInputType.emailAddress,
                      decoration: const InputDecoration(
                          labelText: 'Email', prefixIcon: Icon(Icons.email)),
                      validator: Validadores.email,
                    ),
                    const SizedBox(height: 16),
                    TextFormField(
                      controller: _password,
                      obscureText: _oculto,
                      decoration: InputDecoration(
                        labelText: 'Contraseña',
                        prefixIcon: const Icon(Icons.lock),
                        suffixIcon: IconButton(
                          icon: Icon(_oculto
                              ? Icons.visibility
                              : Icons.visibility_off),
                          onPressed: () => setState(() => _oculto = !_oculto),
                        ),
                      ),
                      validator: (v) => Validadores.requerido(v, 'La contraseña'),
                      onFieldSubmitted: (_) => _ingresar(),
                    ),
                    MensajeError(sesion.error),
                    const SizedBox(height: 16),
                    BotonCargando(
                      texto: 'Ingresar',
                      icono: Icons.login,
                      cargando: sesion.cargando,
                      onPressed: _ingresar,
                    ),
                    const SizedBox(height: 12),
                    TextButton(
                      onPressed: () {
                        sesion.limpiarError();
                        Navigator.of(context).push(MaterialPageRoute(
                            builder: (_) => const RegistroView()));
                      },
                      child: const Text('¿No tienes cuenta? Regístrate'),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
