class AppConfig {
  // Đổi sang 'http://localhost:5222' để chạy với backend cục bộ (Local)
  // Đổi sang 'http://103.75.184.233:5222' để kết nối tới server Staging
  static const String apiBaseUrl = 'http://103.75.184.233:5222';

  static const String identityUrl = '$apiBaseUrl/identity';
}
