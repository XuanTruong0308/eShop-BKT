class OrderSummary {
  final int orderNumber;
  final DateTime date;
  final String status;
  final double total;

  OrderSummary({
    required this.orderNumber,
    required this.date,
    required this.status,
    required this.total,
  });

  factory OrderSummary.fromJson(Map<String, dynamic> json) {
    return OrderSummary(
      orderNumber: json['orderNumber'] as int? ?? 0,
      date: json['date'] != null ? DateTime.parse(json['date'] as String) : DateTime.now(),
      status: json['status'] as String? ?? '',
      total: (json['total'] as num? ?? 0.0).toDouble(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'orderNumber': orderNumber,
      'date': date.toIso8601String(),
      'status': status,
      'total': total,
    };
  }
}
