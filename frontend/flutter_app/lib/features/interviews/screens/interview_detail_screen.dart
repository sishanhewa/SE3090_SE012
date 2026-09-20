import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../core/api/api_client.dart';

class InterviewDetailScreen extends StatefulWidget {
  final String interviewId;

  const InterviewDetailScreen({Key? key, required this.interviewId}) : super(key: key);

  @override
  _InterviewDetailScreenState createState() => _InterviewDetailScreenState();
}

class _InterviewDetailScreenState extends State<InterviewDetailScreen> {
  final ApiClient _apiClient = ApiClient();
  Map<String, dynamic>? _interview;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _fetchInterviewDetails();
  }

  Future<void> _fetchInterviewDetails() async {
    try {
      final response = await _apiClient.get('/interviews/${widget.interviewId}');
      if (response.statusCode == 200) {
        setState(() {
          _interview = jsonDecode(response.body);
          _isLoading = false;
        });
      } else {
        setState(() => _isLoading = false);
      }
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return Scaffold(
        appBar: AppBar(title: const Text('Interview Details')),
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    if (_interview == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Interview Details')),
        body: const Center(child: Text('Failed to load interview details')),
      );
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Interview Details')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Interview Schedule',
              style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 16),
            Card(
              margin: EdgeInsets.zero,
              child: Padding(
                padding: const EdgeInsets.all(16.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('Status', style: TextStyle(fontWeight: FontWeight.bold)),
                    const SizedBox(height: 4),
                    Text(_interview!['status'], style: const TextStyle(fontSize: 16)),
                    const Divider(height: 24),
                    
                    const Text('Scheduled At', style: TextStyle(fontWeight: FontWeight.bold)),
                    const SizedBox(height: 4),
                    Text(DateTime.parse(_interview!['scheduledAt']).toLocal().toString().split('.')[0]),
                    const Divider(height: 24),
                    
                    const Text('Duration', style: TextStyle(fontWeight: FontWeight.bold)),
                    const SizedBox(height: 4),
                    Text('${_interview!['durationMinutes']} minutes'),
                    const Divider(height: 24),
                    
                    if (_interview!['location'] != null) ...[
                      const Text('Location', style: TextStyle(fontWeight: FontWeight.bold)),
                      const SizedBox(height: 4),
                      Text(_interview!['location']),
                      const Divider(height: 24),
                    ],

                    if (_interview!['notes'] != null) ...[
                      const Text('Notes', style: TextStyle(fontWeight: FontWeight.bold)),
                      const SizedBox(height: 4),
                      Text(_interview!['notes']),
                      const Divider(height: 24),
                    ],
                  ],
                ),
              ),
            ),
            const SizedBox(height: 24),
            
            if (_interview!['meetingUrl'] != null)
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  icon: const Icon(Icons.video_call),
                  label: const Text('Join Meeting', style: TextStyle(fontSize: 16)),
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 16),
                  ),
                  onPressed: () async {
                    final url = Uri.parse(_interview!['meetingUrl']);
                    if (await canLaunchUrl(url)) {
                      await launchUrl(url);
                    } else {
                      if (context.mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(content: Text('Could not open meeting link')),
                        );
                      }
                    }
                  },
                ),
              ),
          ],
        ),
      ),
    );
  }
}
