class OrderItem {
  final String productName;
  final int units;
  final double unitPrice;
  final String pictureUrl;

  OrderItem({
    required this.productName,
    required this.units,
    required this.unitPrice,
    required this.pictureUrl,
  });

  factory OrderItem.fromJson(Map<String, dynamic> json) {
    return OrderItem(
      productName: json['productName'] as String? ?? '',
      units: json['units'] as int? ?? 0,
      unitPrice: (json['unitPrice'] as num? ?? 0.0).toDouble(),
      pictureUrl: json['pictureUrl'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'productName': productName,
      'units': units,
      'unitPrice': unitPrice,
      'pictureUrl': pictureUrl,
    };
  }
}
