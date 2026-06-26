class CardType {
  final int id;
  final String name;

  CardType({
    required this.id,
    required this.name,
  });

  factory CardType.fromJson(Map<String, dynamic> json) {
    return CardType(
      id: json['id'] as int? ?? 0,
      name: json['name'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
    };
  }
}
