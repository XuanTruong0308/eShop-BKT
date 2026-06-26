class BasketItemRequest {
  final String id;
  final int productId;
  final String productName;
  final double unitPrice;
  final double oldUnitPrice;
  final int quantity;
  final String pictureUrl;

  BasketItemRequest({
    required this.id,
    required this.productId,
    required this.productName,
    required this.unitPrice,
    required this.oldUnitPrice,
    required this.quantity,
    required this.pictureUrl,
  });

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'productId': productId,
      'productName': productName,
      'unitPrice': unitPrice,
      'oldUnitPrice': oldUnitPrice,
      'quantity': quantity,
      'pictureUrl': pictureUrl,
    };
  }
}

class CreateOrderRequest {
  final String userId;
  final String userName;
  final String city;
  final String street;
  final String state;
  final String country;
  final String zipCode;
  final String cardNumber;
  final String cardHolderName;
  final DateTime cardExpiration;
  final String cardSecurityNumber;
  final int cardTypeId;
  final String buyer;
  final List<BasketItemRequest> items;

  CreateOrderRequest({
    required this.userId,
    required this.userName,
    required this.city,
    required this.street,
    required this.state,
    required this.country,
    required this.zipCode,
    required this.cardNumber,
    required this.cardHolderName,
    required this.cardExpiration,
    required this.cardSecurityNumber,
    required this.cardTypeId,
    required this.buyer,
    required this.items,
  });

  Map<String, dynamic> toJson() {
    return {
      'userId': userId,
      'userName': userName,
      'city': city,
      'street': street,
      'state': state,
      'country': country,
      'zipCode': zipCode,
      'cardNumber': cardNumber,
      'cardHolderName': cardHolderName,
      'cardExpiration': cardExpiration.toUtc().toIso8601String(),
      'cardSecurityNumber': cardSecurityNumber,
      'cardTypeId': cardTypeId,
      'buyer': buyer,
      'items': items.map((item) => item.toJson()).toList(),
    };
  }
}
