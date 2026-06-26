class CatalogBrand {
  final int id;
  final String brand;

  CatalogBrand({required this.id, required this.brand});
  factory CatalogBrand.fromJson(Map<String, dynamic> json) {
    return CatalogBrand(id: json['id'] as int, brand: json['brand'] as String);
  }

  Map<String, dynamic> toJson() {
    return {'id': id, 'brand': brand};
  }
}
