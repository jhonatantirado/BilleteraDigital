import 'billetera.dart';

class Cliente {
  final String id;
  final String nombres;
  final String apellidos;
  final String dni;
  final String email;
  final String telefono;
  final Billetera billetera;

  const Cliente({
    required this.id,
    required this.nombres,
    required this.apellidos,
    required this.dni,
    required this.email,
    required this.telefono,
    required this.billetera,
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
      );

  Cliente conBilletera(Billetera b) => Cliente(
      id: id,
      nombres: nombres,
      apellidos: apellidos,
      dni: dni,
      email: email,
      telefono: telefono,
      billetera: b);
}
