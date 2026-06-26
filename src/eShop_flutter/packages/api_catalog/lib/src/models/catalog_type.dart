class CatalogType {
  final int id;
  final String type;

  CatalogType({required this.id, required this.type});

  factory CatalogType.fromJson(Map<String, dynamic> json) {
    return CatalogType(id: json['id'] as int, type: json['type'] as String);
  }

  Map<String, dynamic> toJson() {
    return {'id': id, 'type': type};
  }
}
