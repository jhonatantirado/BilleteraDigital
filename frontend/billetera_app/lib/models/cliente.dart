import 'billetera.dart';

class Cliente {
  final String id;
  final String nombres;
  final String apellidos;
  final String dni;
  final String email;
  final String telefono;
  final Billetera billetera;

  /// Solo con el celular verificado otros pueden transferirte POR CELULAR.
  final bool telefonoVerificado;

  const Cliente({
    required this.id,
    required this.nombres,
    required this.apellidos,
    required this.dni,
    required this.email,
    required this.telefono,
    required this.billetera,
    this.telefonoVerificado = false,
  });

  String get nombreCompleto => '$nombres $apellidos';

  factory Cliente.fromJson(Map<String, dynamic> json) => Cliente(
        id: json['id'] as String,
        nombres: json['nombres'] as String,
        apellidos: json['apellidos'] as String,
        dni: json['dni'] as String,
        email: json['email'] as String,
        telefono: json['telefono'] as String? ?? '',
        billetera:
            Billetera.fromJson(json['billetera'] as Map<String, dynamic>),
        telefonoVerificado: json['telefonoVerificado'] as bool? ?? false,
      );

  Cliente conBilletera(Billetera b) => Cliente(
      id: id,
      nombres: nombres,
      apellidos: apellidos,
      dni: dni,
      email: email,
      telefono: telefono,
      billetera: b,
      telefonoVerificado: telefonoVerificado);
}
