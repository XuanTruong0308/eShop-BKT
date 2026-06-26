import 'dart:math';
import 'package:core_network/core_network.dart';
import '../models/card_type.dart';
import '../models/order.dart';
import '../models/order_summary.dart';
import '../models/create_order_request.dart';

class OrderingApi {
  final NetworkClient _client;

  OrderingApi(this._client);

  String _generateUuid() {
    final random = Random();
    const hexDigits = '0123456789abcdef';
    String gen(int len) => List.generate(len, (_) => hexDigits[random.nextInt(16)]).join();
    return '${gen(8)}-${gen(4)}-4${gen(3)}-8${gen(3)}-${gen(12)}';
  }

  Future<List<CardType>> getCardTypes() async {
    try {
      final response = await _client.get(
        '/api/orders/cardtypes',
        queryParameters: {'api-version': '1.0'},
      );
      if (response.statusCode == 200) {
        final List<dynamic> data = response.data as List<dynamic>;
        return data
            .map((json) => CardType.fromJson(json as Map<String, dynamic>))
            .toList();
      }
      throw Exception('Không thể lấy danh sách loại thẻ');
    } catch (e) {
      throw Exception('Lỗi kết nối Ordering API: $e');
    }
  }

  Future<List<OrderSummary>> getOrders() async {
    try {
      final response = await _client.get(
        '/api/orders',
        queryParameters: {'api-version': '1.0'},
      );
      if (response.statusCode == 200) {
        final List<dynamic> data = response.data as List<dynamic>;
        return data
            .map((json) => OrderSummary.fromJson(json as Map<String, dynamic>))
            .toList();
      }
      throw Exception('Không thể lấy danh sách đơn hàng');
    } catch (e) {
      throw Exception('Lỗi kết nối Ordering API: $e');
    }
  }

  Future<Order> getOrder(int id) async {
    try {
      final response = await _client.get(
        '/api/orders/$id',
        queryParameters: {'api-version': '1.0'},
      );
      if (response.statusCode == 200) {
        return Order.fromJson(response.data as Map<String, dynamic>);
      }
      throw Exception('Không thể lấy chi tiết đơn hàng');
    } catch (e) {
      throw Exception('Lỗi kết nối Ordering API: $e');
    }
  }

  Future<void> createOrder(CreateOrderRequest request) async {
    try {
      final requestId = _generateUuid();
      final response = await _client.post(
        '/api/orders',
        data: request.toJson(),
        queryParameters: {'api-version': '1.0'},
        headers: {'x-requestid': requestId},
      );
      if (response.statusCode == 200) {
        return;
      }
      throw Exception('Không thể tạo đơn hàng');
    } catch (e) {
      throw Exception('Lỗi kết nối Ordering API: $e');
    }
  }
}
