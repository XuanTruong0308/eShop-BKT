import 'order_item.dart';

class Order {
  final int orderNumber;
  final DateTime date;
  final String status;
  final String description;
  final String street;
  final String city;
  final String state;
  final String zipcode;
  final String country;
  final List<OrderItem> orderItems;
  final double total;

  Order({
    required this.orderNumber,
    required this.date,
    required this.status,
    required this.description,
    required this.street,
    required this.city,
    required this.state,
    required this.zipcode,
    required this.country,
    required this.orderItems,
    required this.total,
  });

  factory Order.fromJson(Map<String, dynamic> json) {
    return Order(
      orderNumber: json['orderNumber'] as int? ?? 0,
      date: json['date'] != null ? DateTime.parse(json['date'] as String) : DateTime.now(),
      status: json['status'] as String? ?? '',
      description: json['description'] as String? ?? '',
      street: json['street'] as String? ?? '',
      city: json['city'] as String? ?? '',
      state: json['state'] as String? ?? '',
      zipcode: json['zipcode'] as String? ?? '',
      country: json['country'] as String? ?? '',
      orderItems: (json['orderItems'] as List<dynamic>?)
              ?.map((item) => OrderItem.fromJson(item as Map<String, dynamic>))
              .toList() ??
          [],
      total: (json['total'] as num? ?? 0.0).toDouble(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'orderNumber': orderNumber,
      'date': date.toIso8601String(),
      'status': status,
      'description': description,
      'street': street,
      'city': city,
      'state': state,
      'zipcode': zipcode,
      'country': country,
      'orderItems': orderItems.map((item) => item.toJson()).toList(),
      'total': total,
    };
  }
}
