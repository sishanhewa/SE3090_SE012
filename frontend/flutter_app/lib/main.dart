import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:go_router/go_router.dart';

import 'core/auth/auth_provider.dart';
import 'features/auth/screens/login_screen.dart';
import 'core/navigation/main_navigation_screen.dart';

final authProvider = AuthProvider();

final router = GoRouter(
  initialLocation: '/login',
  refreshListenable: authProvider,
  redirect: (context, state) {
    final isLoggedIn = authProvider.isAuthenticated;
    final location = state.uri.toString();

    if (!isLoggedIn && location != '/login') return '/login';
    if (isLoggedIn && (location == '/login' || location == '/')) return '/home';

    return null;
  },
  routes: [
    GoRoute(
      path: '/login',
      builder: (context, state) => const LoginScreen(),
    ),
    GoRoute(
      path: '/home',
      builder: (context, state) => const MainNavigationScreen(),
    ),
  ],
);

void main() {
  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider.value(value: authProvider),
      ],
      child: const TalentFlowApp(),
    ),
  );
}

class TalentFlowApp extends StatelessWidget {
  const TalentFlowApp({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      debugShowCheckedModeBanner: false,
      title: 'TalentFlow Candidate',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.blue),
        useMaterial3: true,
      ),
      routerConfig: router,
    );
  }
}
