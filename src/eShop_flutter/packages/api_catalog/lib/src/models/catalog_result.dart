import 'catalog_item.dart';

class CatalogResult {
  final int pageIndex;
  final int pageSize;
  final int count;
  final List<CatalogItem> data;

  CatalogResult({
    required this.pageIndex,
    required this.pageSize,
    required this.count,
    required this.data,
  });

  factory CatalogResult.fromJson(Map<String, dynamic> json) {
    return CatalogResult(
      pageIndex: json['pageIndex'] as int,
      pageSize: json['pageSize'] as int,
      count: json['count'] as int,
      data:
          (json['data'] as List<dynamic>?)
              ?.map(
                (item) => CatalogItem.fromJson(item as Map<String, dynamic>),
              )
              .toList() ??
          [],
    );
  }
}
