import 'package:flutter/material.dart';

/// Botón principal que muestra un indicador mientras se procesa la petición.
class BotonCargando extends StatelessWidget {
  const BotonCargando({
    super.key,
    required this.texto,
    required this.cargando,
    required this.onPressed,
    this.icono,
  });

  final String texto;
  final bool cargando;
  final VoidCallback? onPressed;
  final IconData? icono;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      height: 52,
      child: FilledButton.icon(
        onPressed: cargando ? null : onPressed,
        icon: cargando
            ? const SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(strokeWidth: 2))
            : Icon(icono ?? Icons.check),
        label: Text(texto),
      ),
    );
  }
}
