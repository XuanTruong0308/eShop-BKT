import 'dart:convert';
import 'package:flutter/foundation.dart';

class SessionState {
  static String? token;

  // Lấy userId thực tế (sub claim) từ token JWT
  static String? get userId {
    if (token == null) return null;
    try {
      final parts = token!.split('.');
      if (parts.length == 3) {
        final payload = parts[1];
        var normalized = payload.replaceAll('-', '+').replaceAll('_', '/');
        while (normalized.length % 4 != 0) {
          normalized += '=';
        }
        final decoded = utf8.decode(base64.decode(normalized));
        final claims = json.decode(decoded) as Map<String, dynamic>;
        return claims['sub'] as String?;
      }
    } catch (e) {
      debugPrint('Error parsing userId from JWT: $e');
    }
    return null;
  }

  // Lấy userName thực tế từ token JWT
  static String? get userName {
    if (token == null) return null;
    try {
      final parts = token!.split('.');
      if (parts.length == 3) {
        final payload = parts[1];
        var normalized = payload.replaceAll('-', '+').replaceAll('_', '/');
        while (normalized.length % 4 != 0) {
          normalized += '=';
        }
        final decoded = utf8.decode(base64.decode(normalized));
        final claims = json.decode(decoded) as Map<String, dynamic>;
        return (claims['email'] ?? claims['unique_name'] ?? claims['preferred_username']) as String?;
      }
    } catch (e) {
      debugPrint('Error parsing userName from JWT: $e');
    }
    return null;
  }
}
