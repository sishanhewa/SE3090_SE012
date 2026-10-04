import 'dart:convert';
import 'package:flutter/material.dart';
import '../../../core/api/api_client.dart';

class EmployeeProfileScreen extends StatefulWidget {
  const EmployeeProfileScreen({Key? key}) : super(key: key);

  @override
  _EmployeeProfileScreenState createState() => _EmployeeProfileScreenState();
}

class _EmployeeProfileScreenState extends State<EmployeeProfileScreen> {
  final ApiClient _apiClient = ApiClient();
  Map<String, dynamic>? _employee;
  List<dynamic> _onboardingTasks = [];
  bool _isLoading = true;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _fetchEmployeeProfile();
  }

  Future<void> _fetchEmployeeProfile() async {
    try {
      final response = await _apiClient.get('/employees/me');
      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        setState(() {
          _employee = data;
          _isLoading = false;
        });
        _fetchOnboardingTasks();
      } else if (response.statusCode == 404) {
        setState(() {
          _errorMessage = 'No employee profile found. You may not have been hired yet.';
          _isLoading = false;
        });
      } else {
        setState(() {
          _errorMessage = 'Failed to load employee profile.';
          _isLoading = false;
        });
      }
    } catch (e) {
      setState(() {
        _errorMessage = 'Network error. Please check your connection.';
        _isLoading = false;
      });
    }
  }

  Future<void> _fetchOnboardingTasks() async {
    if (_employee == null) return;
    try {
      final response = await _apiClient.get('/employees/${_employee!['id']}/onboarding');
      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        setState(() {
          _onboardingTasks = data is List ? data : (data['items'] ?? []);
        });
      }
    } catch (e) {
      // Ignore — onboarding might not be set up
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return Scaffold(
        appBar: AppBar(title: const Text('Employee Profile')),
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    if (_errorMessage != null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Employee Profile')),
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(32),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(Icons.person_off, size: 64, color: Colors.grey.shade400),
                const SizedBox(height: 16),
                Text(
                  _errorMessage!,
                  textAlign: TextAlign.center,
                  style: TextStyle(fontSize: 16, color: Colors.grey.shade600),
                ),
              ],
            ),
          ),
        ),
      );
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Employee Profile')),
      body: RefreshIndicator(
        onRefresh: _fetchEmployeeProfile,
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Profile card
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(20),
                  child: Column(
                    children: [
                      CircleAvatar(
                        radius: 40,
                        backgroundColor: Theme.of(context).colorScheme.primary,
                        child: Text(
                          (_employee!['name'] ?? 'E')[0].toUpperCase(),
                          style: const TextStyle(fontSize: 32, color: Colors.white),
                        ),
                      ),
                      const SizedBox(height: 12),
                      Text(
                        _employee!['name'] ?? 'Employee',
                        style: const TextStyle(fontSize: 22, fontWeight: FontWeight.bold),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        _employee!['position'] ?? '',
                        style: TextStyle(fontSize: 16, color: Colors.grey.shade600),
                      ),
                      const SizedBox(height: 8),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                        decoration: BoxDecoration(
                          color: _getStatusColor(_employee!['status'] ?? '').withOpacity(0.1),
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(color: _getStatusColor(_employee!['status'] ?? '')),
                        ),
                        child: Text(
                          _employee!['status'] ?? 'Unknown',
                          style: TextStyle(
                            color: _getStatusColor(_employee!['status'] ?? ''),
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Details card
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Employment Details',
                        style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                      ),
                      const Divider(height: 24),
                      _buildDetailRow(Icons.badge, 'Employee No.', _employee!['employeeNumber'] ?? ''),
                      _buildDetailRow(Icons.calendar_today, 'Start Date',
                          _employee!['startDate'] != null
                              ? DateTime.parse(_employee!['startDate']).toLocal().toString().split(' ')[0]
                              : 'N/A'),
                      _buildDetailRow(Icons.business, 'Department',
                          _employee!['departmentName'] ?? 'N/A'),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Onboarding tasks
              if (_onboardingTasks.isNotEmpty) ...[
                const Text(
                  'Onboarding Tasks',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 8),
                ..._onboardingTasks.map((task) => Card(
                  margin: const EdgeInsets.only(bottom: 8),
                  child: CheckboxListTile(
                    title: Text(
                      task['taskName'] ?? task['name'] ?? 'Task',
                      style: TextStyle(
                        decoration: task['isCompleted'] == true
                            ? TextDecoration.lineThrough
                            : null,
                      ),
                    ),
                    subtitle: task['description'] != null
                        ? Text(task['description'])
                        : null,
                    value: task['isCompleted'] ?? false,
                    onChanged: null, // Read-only for now
                    controlAffinity: ListTileControlAffinity.leading,
                  ),
                )).toList(),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildDetailRow(IconData icon, String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        children: [
          Icon(icon, size: 20, color: Colors.grey),
          const SizedBox(width: 12),
          Text(label, style: const TextStyle(color: Colors.grey)),
          const Spacer(),
          Text(value, style: const TextStyle(fontWeight: FontWeight.w600)),
        ],
      ),
    );
  }

  Color _getStatusColor(String status) {
    switch (status) {
      case 'Onboarding':
        return Colors.blue;
      case 'Active':
        return Colors.green;
      case 'Terminated':
        return Colors.red;
      default:
        return Colors.grey;
    }
  }
}
