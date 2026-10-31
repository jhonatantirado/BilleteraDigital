import 'package:flutter/material.dart';

import '../../models/transaccion.dart';
import '../../utils/formato.dart';

class TransaccionTile extends StatelessWidget {
  const TransaccionTile(this.t, {super.key});
  final Transaccion t;

  @override
  Widget build(BuildContext context) {
    final ingreso = t.tipo.esIngreso;
    final color = ingreso ? Colors.green.shade700 : Colors.red.shade700;
    final icono = switch (t.tipo) {
      TipoTransaccion.recarga => Icons.add_card,
      TipoTransaccion.transferenciaEnviada => Icons.call_made,
      TipoTransaccion.transferenciaRecibida => Icons.call_received,
    };
    final subtitulo = [
      if (t.nombreContraparte != null) t.nombreContraparte!,
      Formato.fechaHora(t.fecha),
      if (t.comision > 0) 'Comisión ${Formato.soles(t.comision)}',
    ].join(' · ');

    return ListTile(
      leading: CircleAvatar(
        backgroundColor: color.withAlpha(30),
        child: Icon(icono, color: color),
      ),
      title: Text(t.descripcion, maxLines: 1, overflow: TextOverflow.ellipsis),
      subtitle: Text(subtitulo, style: const TextStyle(fontSize: 12)),
      trailing: Text(
        '${ingreso ? '+' : '-'} ${Formato.soles(t.monto)}',
        style: TextStyle(color: color, fontWeight: FontWeight.bold),
      ),
    );
  }
}
