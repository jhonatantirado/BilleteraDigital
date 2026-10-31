import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'controllers/operaciones_controller.dart';
import 'controllers/sesion_controller.dart';
import 'views/inicio_view.dart';
import 'views/login_view.dart';

void main() {
  runApp(const BilleteraApp());
}

class BilleteraApp extends StatelessWidget {
  const BilleteraApp({super.key});

  @override
  Widget build(BuildContext context) {
    // Los Controllers se "inyectan" en el árbol de widgets con Provider;
    // las Views los obtienen con context.watch / context.read.
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => SesionController()),
        ChangeNotifierProvider(
          create: (ctx) => OperacionesController(ctx.read<SesionController>()),
        ),
      ],
      child: MaterialApp(
        title: 'Billetera Digital',
        debugShowCheckedModeBanner: false,
        theme: ThemeData(
          useMaterial3: true,
          colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF0F766E)),
          inputDecorationTheme: const InputDecorationTheme(
            border: OutlineInputBorder(),
          ),
        ),
        // Navegación declarativa según el estado de la sesión
        home: Consumer<SesionController>(
          builder: (_, sesion, __) =>
              sesion.autenticado ? const InicioView() : const LoginView(),
        ),
      ),
    );
  }
}
