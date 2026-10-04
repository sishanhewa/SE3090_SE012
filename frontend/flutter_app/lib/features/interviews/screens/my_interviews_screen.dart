import 'dart:convert';
import 'package:flutter/material.dart';
import '../../../core/api/api_client.dart';

class MyInterviewsScreen extends StatefulWidget {
  final String applicationId;

  const MyInterviewsScreen({Key? key, required this.applicationId}) : super(key: key);

  @override
  _MyInterviewsScreenState createState() => _MyInterviewsScreenState();
}

class _MyInterviewsScreenState extends State<MyInterviewsScreen> {
  final ApiClient _apiClient = ApiClient();
  List<dynamic> _interviews = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _fetchInterviews();
  }

  Future<void> _fetchInterviews() async {
    try {
      final response = await _apiClient.get('/interviews/application/${widget.applicationId}');
      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        setState(() {
          _interviews = data['items'] ?? [];
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
      case 'Proposed':
        return Colors.blue;
      case 'Confirmed':
        return Colors.teal;
      case 'InProgress':
        return Colors.orange;
      case 'Completed':
        return Colors.green;
      case 'Cancelled':
        return Colors.red;
      default:
        return Colors.grey;
    }
  }

  IconData _getStatusIcon(String status) {
    switch (status) {
      case 'Proposed':
        return Icons.schedule;
      case 'Confirmed':
        return Icons.check_circle_outline;
      case 'InProgress':
        return Icons.videocam;
      case 'Completed':
        return Icons.done_all;
      case 'Cancelled':
        return Icons.cancel;
      default:
        return Icons.help_outline;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('My Interviews')),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _interviews.isEmpty
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.event_busy, size: 64, color: Colors.grey.shade400),
                      const SizedBox(height: 16),
                      Text(
                        'No interviews scheduled yet',
                        style: TextStyle(fontSize: 16, color: Colors.grey.shade600),
                      ),
                    ],
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _fetchInterviews,
                  child: ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: _interviews.length,
                    itemBuilder: (context, index) {
                      final interview = _interviews[index];
                      final status = interview['status'] ?? 'Unknown';
                      final scheduledAt = interview['scheduledAt'] != null
                          ? DateTime.parse(interview['scheduledAt']).toLocal()
                          : null;
                      final duration = interview['durationMinutes'] ?? 60;

                      return Card(
                        margin: const EdgeInsets.only(bottom: 12),
                        elevation: 2,
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              // Status header
                              Row(
                                children: [
                                  Icon(
                                    _getStatusIcon(status),
                                    color: _getStatusColor(status),
                                    size: 20,
                                  ),
                                  const SizedBox(width: 8),
                                  Container(
                                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                                    decoration: BoxDecoration(
                                      color: _getStatusColor(status).withOpacity(0.1),
                                      borderRadius: BorderRadius.circular(12),
                                      border: Border.all(color: _getStatusColor(status)),
                                    ),
                                    child: Text(
                                      status,
                                      style: TextStyle(
                                        color: _getStatusColor(status),
                                        fontWeight: FontWeight.bold,
                                        fontSize: 12,
                                      ),
                                    ),
                                  ),
                                  const Spacer(),
                                  Text(
                                    '${duration}min',
                                    style: const TextStyle(
                                      fontWeight: FontWeight.w500,
                                      color: Colors.grey,
                                    ),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 12),

                              // Date & Time
                              if (scheduledAt != null) ...[
                                Row(
                                  children: [
                                    const Icon(Icons.calendar_today, size: 16, color: Colors.grey),
                                    const SizedBox(width: 8),
                                    Text(
                                      '${scheduledAt.day}/${scheduledAt.month}/${scheduledAt.year}',
                                      style: const TextStyle(fontWeight: FontWeight.w600),
                                    ),
                                    const SizedBox(width: 16),
                                    const Icon(Icons.access_time, size: 16, color: Colors.grey),
                                    const SizedBox(width: 8),
                                    Text(
                                      '${scheduledAt.hour.toString().padLeft(2, '0')}:${scheduledAt.minute.toString().padLeft(2, '0')}',
                                      style: const TextStyle(fontWeight: FontWeight.w600),
                                    ),
                                  ],
                                ),
                                const SizedBox(height: 8),
                              ],

                              // Location / Meeting URL
                              if (interview['location'] != null && interview['location'].toString().isNotEmpty) ...[
                                Row(
                                  children: [
                                    const Icon(Icons.location_on, size: 16, color: Colors.grey),
                                    const SizedBox(width: 8),
                                    Expanded(
                                      child: Text(
                                        interview['location'],
                                        style: const TextStyle(color: Colors.black87),
                                      ),
                                    ),
                                  ],
                                ),
                                const SizedBox(height: 4),
                              ],
                              if (interview['meetingUrl'] != null && interview['meetingUrl'].toString().isNotEmpty) ...[
                                Row(
                                  children: [
                                    const Icon(Icons.video_call, size: 16, color: Colors.blue),
                                    const SizedBox(width: 8),
                                    Expanded(
                                      child: Text(
                                        interview['meetingUrl'],
                                        style: const TextStyle(
                                          color: Colors.blue,
                                          decoration: TextDecoration.underline,
                                        ),
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                      ),
                                    ),
                                  ],
                                ),
                              ],

                              // Notes
                              if (interview['notes'] != null && interview['notes'].toString().isNotEmpty) ...[
                                const SizedBox(height: 8),
                                const Divider(),
                                const SizedBox(height: 4),
                                Text(
                                  interview['notes'],
                                  style: TextStyle(fontSize: 13, color: Colors.grey.shade700),
                                ),
                              ],
                            ],
                          ),
                        ),
                      );
                    },
                  ),
                ),
    );
  }
}
