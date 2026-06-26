import 'package:core_network/core_network.dart';
import '../models/login_request.dart';

class IdentityApi {
  final NetworkClient _client;

  IdentityApi(this._client);

  Future<String?> login(LoginRequest request) async {
    try {
      final response = await _client.post(
        '/connect/token',
        data: {
          'grant_type': 'password',
          'client_id': 'maui',
          'client_secret': 'secret',
          'username': request.userName,
          'password': request.password,
          'scope': 'openid profile orders basket',
        },
        contentType: 'application/x-www-form-urlencoded',
      );
      if (response.statusCode == 200) {
        return response
            .data['access_token']; //OpenID trả về 'access_token' không phải accessToken
      }
      return null;
    } catch (e) {
      throw Exception('Đăng nhập thất bại!');
    }
  }
}
