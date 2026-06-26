# Cẩm nang Kiến thức Dart & Flutter: Lập trình Bất đồng bộ & Mạng

Khi chuyển từ C# (hoặc các ngôn ngữ khác) sang Dart, có một số cú pháp trông sẽ khá "lạ mắt". Tài liệu này tổng hợp lại toàn bộ các khái niệm cốt lõi xuất hiện trong code `NetworkClient` và `AuthInterceptor` mà bạn vừa gõ. Tài liệu này sẽ **liên tục được cập nhật** mỗi khi chúng ta gặp kiến thức mới!

---

## 1. Lập trình Bất đồng bộ (Asynchronous Programming)

Việc gọi API (mạng lưới) luôn mất thời gian (vài chục milliseconds đến vài giây). Nếu chờ đợi theo kiểu thông thường, ứng dụng sẽ bị "đơ" (treo UI). Dart giải quyết việc này bằng `Future`, `async` và `await`.

### `Future` là gì?
- `Future` trong Dart tương đương với `Task` trong C# hoặc `Promise` trong JavaScript.
- Nó đại diện cho một "lời hứa" rằng: *"Bây giờ tôi chưa có kết quả, nhưng trong tương lai tôi sẽ trả về cho bạn"*.
- **Ví dụ:** `Future<Response>` nghĩa là hàm này sẽ trả về một đối tượng `Response` (kết quả từ server) ở một thời điểm nào đó trong tương lai.

### `async` và `await`
- **`async`**: Đặt ở ngay trước dấu `{` mở thân hàm. Nó báo hiệu cho trình biên dịch biết: *"Hàm này chứa các tác vụ tốn thời gian, hãy cho phép nó chạy ngầm (bất đồng bộ)"*.
- **`await`**: Chỉ được dùng bên trong một hàm có chữ `async`. Nó ra lệnh: *"Hãy tạm dừng tại dòng code này, chờ đến khi có kết quả mạng trả về rồi mới chạy tiếp dòng bên dưới"*.

**Cú pháp:**
```dart
// Chú ý chữ async nằm ở ngay trước dấu {
Future<Response> get(String path) async {
  // Chữ await yêu cầu chờ thư viện dio tải xong dữ liệu
  return await dio.get(path); 
}
```

---

## 2. Tham số Bắt buộc và Tham số Tên (Named Parameters)

Dart có một hệ thống tham số rất mạnh và rõ ràng, giúp code dễ đọc hơn.

### Cặp ngoặc nhọn `{ ... }` trong khai báo hàm
Khi bạn khai báo hàm: `Future<Response> post(String path, {dynamic data})`
- Tham số `path` là **tham số vị trí bắt buộc**: Bắt buộc phải truyền vào đầu tiên.
- Các tham số nằm trong cặp `{ }` gọi là **tham số được đặt tên (Named Parameters)**: Nó là tùy chọn (có thể truyền hoặc không), và khi truyền thì phải gọi đích danh tên của nó.

### Cú pháp `data: data` khi gọi hàm
Vì `data` nằm trong `{ }`, nên khi gọi hàm `dio.post`, bạn không thể truyền khơi khơi giá trị, mà phải viết rõ `tên_tham_số: giá_trị`.
- Từ `data` (trước dấu hai chấm) là **tên của tham số** do thư viện Dio quy định.
- Từ `data` (sau dấu hai chấm) là **biến** chứa dữ liệu bạn muốn truyền vào.

**Ví dụ thực tế:**
```dart
// Định nghĩa hàm
void muaHang(String tenMon, {int so পাল্টা = 1, String ghiChu = ''}) { ... }

// Khi gọi hàm, phải gọi đúng tên tham số
muaHang('Trà Sữa', soLuong: 2, ghiChu: 'Ít đá');
```

---

## 3. Các Từ khóa Quan trọng Khác

### `dynamic`
- Là kiểu dữ liệu đặc biệt báo cho Dart biết: *"Biến này có thể chứa bất kỳ kiểu dữ liệu gì (chuỗi, số, danh sách, JSON...)"*.
- Thường dùng trong gọi API vì dữ liệu đẩy lên (body) có lúc là một chuỗi văn bản, có lúc là một file, có lúc là một danh sách.

