import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../models/cliente.dart';
import '../../utils/formato.dart';

class TarjetaSaldo extends StatelessWidget {
  const TarjetaSaldo({super.key, required this.cliente});
  final Cliente cliente;

  @override
  Widget build(BuildContext context) {
    final b = cliente.billetera;
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(20),
        gradient: const LinearGradient(
          colors: [Color(0xFF0F766E), Color(0xFF134E4A)],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Hola, ${cliente.nombres}',
              style: const TextStyle(color: Colors.white70, fontSize: 15)),
          const SizedBox(height: 12),
          const Text('Saldo disponible',
              style: TextStyle(color: Colors.white70, fontSize: 13)),
          const SizedBox(height: 4),
          Text(Formato.soles(b.saldo),
              style: const TextStyle(
                  color: Colors.white,
                  fontSize: 34,
                  fontWeight: FontWeight.bold)),
          const SizedBox(height: 16),
          InkWell(
            onTap: () {
              Clipboard.setData(ClipboardData(text: b.numeroCuenta));
              ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(content: Text('Número de cuenta copiado')));
            },
            child: Row(mainAxisSize: MainAxisSize.min, children: [
              Text('Cuenta ${Formato.cuenta(b.numeroCuenta)}',
                  style: const TextStyle(
                      color: Colors.white, letterSpacing: 1.2)),
              const SizedBox(width: 6),
              const Icon(Icons.copy, color: Colors.white70, size: 16),
            ]),
          ),
        ],
      ),
    );
  }
}
