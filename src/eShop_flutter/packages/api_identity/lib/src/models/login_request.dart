class LoginRequest {
  final String userName;
  final String password;

  LoginRequest({required this.userName, required this.password});

  //hàm chuyển object dart thành json gửi qua network
  Map<String, dynamic> toJson() {
    return {'username': userName, 'password': password};
  }
}
