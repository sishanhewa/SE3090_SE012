import 'dart:convert';
import 'dart:async';
import 'package:flutter/material.dart';
import '../../../core/api/api_client.dart';
import '../../interviews/screens/my_interviews_screen.dart';

class ApplicationDetailScreen extends StatefulWidget {
  final String applicationId;

  const ApplicationDetailScreen({Key? key, required this.applicationId}) : super(key: key);

  @override
  _ApplicationDetailScreenState createState() => _ApplicationDetailScreenState();
}

class _ApplicationDetailScreenState extends State<ApplicationDetailScreen> {
  final ApiClient _apiClient = ApiClient();
  Map<String, dynamic>? _application;
  bool _isLoading = true;



  Future<void> _fetchApplicationDetails() async {
    try {
      final response = await _apiClient.get('/applications/${widget.applicationId}');
      if (response.statusCode == 200) {
        setState(() {
          _application = jsonDecode(response.body);
          _isLoading = false;
        });
      } else {
        setState(() => _isLoading = false);
      }
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  Future<void> _withdrawApplication() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Withdraw Application'),
        content: const Text('Are you sure you want to withdraw this application?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancel')),
          TextButton(
            onPressed: () => Navigator.pop(context, true), 
            child: const Text('Withdraw', style: TextStyle(color: Colors.red)),
          ),
        ],
      ),
    );

    if (confirm != true) return;

    try {
      final response = await _apiClient.post('/applications/${widget.applicationId}/withdraw');
      if (response.statusCode == 204) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Application withdrawn')));
        _fetchApplicationDetails();
      } else {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Failed to withdraw application')));
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Network error')));
    }
  }

  Map<String, dynamic>? _offer;
  bool _isLoadingOffer = false;

  Future<void> _fetchOffer() async {
    if (_application?['status'] != 'Offered' && _application?['status'] != 'Hired') return;
    
    setState(() => _isLoadingOffer = true);
    try {
      final response = await _apiClient.get('/offers?applicationId=${widget.applicationId}');
      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        if (data['items'] != null && data['items'].isNotEmpty) {
          setState(() {
            _offer = data['items'][0];
          });
        }
      }
    } catch (e) {
      // Ignore
    } finally {
      setState(() => _isLoadingOffer = false);
    }
  }

  Timer? _pollingTimer;

  @override
  void initState() {
    super.initState();
    _fetchApplicationDetails().then((_) => _fetchOffer());
    // Real-time polling: refresh status every 15 seconds
    _pollingTimer = Timer.periodic(const Duration(seconds: 15), (_) {
      _fetchApplicationDetails().then((_) => _fetchOffer());
    });
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    super.dispose();
  }

  Future<void> _updateOfferStatus(String status) async {
    if (_offer == null) return;
    try {
      final response = await _apiClient.patch('/offers/${_offer!['id']}/status', {'status': status});
      if (response.statusCode == 204 || response.statusCode == 200) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Offer $status successfully!')));
        _fetchApplicationDetails().then((_) => _fetchOffer());
      } else {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Failed to update offer status')));
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Network error')));
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return Scaffold(
        appBar: AppBar(title: const Text('Application Details')),
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    if (_application == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Application Details')),
        body: const Center(child: Text('Failed to load application details')),
      );
    }

    final bool canWithdraw = _application!['status'] != 'Rejected' && 
                             _application!['status'] != 'Withdrawn' && 
                             _application!['status'] != 'Hired';

    return Scaffold(
      appBar: AppBar(title: const Text('Application Details')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              _application!['jobTitle'] ?? 'Job Title',
              style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 4),
            Text(
              _application!['companyName'] ?? 'Company',
              style: const TextStyle(fontSize: 16, color: Colors.grey),
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
                    Text(_application!['status'], style: const TextStyle(fontSize: 16)),
                    const Divider(height: 24),
                    const Text('Applied On', style: TextStyle(fontWeight: FontWeight.bold)),
                    const SizedBox(height: 4),
                    Text(DateTime.parse(_application!['submittedAt']).toLocal().toString().split('.')[0]),
                  ],
                ),
              ),
            ),
            
            if (_application!['status'] == 'Screening') ...[
              const SizedBox(height: 16),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.purple.shade50,
                  border: Border.all(color: Colors.purple.shade200),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Row(
                  children: [
                    const SizedBox(
                      width: 24,
                      height: 24,
                      child: CircularProgressIndicator(strokeWidth: 2, color: Colors.purple),
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: const [
                          Text('AI Screening in Progress', style: TextStyle(fontWeight: FontWeight.bold, color: Colors.purple)),
                          Text('Your application is currently being evaluated by TalentFlow AI.', style: TextStyle(fontSize: 12, color: Colors.black87)),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ],
            
            if (_isLoadingOffer) const Center(child: CircularProgressIndicator()),
            if (_offer != null && (_offer!['status'] == 'Sent' || _offer!['status'] == 'Accepted' || _offer!['status'] == 'Rejected')) ...[
              const SizedBox(height: 24),
              const Text(
                'Job Offer',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Colors.blue),
              ),
              const SizedBox(height: 8),
              Card(
                color: Colors.blue.shade50,
                margin: EdgeInsets.zero,
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Position: ${_offer!['position']}', style: const TextStyle(fontWeight: FontWeight.bold)),
                      const SizedBox(height: 8),
                      Text('Salary: \$${_offer!['salary']}'),
                      const SizedBox(height: 8),
                      Text('Start Date: ${DateTime.parse(_offer!['startDate']).toLocal().toString().split(' ')[0]}'),
                      const SizedBox(height: 8),
                      Text('Status: ${_offer!['status']}'),
                      if (_offer!['additionalTerms'] != null) ...[
                        const SizedBox(height: 8),
                        Text('Terms: ${_offer!['additionalTerms']}'),
                      ],
                      if (_offer!['status'] == 'Sent') ...[
                        const SizedBox(height: 16),
                        Row(
                          children: [
                            Expanded(
                              child: ElevatedButton(
                                style: ElevatedButton.styleFrom(backgroundColor: Colors.green),
                                onPressed: () => _updateOfferStatus('Accepted'),
                                child: const Text('Accept Offer', style: TextStyle(color: Colors.white)),
                              ),
                            ),
                            const SizedBox(width: 16),
                            Expanded(
                              child: ElevatedButton(
                                style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
                                onPressed: () => _updateOfferStatus('Rejected'),
                                child: const Text('Reject', style: TextStyle(color: Colors.white)),
                              ),
                            ),
                          ],
                        )
                      ]
                    ],
                  ),
                ),
              ),
            ],

            const SizedBox(height: 24),
            const Text(
              'Cover Letter',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            Card(
              margin: EdgeInsets.zero,
              child: Padding(
                padding: const EdgeInsets.all(16.0),
                child: Text(_application!['coverLetter'] ?? 'No cover letter provided.'),
              ),
            ),

            // Status Timeline
            const SizedBox(height: 24),
            _buildStatusTimeline(),

            // View Interviews button
            if (_application!['status'] == 'Interview' ||
                _application!['status'] == 'Offered' ||
                _application!['status'] == 'Hired') ...[
              const SizedBox(height: 16),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  icon: const Icon(Icons.event),
                  label: const Text('View My Interviews'),
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 14),
                  ),
                  onPressed: () {
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (context) => MyInterviewsScreen(
                          applicationId: widget.applicationId,
                        ),
                      ),
                    );
                  },
                ),
              ),
            ],

            if (canWithdraw) ...[
              const SizedBox(height: 32),
              SizedBox(
                width: double.infinity,
                child: OutlinedButton.icon(
                  icon: const Icon(Icons.cancel, color: Colors.red),
                  label: const Text('Withdraw Application', style: TextStyle(color: Colors.red)),
                  style: OutlinedButton.styleFrom(
                    side: const BorderSide(color: Colors.red),
                    padding: const EdgeInsets.symmetric(vertical: 16),
                  ),
                  onPressed: _withdrawApplication,
                ),
              ),
            ]
          ],
        ),
      ),
    );
  }

  Widget _buildStatusTimeline() {
    final statuses = ['Submitted', 'Screening', 'Shortlisted', 'Interview', 'Offered', 'Hired'];
    final currentStatus = _application!['status'] ?? '';
    final currentIndex = statuses.indexOf(currentStatus);
    final isTerminal = currentStatus == 'Rejected' || currentStatus == 'Withdrawn';

    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Application Progress', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            const SizedBox(height: 16),
            if (isTerminal)
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.red.shade50,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.red.shade200),
                ),
                child: Row(
                  children: [
                    Icon(Icons.cancel, color: Colors.red.shade400),
                    const SizedBox(width: 8),
                    Text(
                      'Application $currentStatus',
                      style: TextStyle(fontWeight: FontWeight.bold, color: Colors.red.shade700),
                    ),
                  ],
                ),
              )
            else
              ...List.generate(statuses.length, (index) {
                final isCompleted = index <= currentIndex;
                final isCurrent = index == currentIndex;
                return Row(
                  children: [
                    Column(
                      children: [
                        Container(
                          width: 24,
                          height: 24,
                          decoration: BoxDecoration(
                            shape: BoxShape.circle,
                            color: isCompleted ? Colors.green : Colors.grey.shade300,
                            border: isCurrent
                                ? Border.all(color: Colors.green, width: 3)
                                : null,
                          ),
                          child: isCompleted
                              ? const Icon(Icons.check, size: 14, color: Colors.white)
                              : null,
                        ),
                        if (index < statuses.length - 1)
                          Container(
                            width: 2,
                            height: 24,
                            color: isCompleted ? Colors.green : Colors.grey.shade300,
                          ),
                      ],
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Text(
                        statuses[index],
                        style: TextStyle(
                          fontWeight: isCurrent ? FontWeight.bold : FontWeight.normal,
                          color: isCompleted ? Colors.black87 : Colors.grey,
                        ),
                      ),
                    ),
                  ],
                );
              }),
          ],
        ),
      ),
    );
  }
}
