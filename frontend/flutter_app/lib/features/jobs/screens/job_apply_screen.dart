import 'dart:convert';
import 'package:flutter/material.dart';
import '../../core/api/api_client.dart';

class JobApplyScreen extends StatefulWidget {
  final String jobId;
  final String jobTitle;

  const JobApplyScreen({Key? key, required this.jobId, required this.jobTitle}) : super(key: key);

  @override
  _JobApplyScreenState createState() => _JobApplyScreenState();
}

class _JobApplyScreenState extends State<JobApplyScreen> {
  final ApiClient _apiClient = ApiClient();
  final _formKey = GlobalKey<FormState>();
  final _coverLetterController = TextEditingController();
  bool _isSubmitting = false;

  Future<void> _submitApplication() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _isSubmitting = true);

    try {
      final response = await _apiClient.post(
        '/applications/jobs/${widget.jobId}',
        jsonEncode({
          'jobId': widget.jobId,
          'coverLetter': _coverLetterController.text,
          // resumeDocumentId could be passed here if resume upload was implemented in Flutter
        }),
      );

      setState(() => _isSubmitting = false);

      if (response.statusCode == 201) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Application submitted successfully!')),
        );
        Navigator.pop(context, true); // Return true to indicate success
      } else {
        final error = jsonDecode(response.body);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to submit: ${error['message'] ?? 'Unknown error'}')),
        );
      }
    } catch (e) {
      setState(() => _isSubmitting = false);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Network error. Please try again later.')),
      );
    }
  }

  @override
  void dispose() {
    _coverLetterController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Apply for Job')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Applying for: ${widget.jobTitle}',
                style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 24),
              const Text(
                'Cover Letter',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),
              TextFormField(
                controller: _coverLetterController,
                maxLines: 8,
                decoration: const InputDecoration(
                  hintText: 'Introduce yourself and explain why you are a good fit for this role...',
                  border: OutlineInputBorder(),
                ),
                validator: (value) {
                  if (value == null || value.trim().isEmpty) {
                    return 'Please provide a cover letter';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 24),
              // Placeholder for resume upload UI
              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  border: Border.all(color: Colors.grey.shade300),
                  borderRadius: BorderRadius.circular(8),
                  color: Colors.grey.shade50,
                ),
                child: Row(
                  children: [
                    const Icon(Icons.upload_file, color: Colors.blue),
                    const SizedBox(width: 16),
                    const Expanded(
                      child: Text('Resume Upload (Coming Soon)'),
                    ),
                    ElevatedButton(
                      onPressed: null, // Disabled for Sprint 1/2
                      child: const Text('Select File'),
                    )
                  ],
                ),
              ),
              const SizedBox(height: 32),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 16),
                  ),
                  onPressed: _isSubmitting ? null : _submitApplication,
                  child: _isSubmitting
                      ? const CircularProgressIndicator(color: Colors.white)
                      : const Text('Submit Application', style: TextStyle(fontSize: 16)),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
