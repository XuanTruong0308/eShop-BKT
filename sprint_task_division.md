# Phân chia Nhiệm vụ Sprint 1 & Hướng dẫn Thực hành Estimate (Thời gian: 5 ngày)

Tài liệu này hướng dẫn cách tổ chức buổi Planning (Thứ 2) và phân chia nhiệm vụ kèm theo **Độ ưu tiên (Priority)**, Story Points và **Trạng thái thực tế** dựa trên giới hạn thời gian thực tế **32 giờ thực hiện** (Thứ 3 đến Thứ 6, mỗi ngày 8 giờ).

---

## 📅 Quy mô Sprint & Sức chứa (Capacity)
Do Thứ 2 dành hoàn toàn cho Planning, cả đội chỉ có **4 ngày thực tế để code** (Thứ 3 ➔ Thứ 6).
* **Thời gian của mỗi thành viên:** 4 ngày × 8 giờ = **32 giờ**.
* **Quy ước Story Points mới:**
  * **1 Point = 4 giờ làm việc** (Nửa ngày làm việc tập trung).
  * **2 Points = 8 giờ làm việc** (1 ngày làm việc).
  * **3 Points = 12 giờ làm việc** (1.5 ngày làm việc).
  * **5 Points = 20 giờ làm việc** (2.5 ngày làm việc).
* **Tổng Capacity tối đa của mỗi người:** **8 Story Points (32 giờ)**.
KW
---

## 🚦 Quy ước Độ ưu tiên (MoSCoW Method)
Để đảm bảo dự án chạy được (MVP) khi kết thúc Sprint kể cả khi gặp sự cố trễ tiến độ, các task được phân loại ưu tiên như sau:
* **P0 - Must Have (Bắt buộc):** Chức năng xương sống, nếu thiếu thì hệ thống/dự án nâng cấp hoàn toàn không hoạt động được.
* **P1 - Should Have (Nên có):** Chức năng quan trọng để hoàn thiện luồng nghiệp vụ nhưng có thể dùng giải pháp tạm thời nếu thiếu thời gian.
* **P2 - Could Have (Có thể có):** Chức năng bổ sung, tối ưu hóa (như Caching, biểu đồ phụ), chỉ làm khi đã hoàn thành toàn bộ task P0 và P1.

---

## 🎯 Bảng phân chia Task MVP kèm Độ ưu tiên & Trạng thái

### 🤖 Dự án 1: Trợ lý Ảo AI (Trường đảm nhận)
*Chiến lược MVP: Đảm bảo luồng RAG và Cosmos DB chạy thông suốt, giao diện tối giản.*

| STT | Tên Task (Agile Story) | Mô tả chi tiết & Giới hạn Scope để đạt MVP | Độ khó | Story Points | Ước lượng | Ưu tiên | Trạng thái |
| :--- | :--- | :--- | :---: | :---: | :---: | :---: | :---: |
| 1.1 | Cấu hình AppHost (Cosmos & Ollama) | Khai báo container Cosmos DB Emulator và dịch vụ Ollama cục bộ trong `AppHost`. | Dễ | 1 | 4 giờ | **P0** | **Đã xong (Done)** |
| 1.2 | Xây dựng Chat Memory Service | Tạo DTO và `ChatMemoryService` để đọc/ghi lịch sử chat với Cosmos DB SDK. | Trung bình | 2 | 8 giờ | **P0** | **Đã xong (Done)** |
| 1.3 | Tích hợp pgvector & Vector Search | Tìm kiếm ngữ nghĩa top 3 sản phẩm phù hợp nhất trong Postgres, bỏ qua các bộ lọc phức tạp. | Khó | 3 | 12 giờ | **P0** | **Đã xong (Done)** |
| 1.4 | Đồng bộ Chat API & ChatState trên WebApp | Tạo API endpoints cho chat và gọi qua HTTP Client đơn giản, không dùng SignalR. | Dễ | 1 | 4 giờ | **P0** | **Đã xong (Done)** |
| 1.5 | Thiết kế UI Khung Chat nổi | Tạo component Razor basic hiển thị tin nhắn, bỏ qua các micro-animation phức tạp. | Dễ | 1 | 4 giờ | **P1** | **Đã xong (Done)** |
| **Tổng**| | **Chỉ số an toàn: Đạt giới hạn Capacity** | | **8 Points** | **32 giờ** | | **Hoàn thành 100%** |

