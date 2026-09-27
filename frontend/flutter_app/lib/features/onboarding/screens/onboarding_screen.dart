import 'package:flutter/material.dart';
import '../../core/api/api_client.dart';
import '../../core/auth/auth_provider.dart';
import 'package:provider/provider.dart';
import 'dart:convert';

class OnboardingScreen extends StatefulWidget {
  const OnboardingScreen({Key? key}) : super(key: key);

  @override
  _OnboardingScreenState createState() => _OnboardingScreenState();
}

class _OnboardingScreenState extends State<OnboardingScreen> {
  final ApiClient _apiClient = ApiClient();
  Map<String, dynamic>? _employee;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _fetchEmployeeDetails();
  }

  Future<void> _fetchEmployeeDetails() async {
    try {
      final auth = Provider.of<AuthProvider>(context, listen: false);
      if (auth.user == null) return;
      
      // We don't have a direct /employees/me endpoint, but we could fetch profile or try to get employee by userId.
      // Since this is a simple mock, we will just show a static onboarding checklist for the employee.
      setState(() => _isLoading = false);
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
    }

    return Scaffold(
      appBar: AppBar(title: const Text('My Onboarding')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Center(
              child: Icon(Icons.rocket_launch, size: 64, color: Colors.blue),
            ),
            const SizedBox(height: 24),
            const Text(
              'Welcome to the Team!',
              style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 16),
            const Text(
              'Please ensure you complete the following tasks with your HR representative to finish your onboarding process.',
              style: TextStyle(fontSize: 16, color: Colors.grey),
            ),
            const SizedBox(height: 32),
            Card(
              margin: EdgeInsets.zero,
              child: Column(
                children: const [
                  ListTile(
                    leading: Icon(Icons.check_circle_outline, color: Colors.grey),
                    title: Text('Sign all tax and legal documents'),
                  ),
                  Divider(height: 1),
                  ListTile(
                    leading: Icon(Icons.check_circle_outline, color: Colors.grey),
                    title: Text('Collect company equipment (Laptop, etc.)'),
                  ),
                  Divider(height: 1),
                  ListTile(
                    leading: Icon(Icons.check_circle_outline, color: Colors.grey),
                    title: Text('Setup IT and email accounts'),
                  ),
                  Divider(height: 1),
                  ListTile(
                    leading: Icon(Icons.check_circle_outline, color: Colors.grey),
                    title: Text('Attend HR orientation session'),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 32),
            const Center(
              child: Text(
                'Your status will automatically update to Active once HR confirms these tasks are complete.',
                textAlign: TextAlign.center,
                style: TextStyle(fontStyle: FontStyle.italic, color: Colors.grey),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
