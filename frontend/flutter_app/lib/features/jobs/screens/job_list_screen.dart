import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/api/api_client.dart';
import '../../core/auth/auth_provider.dart';
import 'job_detail_screen.dart';

class JobListScreen extends StatefulWidget {
  const JobListScreen({Key? key}) : super(key: key);

  @override
  _JobListScreenState createState() => _JobListScreenState();
}

class _JobListScreenState extends State<JobListScreen> {
  final ApiClient _apiClient = ApiClient();
  List<dynamic> _jobs = [];
  bool _isLoading = true;
  String _searchQuery = '';
  String? _selectedDepartment;

  // Mock list of departments for filtering
  final List<String> _departments = ['Engineering', 'Design', 'Marketing', 'Sales', 'HR'];

  @override
  void initState() {
    super.initState();
    _fetchJobs();
  }

  Future<void> _fetchJobs() async {
    setState(() => _isLoading = true);
    try {
      // Query builder
      String query = '?Status=1'; // Published
      if (_searchQuery.isNotEmpty) {
        query += '&Search=${Uri.encodeComponent(_searchQuery)}';
      }
      if (_selectedDepartment != null) {
        query += '&Department=${Uri.encodeComponent(_selectedDepartment!)}';
      }

      final response = await _apiClient.get('/jobs$query');
      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        setState(() {
          _jobs = data['items'] ?? [];
          _isLoading = false;
        });
      } else {
        setState(() => _isLoading = false);
      }
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  void _onSearchChanged(String value) {
    _searchQuery = value;
    _fetchJobs();
  }

  @override
  Widget build(BuildContext context) {
    final authProvider = Provider.of<AuthProvider>(context, listen: false);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Available Jobs'),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: () => authProvider.logout(),
          ),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16.0),
            child: TextField(
              decoration: InputDecoration(
                hintText: 'Search jobs...',
                prefixIcon: const Icon(Icons.search),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
              onSubmitted: _onSearchChanged,
            ),
          ),
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 16.0),
            child: Row(
              children: [
                FilterChip(
                  label: const Text('All'),
                  selected: _selectedDepartment == null,
                  onSelected: (selected) {
                    if (selected) {
                      setState(() => _selectedDepartment = null);
                      _fetchJobs();
                    }
                  },
                ),
                const SizedBox(width: 8),
                ..._departments.map((dept) => Padding(
                  padding: const EdgeInsets.only(right: 8.0),
                  child: FilterChip(
                    label: Text(dept),
                    selected: _selectedDepartment == dept,
                    onSelected: (selected) {
                      setState(() => _selectedDepartment = selected ? dept : null);
                      _fetchJobs();
                    },
                  ),
                )).toList(),
              ],
            ),
          ),
          const SizedBox(height: 8),
          Expanded(
            child: _isLoading
                ? const Center(child: CircularProgressIndicator())
                : _jobs.isEmpty
                    ? const Center(child: Text('No jobs found'))
                    : ListView.builder(
                        itemCount: _jobs.length,
                        itemBuilder: (context, index) {
                          final job = _jobs[index];
                          return Card(
                            margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                            child: ListTile(
                              title: Text(job['title']),
                              subtitle: Text(job['department'] ?? 'No department'),
                              trailing: const Icon(Icons.chevron_right),
                              onTap: () {
                                Navigator.push(
                                  context,
                                  MaterialPageRoute(
                                    builder: (context) => JobDetailScreen(jobId: job['id']),
                                  ),
                                );
                              },
                            ),
                          );
                        },
                      ),
          ),
        ],
      ),
    );
  }
}
