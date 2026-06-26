import 'package:dio/dio.dart';
import 'auth_interceptor.dart';

class NetworkClient {
  late final Dio dio;

  NetworkClient({
    required String baseUrl,
    required Future<String?> Function() getToken,
  }) {
    dio = Dio(
      BaseOptions(
        baseUrl: baseUrl,
        connectTimeout: const Duration(seconds: 10), // Limit wait time is 10s
        receiveTimeout: const Duration(seconds: 10),
        headers: {
          'Accept': 'application/json',
        },
      ),
    );

    // Gắn trạm kiểm soát token vào đường ống của Dio
    dio.interceptors.add(AuthInterceptor(getToken));
  }

  // Hàm Get
  Future<Response> get(
    String path, {
    Map<String, dynamic>? queryParameters,
    Map<String, dynamic>? headers,
  }) async {
    return await dio.get(
      path,
      queryParameters: queryParameters,
      options: Options(headers: headers),
    );
  }

  // Hàm Post
  Future<Response> post(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    String? contentType,
    Map<String, dynamic>? headers,
  }) async {
    return await dio.post(
      path,
      data: data,
      queryParameters: queryParameters,
      options: Options(contentType: contentType, headers: headers),
    );
  }

  // Hàm Put
  Future<Response> put(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    String? contentType,
    Map<String, dynamic>? headers,
  }) async {
    return await dio.put(
      path,
      data: data,
      queryParameters: queryParameters,
      options: Options(contentType: contentType, headers: headers),
    );
  }

  // Hàm Delete
  Future<Response> delete(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    Map<String, dynamic>? headers,
  }) async {
    return await dio.delete(
      path,
      data: data,
      queryParameters: queryParameters,
      options: Options(headers: headers),
    );
  }
}
