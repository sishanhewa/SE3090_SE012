import 'package:flutter_test/flutter_test.dart';
import 'package:talent_flow/main.dart';
import 'package:talent_flow/features/auth/screens/login_screen.dart';

void main() {
  testWidgets('Unauthenticated users see the login screen', (tester) async {
    await tester.pumpWidget(const TalentFlowApp());
    await tester.pumpAndSettle();
    expect(find.byType(LoginScreen), findsOneWidget);
  });
}