---

### 🎫 Dự án 3: Loyalty & Đổi Voucher (Bảo đảm nhận)
*Chiến lược MVP: Ưu tiên luồng tích lũy điểm và áp dụng mã giảm giá. Worker hạ rank làm đơn giản nhất.*

| STT | Tên Task (Agile Story) | Mô tả chi tiết & Giới hạn Scope để đạt MVP | Độ khó | Story Points | Ước lượng | Ưu tiên | Trạng thái |
| :--- | :--- | :--- | :---: | :---: | :---: | :---: | :---: |
| 3.1 | Thiết kế Loyalty & Voucher Schema | Tạo bảng `Voucher` và thêm cột `LoyaltyPoints`, `LastPurchaseDate` vào bảng thành viên hiện tại. | Dễ | 1 | 4 giờ | **P0** | Chưa thực hiện |
| 3.2 | API đổi điểm lấy Voucher | Phát triển endpoint `/api/v1/discount/exchange` trừ điểm và sinh mã Voucher $10. | Trung bình | 2 | 8 giờ | **P0** | Chưa thực hiện |
| 3.3 | Áp dụng Voucher tại Checkout | Hiển thị ô nhập mã Voucher ở trang Checkout và trừ tiền trực tiếp trên giỏ hàng. | Trung bình | 2 | 8 giờ | **P0** | Chưa thực hiện |
| 3.4 | Background Worker hạ hạng (Decay) | Viết `HostedService` quét định kỳ đơn giản khi khởi chạy app hoặc kích hoạt bằng tay, không dùng Quartz.NET. | Trung bình | 2 | 8 giờ | **P1** | Chưa thực hiện |
| 3.5 | Đồng bộ hóa dữ liệu (Memory Cache) | Sử dụng Memory Cache để lưu hạng thành viên trước khi nâng cấp lên Redis Cache nếu dư thời gian. | Dễ | 1 | 4 giờ | **P2** | Chưa thực hiện |
| **Tổng**| | **Chỉ số an toàn: Đạt giới hạn Capacity** | | **8 Points** | **32 giờ** | | |

---

### 📊 Dự án 2: DevOps, Observability & Chaos + Payment (Khải đảm nhận)
*Chiến lược MVP: Giám sát chỉ số lỗi 5xx hệ thống trước, sau đó phát triển bơm lỗi và resilience cho Payment.*

| STT | Tên Task (Agile Story) | Mô tả chi tiết & Giới hạn Scope để đạt MVP | Độ khó | Story Points | Ước lượng | Ưu tiên | Trạng thái |
| :--- | :--- | :--- | :---: | :---: | :---: | :---: | :---: |
| 2.1 | Giám sát Prometheus & Grafana | Cấu hình Prometheus cào metrics hiệu năng hệ thống và dựng Grafana Dashboard hiển thị TPS, tỷ lệ lỗi 5xx. | Trung bình | 2 | 8 giờ | **P0** | Chưa thực hiện |
| 2.2 | API Bơm Lỗi (Chaos Controller) | Viết API `/api/v1/chaos` bật/tắt giả lập lỗi HTTP 500 ngẫu nhiên (chưa cần làm trễ Latency). | Dễ | 1 | 4 giờ | **P0** | Chưa thực hiện |
| 2.3 | **[Cải tiến]** Chaos/Outage trong Payment | Giả lập lỗi ngẫu nhiên cổng thanh toán trực tiếp trong Event Handler của `PaymentProcessor` qua cấu hình môi trường. | Trung bình | 2 | 8 giờ | **P1** | Chưa thực hiện |
| 2.4 | **[Cải tiến]** Telemetry cho Payment | Sử dụng OTel Metrics trong `PaymentProcessor` để đếm số giao dịch thành công/thất bại và hiển thị lên Grafana. | Dễ | 1 | 4 giờ | **P2** | Chưa thực hiện |
| 2.5 | Cấu hình Polly Resilience cho WebApp | Sử dụng Polly Retry cơ bản trên WebApp khi gọi API giỏ hàng/thanh toán để tự khắc phục lỗi 500. | Trung bình | 2 | 8 giờ | **P1** | Chưa thực hiện |
| **Tổng**| | **Chỉ số an toàn: Đạt giới hạn Capacity** | | **8 Points** | **32 giờ** | | |

