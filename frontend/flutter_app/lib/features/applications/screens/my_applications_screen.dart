import 'dart:convert';
import 'package:flutter/material.dart';
import '../../core/api/api_client.dart';
import 'application_detail_screen.dart';

class MyApplicationsScreen extends StatefulWidget {
  const MyApplicationsScreen({Key? key}) : super(key: key);

  @override
  _MyApplicationsScreenState createState() => _MyApplicationsScreenState();
}

class _MyApplicationsScreenState extends State<MyApplicationsScreen> {
  final ApiClient _apiClient = ApiClient();
  List<dynamic> _applications = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _fetchApplications();
  }

  Future<void> _fetchApplications() async {
    try {
      final response = await _apiClient.get('/applications/mine');
      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        setState(() {
          _applications = data['items'] ?? [];
          _isLoading = false;
        });
      } else {
        setState(() => _isLoading = false);
      }
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  Color _getStatusColor(String status) {
    switch (status) {
      case 'Submitted':
        return Colors.blue;
      case 'Screening':
      case 'Shortlisted':
      case 'Interview':
        return Colors.orange;
      case 'Offered':
      case 'Hired':
        return Colors.green;
      case 'Rejected':
      case 'Withdrawn':
        return Colors.red;
      default:
        return Colors.grey;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('My Applications')),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _applications.isEmpty
              ? const Center(child: Text("You haven't submitted any applications yet."))
              : ListView.builder(
                  itemCount: _applications.length,
                  itemBuilder: (context, index) {
                    final app = _applications[index];
                    return Card(
                      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                      child: ListTile(
                        title: Text(app['jobTitle'] ?? 'Unknown Job', style: const TextStyle(fontWeight: FontWeight.bold)),
                        subtitle: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(app['companyName'] ?? ''),
                            const SizedBox(height: 4),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                              decoration: BoxDecoration(
                                color: _getStatusColor(app['status']).withOpacity(0.1),
                                borderRadius: BorderRadius.circular(12),
                                border: Border.all(color: _getStatusColor(app['status'])),
                              ),
                              child: Text(
                                app['status'],
                                style: TextStyle(
                                  color: _getStatusColor(app['status']),
                                  fontSize: 12,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            )
                          ],
                        ),
                        trailing: const Icon(Icons.chevron_right),
                        isThreeLine: true,
                        onTap: () {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (context) => ApplicationDetailScreen(applicationId: app['id']),
                            ),
                          ).then((_) => _fetchApplications());
                        },
                      ),
                    );
                  },
                ),
    );
  }
}
