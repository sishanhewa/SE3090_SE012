import 'dart:convert';
import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

class ApiClient {
  static const String _configuredBaseUrl = String.fromEnvironment('API_BASE_URL');
  static String get baseUrl => _configuredBaseUrl.isNotEmpty
      ? _configuredBaseUrl
      : kIsWeb || !Platform.isAndroid
          ? 'http://localhost:5155/api'
          : 'http://10.0.2.2:5155/api';

  Future<Map<String, String>> _getHeaders() async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString('access_token');
    return {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    };
  }

  Future<http.Response> get(String endpoint) async {
    final headers = await _getHeaders();
    return await http.get(Uri.parse('$baseUrl$endpoint'), headers: headers);
  }

  Future<http.Response> post(String endpoint, [Map<String, dynamic>? data]) async {
    final headers = await _getHeaders();
    return await http.post(
      Uri.parse('$baseUrl$endpoint'),
      headers: headers,
      body: data != null ? jsonEncode(data) : null,
    );
  }

  Future<http.Response> put(String endpoint, [Map<String, dynamic>? data]) async {
    final headers = await _getHeaders();
    return await http.put(
      Uri.parse('$baseUrl$endpoint'),
      headers: headers,
      body: data != null ? jsonEncode(data) : null,
    );
  }

  Future<http.Response> patch(String endpoint, [Map<String, dynamic>? data]) async {
    final headers = await _getHeaders();
    return await http.patch(
      Uri.parse('$baseUrl$endpoint'),
      headers: headers,
      body: data != null ? jsonEncode(data) : null,
    );
  }
}
