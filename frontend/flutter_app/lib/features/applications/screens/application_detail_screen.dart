import 'dart:convert';
import 'package:flutter/material.dart';
import '../../core/api/api_client.dart';

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

  @override
  void initState() {
    super.initState();
    _fetchApplicationDetails().then((_) => _fetchOffer());
  }

  Future<void> _updateOfferStatus(String status) async {
    if (_offer == null) return;
    try {
      final response = await _apiClient.patch('/offers/${_offer!['id']}/status', jsonEncode({'status': status}));
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
}