---

## 👑 Cẩm nang dành cho Trường (Vai trò: PM & Customer) để "Ép" và Tập luyện Estimate cùng Team

Để giúp các thành viên (Bảo, Khải) rèn luyện kỹ năng **định lượng thời gian (estimate)** và **thương lượng phạm vi (scope negotiation)**, Trường hãy áp dụng quy trình 3 bước sau trong ngày Planning (Thứ 2):

### 1. Nguyên tắc "Ép" Scope (Chống phình to tính năng)
* **Quy tắc 80/20:** Nhắc nhở thành viên rằng 80% giá trị đến từ 20% code cốt lõi. Yêu cầu họ chỉ tập trung làm phần chạy được (Happy Path), cắt bỏ các trường hợp xử lý lỗi quá sâu hoặc UI cầu kỳ.
* **Định luật Parkinson:** Công việc sẽ tự phình to ra để chiếm hết thời gian được giao. Nếu bạn giao cho Bảo 5 points để làm Worker hạ hạng, Bảo sẽ dùng hết 20 giờ. Nếu bạn ép xuống 2 points, Bảo buộc phải tìm giải pháp code tối giản nhất.
* **Độ ưu tiên (Priority):** Trường phải làm rõ: **Làm hết các task P0 trước**, rồi đến P1. Nếu không kịp thời gian, các task P2 sẽ tự động bị dời sang Sprint sau (hoặc loại bỏ khỏi MVP).

### 2. Các câu hỏi chất vấn khi Planning (Mẫu câu cho PM)
Khi Bảo hoặc Khải đưa ra estimate cho một Task, Trường hãy "ép ngược" bằng các câu hỏi sau:
* *“Tại sao việc cập nhật CSDL lại mất tận 8 tiếng (2 points)? Anh thấy cấu trúc bảng đã có sẵn, chỉ cần thêm 2 cột và chạy migration. Liệu 4 tiếng (1 point) có xong không? Em đang vướng ở bước nào?”*
* *“Khải ước lượng việc cấu hình Polly mất 5 points (20 tiếng) là quá nhiều. Chúng ta chỉ cần áp dụng cơ chế Retry đơn giản của Polly lên HttpClient trong WebApp. Có thể cắt bớt Circuit Breaker và hạ xuống 2 points (8 tiếng) được không?”*
* *“Dự án này là thử nghiệm nội bộ, anh không cần hệ thống bảo mật hay mã hóa cho Voucher ở Sprint này. Bảo hãy cắt bớt phần xác thực token phức tạp để đưa API đổi điểm về đúng 2 points.”*

### 3. Kỹ thuật Planning Poker (Thực hành ước lượng)
1. Trường đọc tên Task (Ví dụ: *Task 3.2 - API đổi điểm lấy Voucher*).
2. Bảo và Khải tự đưa ra số điểm dự kiến (ví dụ: Bảo nghĩ là 3 points, Khải nghĩ là 1 point).
3. **Phân tích độ lệch:** Bảo giải thích tại sao cần 3 points (vướng logic trừ điểm, ghi log giao dịch). Khải giải thích tại sao nghĩ là 1 point (chỉ cần viết 1 controller đơn giản).
4. Trường đóng vai trò khách hàng/PM phân xử, định hướng cắt giảm các yêu cầu không cần thiết để cả đội chốt số điểm tối ưu (Ví dụ: Chốt 2 points).