### `Map<String, dynamic>`
- Đây là cấu trúc dữ liệu **Dictionary** (trong C#) hoặc **Object** (trong JS).
- Rất phổ biến khi làm việc với JSON. Nghĩa là: Các cái chìa khóa (Key) luôn là chữ (`String`), nhưng giá trị (Value) thì có thể là bất kỳ thứ gì (`dynamic`).
- **Ví dụ:** `{'userName': 'admin', 'age': 25, 'isActive': true}`.

### `required`
- Đi kèm với Named Parameters `{ }`. Bình thường tham số trong `{ }` là tùy chọn (có thể `null`).
- Nhét thêm chữ `required` vào để bắt ép lập trình viên: *"Tham số này có tên rõ ràng, nhưng bắt buộc phải truyền, không được quên"*.
- **Ví dụ:** `NetworkClient({required String baseUrl})` -> Lúc gọi bắt buộc phải viết: `NetworkClient(baseUrl: 'http...')`.

### `late final`
- **`final`**: Biến này chỉ được gán giá trị **1 lần duy nhất** (tương tự `readonly` trong C#).
- **`late`**: Biến này chưa có giá trị ngay lúc khai báo, tôi "hứa" sẽ gán giá trị cho nó muộn hơn (nhưng trước khi tôi sử dụng nó).
- Cặp bài trùng `late final` rất hay dùng cho những công cụ cốt lõi như `Dio`, chỉ khởi tạo một lần khi khởi động app và dùng mãi mãi.

### Callback Functions (Hàm ẩn danh)
Trong file `AuthInterceptor`, bạn thấy khai báo: `final Future<String?> Function() getToken;`
- Đây là một **Biến chứa một Hàm**.
- Thay vì `AuthInterceptor` phải tự đi tìm mật khẩu (Token), nó yêu cầu người tạo ra nó phải đưa cho nó một "công thức" (hàm) để nó tự gọi khi cần. Điều này giúp tách biệt rõ ràng: Trạm kiểm soát không cần biết thẻ Token cất ở két sắt nào, nó chỉ cần hô "Lấy thẻ cho tôi" thông qua hàm callback.

---

## 4. Kế thừa và Nạp chồng (Lập trình Hướng đối tượng)

Trong file `auth_interceptor.dart`, bạn sẽ thấy các khái niệm kinh điển của OOP (Object-Oriented Programming).

### `extends` (Kế thừa)
- `class AuthInterceptor extends Interceptor` có nghĩa là: *"AuthInterceptor của tôi là một phiên bản con của lớp Interceptor do thư viện Dio viết sẵn"*. Nó được hưởng mọi khả năng của lớp cha.

### `@override` (Ghi đè / Nạp chồng)
- Khi lớp cha `Interceptor` đã có sẵn một hàm tên là `onRequest` (hàm này tự động chạy khi có request gửi đi), nhưng chúng ta muốn **thay đổi cách nó hoạt động** (để nhét Token vào).
- Chúng ta viết lại hàm đó và đặt `@override` lên trên đầu để báo cho trình biên dịch biết: *"Tôi đang ghi đè lại hàm của cha tôi"*.

### `super` (Gọi lại cha)
- Ở cuối hàm `onRequest`, bạn thấy dòng `super.onRequest(options, handler);`.
- Sau khi chúng ta đã làm xong việc riêng (nhét Token), từ khóa `super` ra lệnh: *"Trả lại quyền điều khiển cho lớp cha, hãy tiếp tục thực thi các luồng xử lý mạng mặc định của Dio đi"*. Nếu quên dòng này, request mạng sẽ bị "đứng hình" vĩnh viễn ở trạm kiểm soát!

---

## 5. Flutter Widget: Xây Giao Diện (UI)

### `Widget` là gì?
- Mọi thứ hiển thị trên màn hình Flutter đều là **Widget**: nút bấm, ô chữ, hình ảnh, khoảng trống... thậm chí cả màn hình tổng thể cũng là Widget.
- Widget lồng vào nhau như búp bê Matryoshka để tạo ra giao diện phức tạp.

### `StatelessWidget` vs `StatefulWidget`
- **`StatelessWidget`**: Widget "bất động" - một khi hiện ra thì không tự thay đổi được. Dùng cho các thành phần tĩnh như tiêu đề, icon, logo.
- **`StatefulWidget`**: Widget "có trạng thái" - có thể tự cập nhật giao diện khi dữ liệu thay đổi (ví dụ: ô text thay đổi khi gõ phím, nút bấm chuyển từ "Đăng nhập" sang vòng tròn loading...).

### `TextEditingController`
- Là một "đầu đọc" gắn vào ô nhập liệu (`TextField`). Khi người dùng gõ chữ vào ô, bạn dùng `controller.text` để đọc ra nội dung họ đã gõ.
- Phải nhớ gọi `controller.dispose()` trong hàm `dispose()` để giải phóng bộ nhớ khi màn hình bị đóng.

### Cú pháp `Widget build(BuildContext context)` 
- Đây là hàm bắt buộc của mọi Widget. Flutter sẽ gọi hàm này mỗi khi cần vẽ lại Widget lên màn hình. Bên trong hàm này, bạn `return` về cái bố cục (layout) bạn muốn hiển thị.

### `BuildContext`
- Là "địa chỉ" của Widget trong cây Widget. Khi bạn muốn hiển thị hộp thoại (`showDialog`), điều hướng sang màn hình khác (`Navigator`), hay đọc dữ liệu từ Riverpod, bạn đều cần truyền `context` vào.

---

## 6. Null Safety (An toàn Null)

Dart có hệ thống **Null Safety** rất chặt chẽ. Trình biên dịch sẽ bắt lỗi nếu bạn dùng một biến có thể `null` mà không kiểm tra trước.

### `String` vs `String?`
- **`String`** (không có dấu `?`): Biến này **bắt buộc phải có giá trị**, không bao giờ được phép là `null`.
- **`String?`** (có dấu `?`): Biến này **có thể là `null`**. Dart sẽ bắt bạn phải kiểm tra `null` trước khi dùng.

```dart
String  ten = 'Minh';   // ✅ OK
String  ten = null;     // ❌ Lỗi biên dịch!
String? ten = null;     // ✅ OK, cho phép null
String? ten = 'Minh';   // ✅ OK, cũng cho phép có giá trị
```

### Tại sao `getToken` trả về `Future<String?>` (nullable)?
Đây là thiết kế **có chủ ý** để xử lý 2 tình huống với cùng 1 hàm:
- **Màn hình Login**: Người dùng chưa đăng nhập → chưa có token → trả về `null` → Interceptor bỏ qua, không nhét header.
- **Màn hình Home**: Người dùng đã đăng nhập → có token → trả về `"eyJhbGc..."` → Interceptor nhét `Bearer token` vào header.

### Kiểm tra Null: `if (token != null)`
Khi nhận một biến nullable, bạn phải kiểm tra trước khi dùng:
```dart
final token = await getToken(); // token có thể là null

if (token != null) {
  // Bên trong khối này, Dart tự hiểu token chắc chắn có giá trị (non-null)
  options.headers['Authorization'] = 'Bearer $token';
}
```

### Toán tử `??` (Null Coalescing)
Dùng để cung cấp giá trị mặc định khi biến là `null`:
```dart
final token = await getToken();
final header = token ?? 'anonymous'; // Nếu token null thì dùng 'anonymous'
```

### Toán tử `?.` (Null-safe access)
Dùng để gọi method/property mà không sợ crash khi biến là `null`:
```dart
String? ten = null;
print(ten.length);  // ❌ Crash! (Null check operator)
print(ten?.length); // ✅ An toàn, trả về null thay vì crash
```

---

## 7. CORS (Cross-Origin Resource Sharing)

### CORS là gì?
CORS là cơ chế bảo mật của trình duyệt (browser). Nó ngăn cản một trang web ở địa chỉ này gọi API sang địa chỉ khác mà không được phép.

**Ví dụ lỗi bạn đang gặp:**
- Flutter Web chạy tại: `http://localhost:57926` (origin A)
- Gọi API tại: `http://localhost:5223` (origin B)
- Browser hỏi Backend: *"Bạn có cho phép origin A gọi không?"*
- Backend không trả lời → Browser chặn lại → **CORS Error!**

### Tại sao chỉ Flutter Web bị, còn Android/iOS thì không?
- **Android/iOS app**: Gọi API trực tiếp từ thiết bị, **không qua browser** → không bị CORS.
- **Flutter Web** (chạy trên Chrome): Bị coi là "trang web" → browser áp dụng CORS policy.

### 2 Cách xử lý

**Cách 1 (Nhanh - Dev only):** Chạy Flutter dưới dạng **Windows App** thay vì Chrome. Windows app không phải browser nên không bị CORS:
```bash
flutter run -d windows
```

**Cách 2 (Đúng chuẩn - Production):** Cấu hình Backend C# cho phép origin của Flutter Web trong CORS policy.

---

## 8. Cách cấu hình CORS trên ASP.NET Core & IdentityServer (Backend C#)

Để giải quyết vấn đề CORS cho tất cả các microservices và luồng đăng nhập của IdentityServer trong môi trường phát triển (Development), chúng ta làm hai việc:

### A. Đăng ký CORS toàn cục trong ServiceDefaults
Mọi microservice trong dự án .NET Aspire đều gọi `builder.AddServiceDefaults()`. Do đó, cấu hình CORS ở đây sẽ áp dụng cho toàn bộ các API (Catalog, Basket, Ordering...):
```csharp
// Trong Extensions.cs của eShop.ServiceDefaults
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```
Và chèn Middleware vào đầu đường ống xử lý:
```csharp
app.UseCors("CorsPolicy");
```

### B. Cấu hình Bypass CORS cho IdentityServer
IdentityServer có bộ lọc CORS riêng (`ICorsPolicyService`). Nếu chỉ cấu hình CORS ở trên, các endpoint của IdentityServer (như `/connect/token`) vẫn có thể bị chặn. Chúng ta đăng ký dịch vụ `DefaultCorsPolicyService` cho phép tất cả các nguồn truy cập trong Development:
```csharp
// Trong Program.cs của Identity.API
builder.Services.AddSingleton<ICorsPolicyService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<DefaultCorsPolicyService>>();
    return new DefaultCorsPolicyService(logger)
    {
        AllowAll = true
    };
});
```

---

## 9. OAuth 2.0 / OpenID Connect: Grant Types (Luồng xác thực)

Khi gửi yêu cầu lấy token tại `/connect/token`, tham số quan trọng nhất quyết định cách bạn đăng nhập là **`grant_type`**.

### Grant Type là gì?
Là "phương thức" hoặc "luồng" mà Client dùng để lấy Access Token từ Identity Server. Mỗi Client chỉ được phép sử dụng các luồng đã đăng ký trước để đảm bảo an ninh.

### Các luồng phổ biến:
1. **Authorization Code Flow** (`grant_type: 'code'`):
   - Thường dùng cho Web App truyền thống hoặc Mobile App chuẩn.
   - Luồng chạy: App mở trình duyệt -> Người dùng đăng nhập trên web của Server -> Server trả về `code` -> App dùng `code` đổi lấy `token` ở kênh bảo mật phía sau (Backchannel).
2. **Resource Owner Password Credentials** (`grant_type: 'password'`):
   - Dùng khi ứng dụng Flutter tự thiết kế màn hình đăng nhập (như chúng ta vừa làm) và gửi trực tiếp `username` + `password` lên API lấy token.
   - Luồng này cực kỳ nhanh nhưng chỉ dùng cho các ứng dụng nội bộ/tin tưởng tuyệt đối.

### Lỗi 400 Bad Request gặp phải:
Ứng dụng Flutter gửi yêu cầu dạng `password` (nhập username/password trực tiếp), sử dụng `client_id: 'maui'`. Tuy nhiên, trong C# Backend, client `maui` ban đầu cấu hình chỉ cho phép luồng `code` (`AllowedGrantTypes = GrantTypes.Code`). Khi đó Identity Server sẽ từ chối và trả về **400 Bad Request**.

### Cách khắc phục:
Cấu hình cho phép client `maui` sử dụng cả hai luồng xác thực trong `Config.cs` ở Backend:
```csharp
AllowedGrantTypes = new[] { GrantType.AuthorizationCode, GrantType.ResourceOwnerPassword },

---

## 10. Xử lý dữ liệu JSON trong Dart (JSON Serialization)

Khác với C# (dùng Reflection như System.Text.Json để tự động map dữ liệu), Dart tắt tính năng Reflection trong Flutter để tối ưu hóa dung lượng ứng dụng (Tree Shaking). Do đó, chúng ta phải viết tay các hàm `fromJson` và `toJson`.

Dưới đây là các kiến thức "lạ" và dễ gây bug nhất khi parse JSON trong Dart:

### A. Ép kiểu số thực: `(json['price'] as num).toDouble()`
Trong C#, một trường `decimal` hay `double` nhận giá trị là `19` hay `19.5` đều tự động hiểu. Nhưng trong Dart:
* Nếu bạn viết `json['price'] as double` mà server trả về số nguyên `19`, ứng dụng sẽ **Crash ngay lập tức** với lỗi: `TypeError: int is not a subtype of type double`.
* **Giải pháp:** Ép kiểu nó về `num` trước (kiểu cha của cả `int` và `double` trong Dart), sau đó gọi hàm `.toDouble()`.

```dart
// ❌ Dễ crash nếu server trả về số tròn (ví dụ: 10 thay vì 10.0)
price: json['price'] as double 

// ✅ An toàn tuyệt đối
price: (json['price'] as num).toDouble() 
```

### B. Parse danh sách đối tượng: `List<dynamic>` sang `List<T>`
Khi nhận về một mảng JSON (ví dụ: danh sách sản phẩm `data`), Dart hiểu nó là một `List<dynamic>`. Chúng ta phải duyệt mảng đó để chuyển từng phần tử sang kiểu dữ liệu mong muốn.

```dart
data: (json['data'] as List<dynamic>?)
        ?.map((item) => CatalogItem.fromJson(item as Map<String, dynamic>))
        .toList() ?? []
```
* **Giải thích:**
  1. `as List<dynamic>?`: Ép kiểu an toàn (đề phòng trường hợp trường `data` bị null).
  2. `?.map(...)`: Chỉ chạy hàm map nếu list không null. Chuyển từng `item` (bản chất là một `Map<String, dynamic>`) qua hàm `fromJson` của Model.
  3. `.toList()`: Chuyển Iterable kết quả về dạng List.
  4. `?? []`: Nếu toàn bộ cụm trên bị null (do trường `data` từ server null), ta trả về mảng rỗng `[]` để tránh lỗi Null Pointer.

### C. Parse đối tượng con lồng nhau (Nested Objects)
Khi một sản phẩm chứa thông tin của Hãng (`CatalogBrand`), chúng ta phải kiểm tra xem hãng đó có bị null hay không trước khi gọi hàm map của nó:

```dart
catalogBrand: json['catalogBrand'] != null
    ? CatalogBrand.fromJson(json['catalogBrand'] as Map<String, dynamic>)
    : null
```
* Nếu trường `catalogBrand` trong JSON không null, ta gọi tiếp `CatalogBrand.fromJson`. Ngược lại thì gán bằng `null`.

---

## 11. Cấu hình Web Renderer & Chạy Offline trong Flutter Web (Từ Flutter 3.29+)

Từ phiên bản **Flutter 3.29** trở đi (hiện tại máy của bạn đang dùng **Flutter 3.44.2**), Flutter đã **loại bỏ hoàn toàn HTML Renderer** và tùy chọn `--web-renderer`. Bộ dựng hình web hiện tại được mặc định là CanvasKit (hoặc SkWasm đối với WebAssembly).

Do đó, bạn không thể sử dụng `--web-renderer html` để chạy offline hoặc né lỗi tải CanvasKit từ CDN nữa.

### A. Nguyên nhân lỗi crash CanvasKit khi chạy Web
Mặc định, Flutter Web sẽ cố gắng tải các file tài nguyên tĩnh (`canvaskit.js`, `canvaskit.wasm`, phông chữ Roboto...) từ máy chủ CDN của Google (`gstatic.com` và `fonts.gstatic.com`). Nếu môi trường phát triển của bạn bị giới hạn mạng, mất kết nối internet, hoặc mạng chập chờn, trình duyệt sẽ không tải được các file này và dẫn đến lỗi `TypeError: Failed to fetch` làm crash app ngay lập tức.

### B. Giải pháp: Sử dụng flag `--no-web-resources-cdn`
Để yêu cầu Flutter Web phục vụ các file CanvasKit và font trực tiếp từ mã nguồn SDK local (offline) mà không gọi lên CDN của Google, bạn cần sử dụng cờ:
```bash
flutter run -d chrome --no-web-resources-cdn
```
* **Cách hoạt động:** Khi truyền cờ `--no-web-resources-cdn`, dev server của Flutter sẽ tự động tìm các file CanvasKit trong thư mục cache của SDK Flutter trên máy của bạn và gửi trực tiếp cho trình duyệt Chrome. Bạn có thể chạy và phát triển ứng dụng web hoàn toàn offline.

---

## 12. Hiển thị danh sách dạng lưới (Grid) & Tải ảnh từ API trong Flutter

### A. Hiển thị danh sách dạng lưới với `GridView.builder`
Khi hiển thị danh sách sản phẩm theo dạng nhiều cột, chúng ta sử dụng `GridView.builder` thay vì bọc các hàng (`Row`) trong `SingleChildScrollView`.
* **Lý do hiệu năng:** Tương tự như `ListView.builder`, `GridView.builder` chỉ vẽ (render) các ô lưới đang thực sự hiển thị trên màn hình. Khi người dùng cuộn, các widget nằm ngoài tầm nhìn sẽ bị giải phóng và tái sử dụng, giúp giảm đáng kể bộ nhớ tiêu thụ.
* **Cấu hình quan trọng:**
  * `gridDelegate`: Xác định bố cục của lưới. Chúng ta dùng `SliverGridDelegateWithFixedCrossAxisCount` để chỉ định số cột cố định (ví dụ: `crossAxisCount: 2`).
  * `childAspectRatio`: Tỷ lệ `chiều rộng / chiều cao` của mỗi ô sản phẩm. Ví dụ `0.7` nghĩa là chiều cao lớn hơn chiều rộng, chừa khoảng trống cho hình ảnh ở trên, tên và giá sản phẩm ở dưới.

### B. Tải ảnh động từ API
Trong eShop, hình ảnh của sản phẩm được phục vụ từ API: `GET http://localhost:5222/api/catalog/items/{id}/pic`.
* Để hiển thị ảnh này, ta sử dụng `Image.network(imageUrl)`.
* Vì Backend đã được cấu hình CORS cho phép mọi nguồn ở các bước trước, Flutter Web chạy trên trình duyệt sẽ không gặp trở ngại nào khi tải ảnh từ Catalog API ở cổng `5222`.

---

## 13. API Versioning & Ảnh Hưởng của Cấu Hình Header Content-Type Lên API (Lỗi 400 Bad Request)

Khi kết nối với các microservices trong hệ thống thực tế lớn, có 2 nguyên nhân cực kỳ phổ biến gây ra lỗi **400 Bad Request** trên các API công khai:

### A. Thiếu Phiên Bản API (API Versioning)
Ở Backend ASP.NET Core, dịch vụ `Catalog.API` được thiết kế có phân chia phiên bản API (`AddApiVersioning`) và không bật chế độ tự động nhận diện phiên bản mặc định khi thiếu (`AssumeDefaultVersionWhenUnspecified = true`).
* **Hậu quả:** Mọi request gửi lên endpoint của Catalog (như `/api/catalog/catalogBrands`, `/api/catalog/catalogTypes` hay `/api/catalog/items`) bắt buộc phải truyền tham số phiên bản. Nếu không truyền, middleware lọc phiên bản của Backend sẽ lập tức từ chối và trả về lỗi **400 Bad Request**.
* **Giải pháp:** Cần bổ sung thêm tham số `'api-version': '2.0'` vào phần `queryParameters` của tất cả các lệnh gọi API Catalog bên phía Flutter:
  ```dart
  final response = await _client.get(
    '/api/catalog/catalogBrands',
    queryParameters: {'api-version': '2.0'},
  );
  ```

### B. Thiết lập header `Content-Type` toàn cục không hợp lệ
Trước đây, trong `NetworkClient` (Dio), chúng ta cấu hình mặc định trong `BaseOptions.headers`:
`'Content-Type': 'application/x-www-form-urlencoded'`
* **Hậu quả:** Điều này vô tình ép buộc **tất cả** các request (bao gồm cả các yêu cầu **GET** tải dữ liệu của Catalog) phải mang header này đi. Trong ASP.NET Core Minimal API, một yêu cầu GET mà có header `Content-Type` dạng Form-urlencoded sẽ bị coi là bất hợp lý (hoặc gây lỗi cho cơ chế binding dữ liệu) dẫn đến lỗi **400 Bad Request**.
* **Giải pháp:**
  1. Loại bỏ `'Content-Type': 'application/x-www-form-urlencoded'` khỏi cấu hình mặc định (BaseOptions) của `NetworkClient`.
  2. Chỉ truyền tham số này khi gọi các endpoint cụ thể cần thiết (như đăng nhập `/connect/token` trong `IdentityApi`):
     ```dart
     final response = await _client.post(
       '/connect/token',
       data: { ... },
       contentType: 'application/x-www-form-urlencoded',
     );
     ```

---

## 14. Bổ Sung REST Minimal APIs và Cấu Hình Định Tuyến YARP Gateway Cho Dịch Vụ gRPC (Basket.API)

Khi làm việc với các dịch vụ chỉ hỗ trợ gRPC qua HTTP/2 (như `Basket.API` trong hệ thống C#) trong ứng dụng Flutter Web, chúng ta sẽ gặp giới hạn của trình duyệt (trình duyệt không cho phép điều khiển trực tiếp các khung HTTP/2). Để giải quyết vấn đề này, chúng ta kết hợp hai giải pháp: bổ sung REST Minimal APIs trực tiếp vào Backend gRPC và định tuyến chúng qua YARP BFF Gateway.

### A. Bổ sung REST Minimal APIs vào gRPC Service (C# Backend)
Trong `Program.cs` của dịch vụ `Basket.API`, chúng ta có thể trực tiếp ánh xạ các HTTP GET, POST và DELETE bên cạnh dịch vụ gRPC mà không cần tạo Controller MVC cồng kềnh.

* **Cách xác định định danh người dùng:** Giống như gRPC, chúng ta đọc Claim `"sub"` (subject/user ID) từ mã JWT được gửi kèm trong Header Authorization:
  ```csharp
  var userId = httpContext.User.FindFirst("sub")?.Value ?? "alice";
  ```
* **Mẫu định nghĩa Minimal APIs trong `Program.cs`:**
  ```csharp
  // Lấy giỏ hàng
  app.MapGet("/api/basket", async (IBasketRepository repository, HttpContext httpContext) =>
  {
      var userId = httpContext.User.FindFirst("sub")?.Value ?? "alice";
      var basket = await repository.GetBasketAsync(userId);
      return Results.Ok(basket ?? new CustomerBasket(userId));
  });

  // Cập nhật giỏ hàng
  app.MapPost("/api/basket", async (CustomerBasket basket, IBasketRepository repository, HttpContext httpContext) =>
  {
      var userId = httpContext.User.FindFirst("sub")?.Value ?? "alice";
      basket.BuyerId = userId;
      var updatedBasket = await repository.UpdateBasketAsync(basket);
      return Results.Ok(updatedBasket);
  });
  ```

### B. Cấu hình định tuyến YARP Gateway (eShop.AppHost)
YARP (Yet Another Reverse Proxy) đóng vai trò là BFF (Backend For Frontend) giúp gom tất cả các dịch vụ (Catalog ở cổng `5222`, Identity ở cổng `5223`, Basket ở cổng `5221`) về một cổng duy nhất (ví dụ: `5222`).

Để thêm định tuyến cho một dịch vụ mới thông qua YARP:
1. **Liên kết Resource trong AppHost:** Truyền biến tài nguyên API (ví dụ: `basketApi`) vào phương thức mở rộng YARP cấu hình BFF:
   ```csharp
   builder.AddYarp("mobile-bff")
          .ConfigureMobileBffRoutes(catalogApi, orderingApi, identityApi, basketApi);
   ```
2. **Khai báo Route và Cluster trong Extensions:**
   * Thêm tham số `basketApi` vào chữ ký của hàm `ConfigureMobileBffRoutes`.
   * Khai báo cluster của dịch vụ basket: `var basketCluster = yarp.AddCluster(basketApi);`.
   * Cấu hình Route chuyển tiếp bắt đầu bằng `/api/basket`:
     ```csharp
     yarp.AddRoute("/api/basket/{*any}", basketCluster);
     ```
   Nhờ đó, khi Flutter client gửi yêu cầu đến `http://localhost:5222/api/basket`, YARP Gateway sẽ tự động chuyển tiếp yêu cầu đến cổng `5221` của dịch vụ `Basket.API` một cách trong suốt.

### C. Mở rộng NetworkClient hỗ trợ đầy đủ các phương thức HTTP (PUT & DELETE)
Để tương tác trọn vẹn với các REST API CRUD (như xóa giỏ hàng bằng `DELETE` hoặc hủy/vận chuyển đơn hàng bằng `PUT`), lớp `NetworkClient` trong gói `core_network` đã được mở rộng để cung cấp hai hàm helper bất đồng bộ bọc ngoài thư viện Dio:

* **Phương thức PUT:**
  ```dart
  Future<Response> put(
    String path, {
    dynamic data,
    String? contentType,
  }) async {
    return await dio.put(
      path,
      data: data,
      options: Options(contentType: contentType),
    );
  }
  ```
* **Phương thức DELETE:**
  ```dart
  Future<Response> delete(
    String path, {
    dynamic data,
  }) async {
    return await dio.delete(path, data: data);
  }
}

---

## 15. Sử dụng `.withValues(alpha: ...)` thay thế cho `withOpacity(...)` trong Flutter 3.24+

Từ phiên bản Flutter 3.24.0, hàm `withOpacity` trên đối tượng `Color` đã bị đánh dấu deprecated (không khuyến khích sử dụng nữa).
- **Lý do:** `withOpacity` thực hiện phép nhân toán học trên kênh Alpha có thể dẫn tới sai số dấu phẩy động (precision loss).
- **Giải pháp mới:** Sử dụng phương thức `.withValues(alpha: ...)` để gán giá trị độ mờ (alpha) một cách chính xác hơn. Giá trị `alpha` vẫn nhận khoảng từ `0.0` (trong suốt hoàn toàn) đến `1.0` (đậm đặc hoàn toàn).

**Ví dụ chuyển đổi:**
```diff
-Colors.black.withOpacity(0.5)
+Colors.black.withValues(alpha: 0.5)
```

---

## 16. Cơ chế Idempotency Request Header (`x-requestid`) trong API Đơn hàng

Khi gửi yêu cầu tạo đơn hàng (Checkout / Create Order) trong hệ thống eShop, hành động này cần đảm bảo tính **Idempotent** (tránh trường hợp khách hàng bị tạo 2 đơn hàng trùng lặp hoặc trừ tiền 2 lần khi click nút Thanh toán nhiều lần do mạng lag).
- **Cách hoạt động:** Frontend Flutter tạo ra một mã định danh ngẫu nhiên và duy nhất UUID (Universal Unique Identifier) cho mỗi phiên giao dịch thanh toán.
- **Thực thi:** Mã này được truyền vào Header của request HTTP với tên `x-requestid`.
- **Backend xử lý:** Backend C# sẽ lưu trữ `x-requestid` này. Nếu nhận được một request khác có cùng `x-requestid` trong một khoảng thời gian ngắn, Backend sẽ trả về kết quả của đơn hàng cũ thay vì tạo thêm đơn hàng mới.

**Ví dụ trong Dart (sử dụng gói `uuid`):**
```dart
import 'package:uuid/uuid.dart';

final requestId = const Uuid().v4();
final response = await _client.post(
  '/api/v1/orders/draft',
  data: order.toJson(),
  headers: {
    'x-requestid': requestId,
  },
);
```

---

## 17. Tầm quan trọng của định tuyến YARP không chứa dấu gạch chéo ở cuối (Trailing Slashes)

Khi cấu hình định tuyến cho các API Gateway YARP BFF (`eShop.AppHost`):
* Cú pháp `/api/basket/{*any}` chỉ khớp khi có các thư mục con đằng sau hoặc có dấu gạch chéo `/` ở cuối (ví dụ: `/api/basket/` hoặc `/api/basket/123`).
* Khi Flutter client gọi thẳng tới endpoint gốc như `/api/basket` hoặc `/api/orders` (không có dấu gạch chéo `/` ở cuối), Gateway sẽ không khớp đường dẫn gốc và trả về **404 Not Found**.

**Cách khắc phục chuẩn:** Cần cấu hình khai báo song song hai tuyến đường trong AppHost:
```csharp
yarp.AddRoute("/api/basket", basketCluster);
yarp.AddRoute("/api/basket/{*any}", basketCluster);
```

---

## 18. Mở rộng NetworkClient hỗ trợ `queryParameters` cho POST, PUT, DELETE

Mặc định khi sử dụng thư viện Dio trong Dart, các phương thức `post`, `put`, `delete` đều hỗ trợ truyền tham số `queryParameters` dạng chuỗi truy vấn (ví dụ: `/api/orders?api-version=1.0`).
* Để đồng bộ hóa và hỗ trợ tốt nhất cho các microservice phiên bản hóa (API Versioning) yêu cầu truyền `api-version` trực tiếp ở URL truy vấn, lớp `NetworkClient` trong `core_network` đã được mở rộng:
```dart
  Future<Response> post(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters, // Thêm tham số này
    String? contentType,
    Map<String, dynamic>? headers,
  }) async {
    return await dio.post(
      path,
      data: data,
      queryParameters: queryParameters, // Truyền cho Dio
      options: Options(contentType: contentType, headers: headers),
    );
  }
```

---

## 19. Giải quyết lỗi tràn giao diện (Overflow) bằng LayoutBuilder và Responsive Grid

Lỗi tràn pixel hiển thị (kẻ sọc vàng đen) thường xuất hiện trên Flutter Web hoặc các thiết bị di động khi không gian hiển thị bị co hẹp nhưng kích thước phần tử bên trong bị ép cố định.
1. **Tràn chiều dọc (Vertical Overflow):** Khi chia 2 cột với tỷ lệ `childAspectRatio = 0.68` cố định, màn hình hẹp làm giảm chiều cao card sản phẩm, ép phần text/image bên trong bị thiếu chỗ và tràn đáy.
   * **Giải pháp:** Sử dụng `LayoutBuilder` để đo kích thước hiện tại và tăng/giảm số cột (`crossAxisCount`) cũng như thay đổi aspect ratio (`childAspectRatio`) thích ứng với chiều rộng thực tế.
2. **Tràn chiều ngang (Horizontal Overflow):** Khi chữ (ví dụ: giá tiền) và nút bấm nằm trên cùng một `Row` và vượt quá chiều rộng của cột.
   * **Giải pháp:** Bọc Text trong widget `Expanded` hoặc `Flexible`, đồng thời chỉ định `maxLines: 1` và `overflow: TextOverflow.ellipsis` để tự động thu gọn nếu thiếu không gian.

---

## 20. Hiển thị & Hỗ trợ Tiếp cận với Widget `Semantics` (SEO cho Mobile & Web)

Để nâng cao khả năng tiếp cận (Accessibility - hỗ trợ các trình đọc màn hình như TalkBack trên Android và VoiceOver trên iOS) cũng như cải thiện khả năng SEO khi biên dịch sang nền tảng Flutter Web, chúng ta sử dụng các widget ngữ nghĩa.

### Widget `Semantics` là gì?
- `Semantics` là một widget đặc biệt dùng để gán các thuộc tính ngữ nghĩa (như nhãn mô tả, loại widget, trạng thái) vào cây ngữ nghĩa (Semantics Tree) của ứng dụng.
- **Trên nền tảng Di động (Mobile):** Khi người dùng khiếm thị bật các dịch vụ hỗ trợ tiếp cận (Screen Readers), trình đọc màn hình sẽ đọc to các mô tả cấu hình trong `Semantics` (nhãn `label`, trạng thái hoạt động...).
- **Trên nền tảng Web:** Khi chạy ứng dụng dưới dạng Flutter Web, Flutter sẽ tự động ánh xạ (compile) các widget `Semantics` này thành các thẻ HTML ngữ nghĩa chuẩn tương ứng như `<h1>`, `<button>`, `<input>`, giúp các công cụ tìm kiếm (Search Engine Crawlers) thu thập dữ liệu và lập chỉ mục (index) dễ dàng hơn -> **Tối ưu SEO**.

**Ví dụ:**
```dart
Semantics(
  header: true, // Ánh xạ thành thẻ <h1> trên Web (như tiêu đề trang)
  child: const Text('Tiêu đề sản phẩm'),
)

Semantics(
  label: 'Nút bấm thanh toán đơn hàng',
  button: true, // Ánh xạ thành thẻ <button> trên Web
  child: GestureDetector(
    onTap: () => thanhToan(),
    child: MyCustomButtonWidget(),
  ),
)
```

### `MergeSemantics` và `ExcludeSemantics`
* **`MergeSemantics`**: Hợp nhất tất cả các thuộc tính ngữ nghĩa của các widget con bên trong thành một nút ngữ nghĩa duy nhất. Rất thích hợp khi bạn muốn trình đọc màn hình đọc toàn bộ một cụm thông tin (ví dụ: một Card sản phẩm gồm ảnh, tên, giá) thay vì người dùng phải vuốt qua từng phần nhỏ lẻ.
* **`ExcludeSemantics`**: Loại bỏ hoàn toàn widget con và các con của nó khỏi cây ngữ nghĩa. Thường dùng để loại bỏ các hình ảnh trang trí, icon nền không mang giá trị thông tin thực tế, giúp trình đọc màn hình không bị phân tâm.

---

## 21. Quy chuẩn Kích thước Touch Target (Vùng chạm trên thiết bị di động)

Khi thiết kế giao diện di động, tính thân thiện với thao tác chạm là tối quan trọng. Thiết kế các nút bấm quá nhỏ sẽ khiến người dùng khó bấm trúng, dễ bấm nhầm và vi phạm các quy chuẩn thiết kế của Apple/Google.

### Kích thước Vùng chạm Tiêu chuẩn
* **Quy chuẩn iOS (Apple):** Kích thước vùng chạm tối thiểu khuyến nghị là **44 x 44 Logical Pixels** (điểm ảnh logic).
* **Quy chuẩn Android (Google Material Design):** Kích thước vùng chạm tối thiểu là **48 x 48 Logical Pixels**.

### Ứng dụng trong mã nguồn
Trong màn hình giỏ hàng (`basket_screen.dart`), các nút tăng và giảm số lượng sản phẩm được thiết kế bằng container có kích thước chạm chuẩn để tránh tình trạng các nút quá nhỏ đặt gần nhau dẫn đến thao tác lỗi:
```dart
Widget _buildQuantityButton({
  required IconData icon,
  required String label,
  required VoidCallback onPressed,
}) {
  return Semantics(
    label: label,
    button: true,
    child: Container(
      width: 44, // Kích thước vùng chạm tối thiểu chuẩn 44px
      height: 44,
      decoration: BoxDecoration(
        color: AppTheme.primaryLight,
        borderRadius: BorderRadius.circular(12),
      ),
      child: IconButton(
        icon: Icon(icon, color: AppTheme.primary, size: 16),
        padding: EdgeInsets.zero,
        onPressed: onPressed,
      ),
    ),
  );
}
```

---

## 22. Hệ thống Theme tập trung (Design System) trong Flutter

Để ứng dụng không bị rối rắm bởi quá nhiều màu sắc khác nhau ("không quá màu sắc rực rỡ") và dễ dàng bảo trì giao diện, chúng ta định nghĩa một hệ thống Design System tĩnh, tập trung.

### Cấu trúc lớp `AppTheme`
Chúng ta tạo một file quản lý theme tập trung như [theme.dart](file:///d:/Subagent/Nhom_10_DevOps_Testing_Production/eShop-main/src/eShop_flutter/apps/mobile_app/lib/theme.dart) để định nghĩa các màu sắc chủ đạo, kiểu font chữ (ví dụ dùng các bảng màu Slate nhã nhặn, dịu mắt):
```dart
class AppTheme {
  static const Color background = Color(0xFFF8FAFC); // Slate 50 nhã nhặn
  static const Color cardBg = Color(0xFFFFFFFF);     // Trắng tinh tế
  static const Color textPrimary = Color(0xFF0F172A); // Slate 900 cho tiêu đề
  static const Color textSecondary = Color(0xFF475569); // Slate 600 cho mô tả
  static const Color primary = Color(0xFF0891B2);    // Cyan 600 làm điểm nhấn
  
  static ThemeData get themeData {
    return ThemeData(
      scaffoldBackgroundColor: background,
      primaryColor: primary,
      colorScheme: const ColorScheme.light(
        primary: primary,
        secondary: primary,
        surface: cardBg,
      ),
    );
  }
}
```

### Sử dụng Theme toàn cục
* **Đăng ký theme:** Tại [main.dart](file:///d:/Subagent/Nhom_10_DevOps_Testing_Production/eShop-main/src/eShop_flutter/apps/mobile_app/lib/main.dart), truyền `AppTheme.themeData` vào thuộc tính `theme` của `MaterialApp`.
* **Sử dụng trong widget con:** Các widget con sẽ tự động kế thừa bảng màu của hệ thống, hoặc có thể truy cập động thông qua `Theme.of(context)` để lấy các thuộc tính định vị của theme (như `primaryColor`, `colorScheme.surface`...). Việc này giúp giao diện nhất quán trên mọi màn hình.


