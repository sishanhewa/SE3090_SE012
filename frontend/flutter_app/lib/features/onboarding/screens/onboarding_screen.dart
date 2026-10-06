import 'dart:convert';
import 'package:flutter/material.dart';
import '../../../core/api/api_client.dart';

class OnboardingScreen extends StatefulWidget {
  const OnboardingScreen({super.key});

  @override
  State<OnboardingScreen> createState() => _OnboardingScreenState();
}

class _OnboardingScreenState extends State<OnboardingScreen> {
  final ApiClient _apiClient = ApiClient();
  Map<String, dynamic>? _employee;
  List<Map<String, dynamic>> _tasks = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _refresh();
  }

  Future<void> _refresh() async {
    setState(() { _loading = true; _error = null; });
    try {
      final employeeResponse = await _apiClient.get('/employees/me');
      if (employeeResponse.statusCode != 200) {
        if (mounted) setState(() { _employee = null; _tasks = []; _loading = false; });
        return;
      }
      final employee = jsonDecode(employeeResponse.body) as Map<String, dynamic>;
      final tasksResponse = await _apiClient.get('/employees/${employee['id']}/onboarding');
      if (tasksResponse.statusCode != 200) throw Exception('Tasks could not be loaded');
      final tasks = (jsonDecode(tasksResponse.body) as List)
          .map((item) => Map<String, dynamic>.from(item as Map)).toList();
      if (mounted) setState(() { _employee = employee; _tasks = tasks; _loading = false; });
    } catch (_) {
      if (mounted) setState(() { _error = 'Could not load onboarding. Please try again.'; _loading = false; });
    }
  }

  Future<void> _complete(String taskId) async {
    try {
      final response = await _apiClient.patch('/onboarding/tasks/$taskId', {'notes': ''});
      if (response.statusCode != 200) throw Exception('Task update failed');
      await _refresh();
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Could not complete this task. Please retry.')));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('My Onboarding')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(child: Column(mainAxisSize: MainAxisSize.min, children: [
                  Text(_error!), TextButton(onPressed: _refresh, child: const Text('Retry'))]))
              : _employee == null
                  ? const Center(child: Text('Your onboarding will appear here after your offer is accepted.'))
                  : RefreshIndicator(
                      onRefresh: _refresh,
                      child: ListView(padding: const EdgeInsets.all(16), children: [
                        Text('Welcome, ${_employee!['name'] ?? 'new teammate'}',
                            style: Theme.of(context).textTheme.headlineSmall),
                        const SizedBox(height: 8),
                        Text('Position: ${_employee!['position'] ?? ''}'),
                        Text('Status: ${_employee!['status'] ?? ''}'),
                        const SizedBox(height: 24),
                        if (_tasks.isEmpty)
                          const Text('Your hiring team has not assigned onboarding tasks yet.'),
                        for (final task in _tasks)
                          Card(child: CheckboxListTile(
                            title: Text(task['taskTitle']?.toString() ?? 'Task'),
                            subtitle: Text(task['taskDescription']?.toString() ??
                                (task['isMandatory'] == true ? 'Required' : 'Optional')),
                            value: task['isCompleted'] == true,
                            onChanged: task['isCompleted'] == true ? null : (_) => _complete(task['id'].toString()),
                          )),
                      ]),
                    ),
    );
  }
}
