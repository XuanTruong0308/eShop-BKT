import 'basket_item.dart';

class CustomerBasket {
  final String buyerId;
  final List<BasketItem> items;

  CustomerBasket({
    required this.buyerId,
    required this.items,
  });

  factory CustomerBasket.fromJson(Map<String, dynamic> json) {
    return CustomerBasket(
      buyerId: json['buyerId'] as String,
      items: (json['items'] as List<dynamic>?)
              ?.map((item) => BasketItem.fromJson(item as Map<String, dynamic>))
              .toList() ??
          [],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'buyerId': buyerId,
      'items': items.map((item) => item.toJson()).toList(),
    };
  }
}
