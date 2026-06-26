class BasketItem {
  final String? id;
  final int productId;
  final String? productName;
  final double? unitPrice;
  final double? oldUnitPrice;
  final int quantity;
  final String? pictureUrl;

  BasketItem({
    this.id,
    required this.productId,
    this.productName,
    this.unitPrice,
    this.oldUnitPrice,
    required this.quantity,
    this.pictureUrl,
  });

  factory BasketItem.fromJson(Map<String, dynamic> json) {
    return BasketItem(
      id: json['id'] as String?,
      productId: json['productId'] as int,
      productName: json['productName'] as String?,
      unitPrice: (json['unitPrice'] as num?)?.toDouble(),
      oldUnitPrice: (json['oldUnitPrice'] as num?)?.toDouble(),
      quantity: json['quantity'] as int,
      pictureUrl: json['pictureUrl'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      if (id != null) 'id': id,
      'productId': productId,
      if (productName != null) 'productName': productName,
      if (unitPrice != null) 'unitPrice': unitPrice,
      if (oldUnitPrice != null) 'oldUnitPrice': oldUnitPrice,
      'quantity': quantity,
      if (pictureUrl != null) 'pictureUrl': pictureUrl,
    };
  }
}
