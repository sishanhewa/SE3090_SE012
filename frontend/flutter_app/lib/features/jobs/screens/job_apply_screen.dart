import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:file_picker/file_picker.dart';
import '../../../core/api/api_client.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

class JobApplyScreen extends StatefulWidget {
  final String jobId;
  final String jobTitle;

  const JobApplyScreen({super.key, required this.jobId, required this.jobTitle});

  @override
  State<JobApplyScreen> createState() => _JobApplyScreenState();
}

class _JobApplyScreenState extends State<JobApplyScreen> {
  final ApiClient _apiClient = ApiClient();
  final _formKey = GlobalKey<FormState>();
  final _coverLetterController = TextEditingController();
  bool _isSubmitting = false;

  // CV file state
  PlatformFile? _selectedFile;
  String? _uploadedDocumentId;
  bool _isUploading = false;
  String? _createdApplicationId;

  Future<void> _pickFile() async {
    try {
      FilePickerResult? result = await FilePicker.platform.pickFiles(
        type: FileType.custom,
        allowedExtensions: ['pdf', 'docx'],
        withData: true,
      );

      if (result != null) {
        setState(() {
          _selectedFile = result.files.single;
          _uploadedDocumentId = null; // Reset since we have a new file
        });
      }
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Failed to select file.')),
      );
    }
  }

  Future<String?> _uploadResume(String applicationId) async {
    if (_selectedFile == null) return null;
    if (_uploadedDocumentId != null) return _uploadedDocumentId;

    setState(() => _isUploading = true);

    try {
      final prefs = await SharedPreferences.getInstance();
      final token = prefs.getString('access_token');

      var request = http.MultipartRequest(
        'POST',
        Uri.parse('${ApiClient.baseUrl}/applications/$applicationId/documents'),
      );

      if (token != null) {
        request.headers['Authorization'] = 'Bearer $token';
      }

      if (_selectedFile!.bytes != null) {
        request.files.add(http.MultipartFile.fromBytes(
          'file',
          _selectedFile!.bytes!,
          filename: _selectedFile!.name,
        ));
      } else if (_selectedFile!.path != null) {
        request.files.add(await http.MultipartFile.fromPath(
          'file',
          _selectedFile!.path!,
        ));
      } else {
        throw StateError('The selected CV could not be read.');
      }

      final response = await request.send();
      final responseBody = await response.stream.bytesToString();

      if (response.statusCode == 200 || response.statusCode == 201) {
        final data = jsonDecode(responseBody);
        setState(() {
          _uploadedDocumentId = data['id'];
          _isUploading = false;
        });
        return data['id'];
      } else {
        setState(() => _isUploading = false);
        return null;
      }
    } catch (e) {
      setState(() => _isUploading = false);
      return null;
    }
  }

  Future<void> _submitApplication() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedFile == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Select a PDF or DOCX CV before applying.')),
      );
      return;
    }
    if (_selectedFile!.size > 10000000) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('CV must be smaller than 10 MB.')),
      );
      return;
    }

    setState(() => _isSubmitting = true);

    try {
      if (_createdApplicationId == null) {
        final response = await _apiClient.post(
          '/applications/jobs/${widget.jobId}',
          {'coverLetter': _coverLetterController.text.trim()},
        );
        if (response.statusCode != 201) {
          final error = jsonDecode(response.body);
          throw StateError(error is String ? error : (error['message'] ?? 'Application could not be submitted.'));
        }
        _createdApplicationId = jsonDecode(response.body)['id'] as String?;
        if (_createdApplicationId == null) throw StateError('Application response was incomplete.');
      }

      final resumeId = await _uploadResume(_createdApplicationId!);
      if (resumeId != null) {
        if (!mounted) return;
        setState(() => _isSubmitting = false);
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Application and CV submitted successfully.')),
        );
        Navigator.pop(context, true);
      } else {
        if (!mounted) return;
        setState(() => _isSubmitting = false);
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Application saved, but CV upload failed. Tap Retry CV upload.')),
        );
      }
    } catch (e) {
      if (!mounted) return;
      setState(() => _isSubmitting = false);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(e is StateError ? e.message.toString() : 'Network error. Please try again.')),
      );
    }
  }

  String _formatFileSize(int bytes) {
    if (bytes < 1024) return '$bytes B';
    if (bytes < 1024 * 1024) return '${(bytes / 1024).toStringAsFixed(1)} KB';
    return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
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

              // Resume Upload Section
              const Text(
                'Resume / CV',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),
              InkWell(
                onTap: _isUploading ? null : _pickFile,
                borderRadius: BorderRadius.circular(8),
                child: Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    border: Border.all(
                      color: _selectedFile != null ? Colors.green.shade300 : Colors.grey.shade300,
                      width: _selectedFile != null ? 2 : 1,
                    ),
                    borderRadius: BorderRadius.circular(8),
                    color: _selectedFile != null ? Colors.green.shade50 : Colors.grey.shade50,
                  ),
                  child: _selectedFile != null
                      ? Row(
                          children: [
                            Icon(Icons.description, color: Colors.green.shade600),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    _selectedFile!.name,
                                    style: const TextStyle(fontWeight: FontWeight.w600),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                  Text(
                                    _formatFileSize(_selectedFile!.size),
                                    style: TextStyle(fontSize: 12, color: Colors.grey.shade600),
                                  ),
                                ],
                              ),
                            ),
                            IconButton(
                              icon: const Icon(Icons.close, size: 20),
                              onPressed: () => setState(() {
                                _selectedFile = null;
                                _uploadedDocumentId = null;
                              }),
                            ),
                          ],
                        )
                      : Row(
                          children: [
                            Icon(Icons.cloud_upload_outlined, color: Colors.blue.shade400, size: 32),
                            const SizedBox(width: 16),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const Text('Tap to upload your CV', style: TextStyle(fontWeight: FontWeight.w600)),
                                  Text(
                                    'PDF, DOC, DOCX (max 10 MB)',
                                    style: TextStyle(fontSize: 12, color: Colors.grey.shade600),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
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
                      ? const Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            SizedBox(width: 20, height: 20, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2)),
                            SizedBox(width: 12),
                            Text('Submitting...', style: TextStyle(fontSize: 16)),
                          ],
                        )
                      : Text(_createdApplicationId == null ? 'Submit Application' : 'Retry CV upload', style: const TextStyle(fontSize: 16)),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
