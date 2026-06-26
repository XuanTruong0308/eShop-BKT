class AppConfig {
  // Đổi sang 'http://localhost:5222' để chạy với backend cục bộ (Local)
  // Đổi sang 'http://103.75.184.233:5222' để kết nối tới server Staging
  static const String apiBaseUrl = 'http://103.75.184.233:5222';

  // URL đăng nhập trỏ thẳng tới Identity Server (bỏ qua YARP)
  // để token được phát với issuer khớp với cấu hình của Ordering API.
  static const String identityBaseUrl = 'http://103.75.184.233:10002';

  // Giữ lại để tương thích ngược nếu cần
  static const String identityUrl = '$identityBaseUrl';
}
