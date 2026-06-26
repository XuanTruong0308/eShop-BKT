import 'package:core_network/core_network.dart';
import '../models/catalog_brand.dart';
import '../models/catalog_result.dart';
import '../models/catalog_type.dart';

class CatalogApi {
  final NetworkClient _client;

  CatalogApi(this._client);

  //lấy  danh sách sản phẩm trang và hỗ trợ lọc theo brand/type
  Future<CatalogResult> getCatalogItems({
    required int pageIndex,
    required int pageSize,
    int? brandId,
    int? typeId,
  }) async {
    //xây dựng query parameters giống như hàm GetAllCatalogItemUri ở C# BE
    final Map<String, dynamic> queryParameters = {
      'pageIndex': pageIndex,
      'pageSize': pageSize,
      'api-version': '2.0',
    };

    if (brandId != null) {
      queryParameters['brand'] = brandId;
    }

    if (typeId != null) {
      queryParameters['type'] = typeId;
    }

    try {
      final response = await _client.get(
        '/api/catalog/items',
        queryParameters: queryParameters,
      );

      if (response.statusCode == 200) {
        return CatalogResult.fromJson(response.data as Map<String, dynamic>);
      }

      throw Exception('Không thể lấy danh sách sản phẩm');
    } catch (e) {
      throw Exception('Lỗi kết nối Catalog API: $e');
    }
  }

  //Lấy toàn bộ danh sách Hãng(Brands)
  Future<List<CatalogBrand>> getBrands() async {
    try {
      final response = await _client.get(
        '/api/catalog/catalogBrands',
        queryParameters: {'api-version': '2.0'},
      );
      if (response.statusCode == 200) {
        final List<dynamic> data = response.data as List<dynamic>;
        return data
            .map((json) => CatalogBrand.fromJson(json as Map<String, dynamic>))
            .toList();
      }
      throw Exception('Không thể lấy danh sách Brands');
    } catch (e) {
      throw Exception('Lỗi khi kết nối lấy brands: $e');
    }
  }

  //Lấy toàn bộ danh sách Types
  Future<List<CatalogType>> getTypes() async {
    try {
      final response = await _client.get(
        '/api/catalog/catalogTypes',
        queryParameters: {'api-version': '2.0'},
      );
      if (response.statusCode == 200) {
        final List<dynamic> data = response.data as List<dynamic>;
        return data
            .map((json) => CatalogType.fromJson(json as Map<String, dynamic>))
            .toList();
      }
      throw Exception('Không thể lấy danh sách Types');
    } catch (e) {
      throw Exception('Lỗi không kết nối lấy Types: $e');
    }
  }
}
