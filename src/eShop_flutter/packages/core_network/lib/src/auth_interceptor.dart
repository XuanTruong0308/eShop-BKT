import 'dart:convert';
import 'package:dio/dio.dart';

class AuthInterceptor extends Interceptor {
  //Biến này là một hàm (callback) để xin token từ bộ nhớ máy
  final Future<String?> Function() getToken;

  AuthInterceptor(this.getToken);

  @override
  void onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    //1. Chặn request lại và lấy token
    final token = await getToken();
    
    // In log ra terminal để debug
    print('--- AuthInterceptor Debug ---');
    print('Request URI: ${options.uri}');
    print('Request Method: ${options.method}');
    print('Request Headers: ${options.headers}');
    print('Token: ${token != null ? "HAS_TOKEN (length: ${token.length})" : "NULL"}');
    
    if (token != null) {
      options.headers['Authorization'] = 'Bearer $token';
      print('Authorization header attached.');
      
      // Decode JWT và in claims ra console
      try {
        final parts = token.split('.');
        if (parts.length == 3) {
          final payload = parts[1];
          // Giải mã Base64Url
          var normalized = payload.replaceAll('-', '+').replaceAll('_', '/');
          while (normalized.length % 4 != 0) {
            normalized += '=';
          }
          final decoded = utf8.decode(base64.decode(normalized));
          print('JWT Claims: $decoded');
        }
      } catch (e) {
        print('Error decoding JWT for log: $e');
      }
    } else {
      print('No token, Authorization header NOT attached.');
    }
    print('-------------------------------');

    //3. Cho phép request tiếp tục bay đi
    super.onRequest(options, handler);
  }
}
