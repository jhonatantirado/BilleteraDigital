import 'package:flutter/material.dart';

class MensajeError extends StatelessWidget {
  const MensajeError(this.mensaje, {super.key});
  final String? mensaje;

  @override
  Widget build(BuildContext context) {
    if (mensaje == null) return const SizedBox.shrink();
    final c = Theme.of(context).colorScheme;
    return Container(
      width: double.infinity,
      margin: const EdgeInsets.symmetric(vertical: 8),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: c.errorContainer,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(children: [
        Icon(Icons.error_outline, color: c.onErrorContainer),
        const SizedBox(width: 8),
        Expanded(
            child: Text(mensaje!, style: TextStyle(color: c.onErrorContainer))),
      ]),
    );
  }
}
