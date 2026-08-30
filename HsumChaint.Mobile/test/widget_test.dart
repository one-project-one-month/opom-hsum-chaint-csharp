import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:hsum_chaint_mobile/src/app.dart';
import 'package:hsum_chaint_mobile/src/app_state.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  testWidgets('shows Burmese login screen first', (tester) async {
    SharedPreferences.setMockInitialValues({});
    final preferences = await SharedPreferences.getInstance();

    await tester.pumpWidget(
      ProviderScope(
        overrides: [sharedPreferencesProvider.overrideWithValue(preferences)],
        child: const HsumChaintApp(),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('ဆွမ်းချိုင့်'), findsWidgets);
    expect(find.text('ဝင်ရန်'), findsWidgets);
    expect(find.text('စာရင်းသွင်းရန်'), findsOneWidget);
  });
}
