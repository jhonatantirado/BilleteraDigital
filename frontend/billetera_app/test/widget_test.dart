import 'package:billetera_app/main.dart';
import 'package:billetera_app/views/inicio_view.dart';
import 'package:billetera_app/views/login_view.dart';
import 'package:flutter_test/flutter_test.dart';

/// Prueba de humo: la app arranca y, sin sesión iniciada, muestra el login.
/// (Reemplaza la prueba del contador que genera `flutter create`.)
void main() {
  testWidgets('Sin sesión iniciada, la app muestra la pantalla de login',
      (WidgetTester tester) async {
    await tester.pumpWidget(const BilleteraApp());

    expect(find.byType(LoginView), findsOneWidget);
    expect(find.byType(InicioView), findsNothing);
    expect(find.text('Billetera Digital'), findsOneWidget);
    expect(find.text('Ingresar'), findsOneWidget);
    expect(find.text('¿No tienes cuenta? Regístrate'), findsOneWidget);
  });
}
