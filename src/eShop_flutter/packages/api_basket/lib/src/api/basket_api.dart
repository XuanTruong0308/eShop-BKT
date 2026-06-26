import 'package:core_network/core_network.dart';
import '../models/customer_basket.dart';

class BasketApi {
  final NetworkClient _client;

  BasketApi(this._client);

  Future<CustomerBasket> getBasket() async {
    try {
      final response = await _client.get('/api/basket');
      if (response.statusCode == 200) {
        return CustomerBasket.fromJson(response.data as Map<String, dynamic>);
      }
      throw Exception('Không thể lấy thông tin giỏ hàng');
    } catch (e) {
      throw Exception('Lỗi kết nối Basket API: $e');
    }
  }

  Future<CustomerBasket> updateBasket(CustomerBasket basket) async {
    try {
      final response = await _client.post('/api/basket', data: basket.toJson());
      if (response.statusCode == 200) {
        return CustomerBasket.fromJson(response.data as Map<String, dynamic>);
      }
      throw Exception('Không thể cập nhật giỏ hàng');
    } catch (e) {
      throw Exception('Lỗi kết nối Basket API: $e');
    }
  }

  Future<void> deleteBasket() async {
    try {
      final response = await _client.delete('/api/basket');
      if (response.statusCode == 200) {
        return;
      }
      throw Exception('Không thể xóa giỏ hàng');
    } catch (e) {
      throw Exception('Lỗi kết nối Basket API: $e');
    }
  }
}
