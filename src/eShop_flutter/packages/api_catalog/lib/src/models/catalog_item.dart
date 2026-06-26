import 'catalog_brand.dart';
import 'catalog_type.dart';

class CatalogItem {
  final int id;
  final String name;
  final String? description;
  final double price;
  final String? pictureFileName;
  final int catalogTypeId;
  final CatalogType? catalogType;
  final int catalogBrandId;
  final CatalogBrand? catalogBrand;

  CatalogItem({
    required this.id,
    required this.name,
    this.description,
    required this.price,
    this.pictureFileName,
    required this.catalogTypeId,
    this.catalogType,
    required this.catalogBrandId,
    this.catalogBrand,
  });

  factory CatalogItem.fromJson(Map<String, dynamic> json) {
    return CatalogItem(
      id: json['id'] as int,
      name: json['name'] as String,
      description: json['description'] as String?,
      price: (json['price'] as num)
          .toDouble(), // Ép kiểu từ num sang double an toàn
      pictureFileName: json['pictureFileName'] as String?,
      catalogTypeId: json['catalogTypeId'] as int,
      catalogType: json['catalogType'] != null
          ? CatalogType.fromJson(json['catalogType'] as Map<String, dynamic>)
          : null,
      catalogBrandId: json['catalogBrandId'] as int,
      catalogBrand: json['catalogBrand'] != null
          ? CatalogBrand.fromJson(json['catalogBrand'] as Map<String, dynamic>)
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'description': description,
      'price': price,
      'pictureFileName': pictureFileName,
      'catalogTypeId': catalogTypeId,
      'CatalogType': catalogType?.toJson(),
      'catalogBrandId': catalogBrandId,
      'catalogBrand': catalogBrand?.toJson(),
    };
  }
}
