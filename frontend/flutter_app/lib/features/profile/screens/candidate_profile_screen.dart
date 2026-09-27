import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/api/api_client.dart';
import '../../core/auth/auth_provider.dart';

class CandidateProfileScreen extends StatefulWidget {
  const CandidateProfileScreen({Key? key}) : super(key: key);

  @override
  _CandidateProfileScreenState createState() => _CandidateProfileScreenState();
}

class _CandidateProfileScreenState extends State<CandidateProfileScreen> {
  final ApiClient _apiClient = ApiClient();
  final _formKey = GlobalKey<FormState>();
  
  bool _isLoading = true;
  bool _isSaving = false;
  Map<String, dynamic>? _profile;

  final _headlineController = TextEditingController();
  final _summaryController = TextEditingController();
  final _skillsController = TextEditingController();
  final _experienceController = TextEditingController();
  final _educationController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _fetchProfile();
  }

  Future<void> _fetchProfile() async {
    try {
      final response = await _apiClient.get('/candidateprofiles/me');
      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        setState(() {
          _profile = data;
          _headlineController.text = data['headline'] ?? '';
          _summaryController.text = data['professionalSummary'] ?? '';
          _skillsController.text = data['skills'] ?? '';
          _experienceController.text = data['experience'] ?? '';
          _educationController.text = data['education'] ?? '';
          _isLoading = false;
        });
      } else if (response.statusCode == 404) {
        // Profile doesn't exist yet
        setState(() => _isLoading = false);
      } else {
        setState(() => _isLoading = false);
      }
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  Future<void> _saveProfile() async {
    if (!_formKey.currentState!.validate()) return;
    
    setState(() => _isSaving = true);
    
    final payload = jsonEncode({
      'headline': _headlineController.text,
      'professionalSummary': _summaryController.text,
      'skills': _skillsController.text,
      'experience': _experienceController.text,
      'education': _educationController.text,
    });

    try {
      final isUpdate = _profile != null;
      final response = isUpdate 
          ? await _apiClient.put('/candidateprofiles/me', payload)
          : await _apiClient.post('/candidateprofiles', payload);
          
      if (response.statusCode == 200 || response.statusCode == 201) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Profile saved successfully!')));
        _fetchProfile();
      } else {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Failed to save profile')));
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Network error')));
    } finally {
      setState(() => _isSaving = false);
    }
  }

  @override
  void dispose() {
    _headlineController.dispose();
    _summaryController.dispose();
    _skillsController.dispose();
    _experienceController.dispose();
    _educationController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final authProvider = Provider.of<AuthProvider>(context, listen: false);

    if (_isLoading) {
      return Scaffold(
        appBar: AppBar(title: const Text('My Profile')),
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    return Scaffold(
      appBar: AppBar(
        title: const Text('My Profile'),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: () => authProvider.logout(),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (_profile == null) 
                Container(
                  padding: const EdgeInsets.all(12),
                  margin: const EdgeInsets.bottom(16),
                  color: Colors.amber.shade100,
                  child: const Text('Please complete your profile to apply for jobs.'),
                ),
                
              const Text('Headline', style: TextStyle(fontWeight: FontWeight.bold)),
              const SizedBox(height: 8),
              TextFormField(
                controller: _headlineController,
                decoration: const InputDecoration(border: OutlineInputBorder(), hintText: 'e.g. Senior Software Engineer'),
                validator: (val) => val == null || val.isEmpty ? 'Required' : null,
              ),
              const SizedBox(height: 16),
              
              const Text('Professional Summary', style: TextStyle(fontWeight: FontWeight.bold)),
              const SizedBox(height: 8),
              TextFormField(
                controller: _summaryController,
                maxLines: 4,
                decoration: const InputDecoration(border: OutlineInputBorder()),
              ),
              const SizedBox(height: 16),
              
              const Text('Skills (comma separated)', style: TextStyle(fontWeight: FontWeight.bold)),
              const SizedBox(height: 8),
              TextFormField(
                controller: _skillsController,
                decoration: const InputDecoration(border: OutlineInputBorder(), hintText: 'Java, Flutter, SQL'),
              ),
              const SizedBox(height: 16),
              
              const Text('Experience', style: TextStyle(fontWeight: FontWeight.bold)),
              const SizedBox(height: 8),
              TextFormField(
                controller: _experienceController,
                maxLines: 3,
                decoration: const InputDecoration(border: OutlineInputBorder()),
              ),
              const SizedBox(height: 16),
              
              const Text('Education', style: TextStyle(fontWeight: FontWeight.bold)),
              const SizedBox(height: 8),
              TextFormField(
                controller: _educationController,
                maxLines: 2,
                decoration: const InputDecoration(border: OutlineInputBorder()),
              ),
              const SizedBox(height: 32),
              
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  style: ElevatedButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 16)),
                  onPressed: _isSaving ? null : _saveProfile,
                  child: _isSaving 
                    ? const CircularProgressIndicator(color: Colors.white)
                    : const Text('Save Profile', style: TextStyle(fontSize: 16)),
                ),
              )
            ],
          ),
        ),
      ),
    );
  }
}
