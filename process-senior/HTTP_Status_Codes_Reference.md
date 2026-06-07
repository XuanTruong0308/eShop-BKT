# HTTP Status Codes — Tổng Hợp Đầy Đủ

> Tài liệu tham chiếu nhanh tất cả mã trạng thái HTTP theo RFC 9110 (HTTP Semantics) và các RFC mở rộng. Phù hợp để học, debug API, và đi phỏng vấn.

---

## Phân Loại Tổng Quan

HTTP status code chia thành **5 class** dựa trên chữ số đầu:

| Class | Range | Ý nghĩa | Vai trò |
|-------|-------|---------|---------|
| **1xx** | 100–199 | Informational | Server đang xử lý, chưa xong |
| **2xx** | 200–299 | Successful | Request đã pass, server xử lý OK |
| **3xx** | 300–399 | Redirection | Client cần làm thêm bước (redirect) |
| **4xx** | 400–499 | Client Error | Lỗi do client (request sai) |
| **5xx** | 500–599 | Server Error | Lỗi do server (code crash, downstream fail) |

```
        1xx          2xx          3xx          4xx          5xx
         │            │            │            │            │
      Đang xử       Thành        Cần          Client      Server
       lý...         công       redirect       sai          sai
```

---

## 1xx — Informational (Đang Xử Lý)

Hiếm gặp trong CRUD API thông thường, chủ yếu xuất hiện ở giao thức real-time.

| Mã | Tên | Ý nghĩa | Khi nào gặp |
|----|-----|---------|-------------|
| **100** | Continue | "Tiếp tục gửi phần còn lại của request" | Client gửi `Expect: 100-continue` để hỏi server có chấp nhận body lớn không trước khi upload |
| **101** | Switching Protocols | "Chuyển protocol theo yêu cầu của bạn" | Upgrade từ HTTP sang WebSocket |
| **102** | Processing (WebDAV) | "Tao đang làm, đừng timeout" | Operation dài, server cần báo client chờ |
| **103** | Early Hints | "Đây là một phần header sớm để bạn preload" | Server hint browser preload CSS/JS trước khi response chính sẵn sàng |

---

## 2xx — Successful (Thành Công)

Class quan trọng nhất khi xây REST API. Hiểu rõ khi nào dùng mã nào là dấu hiệu của senior dev.

| Mã | Tên | Ý nghĩa | Khi nào dùng |
|----|-----|---------|--------------|
| **200** | OK | "Xong, đây là kết quả" | GET trả dữ liệu, PUT update thành công có body, generic success |
| **201** | Created | "Tạo mới resource thành công" | POST tạo mới (đính kèm header `Location: /resource/123`) |
| **202** | Accepted | "Đã nhận, sẽ xử lý bất đồng bộ" | Queue job, batch background, gọi service async |
| **203** | Non-Authoritative Information | "Dữ liệu OK nhưng đến từ proxy/cache" | Hiếm gặp |
| **204** | No Content | "Xong, không có gì trả về" | DELETE thành công, PUT/PATCH không cần body |
| **205** | Reset Content | "Xong, client nên reset form" | Form input đã save, browser nên clear |
| **206** | Partial Content | "Đây là một phần dữ liệu bạn yêu cầu" | Range request (download tải lại từ giữa, video streaming) |
| **207** | Multi-Status (WebDAV) | "Nhiều operation, mỗi cái một status" | Batch API, WebDAV |
| **208** | Already Reported (WebDAV) | "Item này đã có trong response trước rồi" | WebDAV |
| **226** | IM Used | "Áp delta encoding theo HTTP Delta" | Gần như không gặp |

### Phân biệt 200 vs 201 vs 204 (Hay Bị Nhầm)

```
POST /items      → 201 Created    (tạo mới)   + Location header
PUT  /items/123  → 200 OK         (update có body)
PUT  /items/123  → 204 No Content (update không body)
GET  /items/123  → 200 OK         (đọc dữ liệu)
DELETE /items/123 → 204 No Content (xoá xong, không cần body)
```

---

## 3xx — Redirection (Cần Chuyển Hướng)

Hiếm dùng trong REST JSON API thuần, nhưng phổ biến ở web browser flow.

| Mã | Tên | Ý nghĩa | Khi nào dùng |
|----|-----|---------|--------------|
| **300** | Multiple Choices | "Có nhiều resource phù hợp, chọn 1" | Hiếm gặp |
| **301** | Moved Permanently | "URL đã đổi vĩnh viễn, lần sau dùng URL mới" | Domain redirect, đổi cấu trúc URL |
| **302** | Found | "Tạm thời ở chỗ khác, lần sau vẫn dùng URL cũ" | Login redirect (nhưng nên dùng 303) |
| **303** | See Other | "Sau POST này, đi GET URL khác" | POST-Redirect-GET pattern, tránh resubmit form khi reload |
| **304** | Not Modified | "Resource không đổi, dùng cache đi" | Conditional request với `If-Modified-Since` / `If-None-Match` |
| **305** | Use Proxy | (Deprecated) | Không dùng |
| **307** | Temporary Redirect | "Đi URL khác, giữ nguyên method" | Như 302 nhưng giữ POST/PUT (302 thường bị browser chuyển sang GET) |
| **308** | Permanent Redirect | "URL đổi vĩnh viễn, giữ nguyên method" | Như 301 nhưng giữ POST/PUT |

### Phân biệt 301 vs 302 vs 307 vs 308

```
                        Vĩnh viễn   Tạm thời
              ────────  ─────────  ─────────
Method có      Có thể     301         302       (cho phép GET hoá)
thể đổi
              ────────  ─────────  ─────────
Method giữ      308         307                 (REST API nên dùng các mã này)
nguyên
```

---

## 4xx — Client Error (Lỗi Do Client)

Class mà bạn debug nhiều nhất khi build API. Mỗi mã có ý nghĩa rất cụ thể, đừng dùng bừa.

### 4xx Cơ Bản (Phải Thuộc)

| Mã | Tên | Ý nghĩa | Khi nào trả |
|----|-----|---------|-------------|
| **400** | Bad Request | "Request sai cú pháp/dữ liệu" | JSON body invalid, validation fail, missing required field, version sai |
| **401** | Unauthorized | "Tao không biết bạn là ai" | Thiếu token, token expired, signature sai, issuer/audience không khớp |
| **402** | Payment Required | "Cần thanh toán" (reserved) | Hiếm dùng, một số API quota dùng (Stripe, GitHub) |
| **403** | Forbidden | "Tao biết bạn rồi, nhưng không cho" | Token hợp lệ nhưng thiếu role/scope/permission |
| **404** | Not Found | "Resource không tồn tại" | URL không match endpoint, hoặc ID không có trong DB |
| **405** | Method Not Allowed | "Method này không hỗ trợ" | POST tới endpoint chỉ accept GET |
| **406** | Not Acceptable | "Tao không trả được format bạn yêu cầu" | Client `Accept: application/xml` nhưng server chỉ có JSON |
| **407** | Proxy Authentication Required | "Proxy cần xác thực" | Hiếm gặp ở API |
| **408** | Request Timeout | "Bạn gửi quá chậm, tao đóng connection" | Client gửi request quá lâu |
| **409** | Conflict | "Xung đột state" | Optimistic concurrency, version mismatch, duplicate resource |
| **410** | Gone | "Resource từng có, giờ đã bị xoá vĩnh viễn" | Cứng hơn 404 — báo client đừng thử lại |
| **411** | Length Required | "Phải có Content-Length" | Hiếm |
| **412** | Precondition Failed | "Điều kiện `If-Match` không thoả" | Optimistic concurrency với ETag |
| **413** | Content Too Large | "Body quá to" | Upload file vượt limit |
| **414** | URI Too Long | "URL dài quá" | Query string khổng lồ |
| **415** | Unsupported Media Type | "Content-Type không hỗ trợ" | Client gửi `text/plain` cho endpoint chỉ nhận JSON |
| **416** | Range Not Satisfiable | "Range bạn xin không hợp lệ" | Resume download lỗi |
| **417** | Expectation Failed | "`Expect:` header không thoả" | Hiếm |
| **418** | I'm a Teapot | "Tao là ấm trà, không pha cà phê được" | Joke RFC, không dùng thực tế |
| **421** | Misdirected Request | "Bạn gửi sai server" | HTTP/2 |
| **422** | Unprocessable Content | "Cú pháp đúng, nội dung sai" | JSON parse OK nhưng business rule fail (vd: email đúng format nhưng đã tồn tại) |
| **423** | Locked (WebDAV) | "Resource đang khoá" | WebDAV |
| **424** | Failed Dependency (WebDAV) | "Operation phụ thuộc cái khác đã fail" | WebDAV |
| **425** | Too Early | "Tao chưa sẵn sàng nhận đâu" | Liên quan TLS 0-RTT |
| **426** | Upgrade Required | "Phải upgrade protocol" | Hiếm |
| **428** | Precondition Required | "Phải có `If-Match` thì tao mới làm" | Bắt buộc optimistic concurrency |
| **429** | Too Many Requests | "Bạn gửi quá nhanh, slow down" | Rate limiting |
| **431** | Request Header Fields Too Large | "Header to quá" | Cookie / token to vượt limit |
| **451** | Unavailable For Legal Reasons | "Vì lý do pháp lý, tao không trả nội dung này" | Censorship, GDPR, DMCA takedown |

### Phân Biệt 4 Mã Hay Nhầm

```
400 Bad Request      → Cú pháp request sai (JSON parse fail)
401 Unauthorized     → Authentication fail (chưa biết bạn là ai)
403 Forbidden        → Authorization fail (biết bạn nhưng không cho)
404 Not Found        → Resource không tồn tại
422 Unprocessable    → Cú pháp đúng nhưng business rule fail
```

Ví dụ:
```
POST /users
{
  "email": "not-an-email",     ← 422 (cú pháp JSON OK, validation fail)
  "age": "twenty"              ← 400 (parse "twenty" thành int fail)
}
```

---

## 5xx — Server Error (Lỗi Do Server)

Class mà bạn không muốn user thấy nhưng phải biết khi đọc log production.

| Mã | Tên | Ý nghĩa | Khi nào gặp |
|----|-----|---------|-------------|
| **500** | Internal Server Error | "Server crash, generic error" | Unhandled exception, NullReferenceException, DB connection lỗi |
| **501** | Not Implemented | "Server chưa làm tính năng này" | Endpoint chưa code xong |
| **502** | Bad Gateway | "Tao là proxy, downstream trả lỗi" | Reverse proxy (Nginx, YARP) thấy upstream fail |
| **503** | Service Unavailable | "Server đang quá tải / maintenance" | Health check fail, circuit breaker open, scaling chưa kịp |
| **504** | Gateway Timeout | "Tao là proxy, downstream không trả lời" | Upstream service slow / down |
| **505** | HTTP Version Not Supported | "Phiên bản HTTP không hỗ trợ" | Hiếm |
| **506** | Variant Also Negotiates | Internal config error | Hiếm |
| **507** | Insufficient Storage (WebDAV) | "Hết dung lượng" | WebDAV |
| **508** | Loop Detected (WebDAV) | "Phát hiện vòng lặp vô tận" | WebDAV |
| **510** | Not Extended | Hiếm | Hiếm |
| **511** | Network Authentication Required | "Phải login captive portal" | Wifi public chặn truy cập |

### Phân Biệt 502 vs 503 vs 504

```
                        Tao crash         Tao quá tải       Downstream timeout
                        ─────────        ─────────        ──────────────
Server tự nó            500              503               
Tao là gateway/proxy   502              503               504
```

---

## Mã Status Theo Use Case Phổ Biến

### REST CRUD API

| Action | Mã success | Mã fail thường gặp |
|--------|------------|---------------------|
| `GET /items` (list) | 200 | 401, 403 |
| `GET /items/{id}` | 200 | 401, 403, 404 |
| `POST /items` (create) | **201** + `Location` | 400, 401, 403, 409 (duplicate), 422 |
| `PUT /items/{id}` (update có body) | 200 | 400, 401, 403, 404, 409, 422 |
| `PUT /items/{id}` (update không body) | 204 | 400, 401, 403, 404, 409, 422 |
| `PATCH /items/{id}` | 200 hoặc 204 | 400, 401, 403, 404, 409, 422 |
| `DELETE /items/{id}` | **204** | 401, 403, 404, 409 |

### Authentication / Authorization Flow

| Tình huống | Mã |
|------------|-----|
| Login thành công | 200 (kèm token) hoặc 302 (redirect) |
| Sai username/password | 401 |
| Login OK nhưng tài khoản bị khóa | 403 |
| Token expired | 401 (kèm `WWW-Authenticate`) |
| Token thiếu scope/role | 403 |
| Quá nhiều request | 429 |
| Gọi endpoint không tồn tại | 404 |

### File Upload

| Tình huống | Mã |
|------------|-----|
| Upload thành công | 201 (kèm Location) |
| File quá to | 413 |
| Sai content type | 415 |
| Hết dung lượng | 507 |

### Pagination & Caching

| Tình huống | Mã |
|------------|-----|
| Range hợp lệ | 206 Partial Content |
| Cache hit (resource không đổi) | 304 Not Modified |
| Range không hợp lệ | 416 |

---

## Sơ Đồ Quyết Định: Trả Mã Gì?

```
┌─────────────────────────────────────────────────────────┐
│                Server xử lý request                     │
└──────────────────────┬──────────────────────────────────┘
                       │
        ┌──────────────┴──────────────┐
        │                             │
   Lỗi do client                  Server crash
   (4xx)                          (5xx)
        │                             │
   ┌────┴────┐                   ┌────┴────┐
   │         │                   │         │
   Auth?    Khác?               Tao?    Downstream?
   │         │                   │         │
 401/403  400/404/             500       502/503/504
        409/422/429
```

### Auth Sub-decision

```
Có token?                      
  ├─ Không ──────────────────► 401
  └─ Có
     │
     Token hợp lệ?
        ├─ Không (expired/sai chữ ký) ─► 401
        └─ Có
           │
           User có quyền?
              ├─ Không ──────────────► 403
              └─ Có ──────────────► 200/201/204 (tùy action)
```

---

## Câu Hỏi Phỏng Vấn Thường Gặp

### Câu 1: Khác biệt 401 và 403?

- **401** = "ai đó?" — chưa xác thực được, redirect login.
- **403** = "biết rồi, không cho" — đã login nhưng không đủ quyền, không phải redirect login.

### Câu 2: POST tạo user mới nên trả gì?

**201 Created** + header `Location: /users/{newId}` + body chứa user vừa tạo (tùy convention).

### Câu 3: PUT vs PATCH trả gì?

- PUT: 200 (có body trả về) hoặc 204 (không body).
- PATCH: thường 200 với resource đã update.

### Câu 4: Khi nào dùng 422 thay vì 400?

- **400**: cú pháp request sai (JSON parse fail, query param sai type).
- **422**: cú pháp đúng, business rule fail (validation logic).

Ví dụ: gửi `{"age": "twenty"}` → 400 (parse string→int fail). Gửi `{"age": 200}` → 422 (parse OK nhưng > 150 invalid theo logic).

### Câu 5: 502 vs 503 vs 504?

- **502**: gateway gọi upstream, upstream trả lỗi.
- **503**: server bận / down / maintenance, cờ-báo "đợi tí thử lại".
- **504**: gateway gọi upstream, upstream timeout không trả lời.

### Câu 6: Mã nào nên kèm header `Retry-After`?

- **429** (rate limit) — báo client đợi bao lâu trước khi retry.
- **503** (unavailable) — báo client lúc nào server sẵn sàng lại.

### Câu 7: 304 Not Modified hoạt động ra sao?

Client gửi `If-None-Match: <etag>` → server so với etag hiện tại → nếu trùng, trả 304 không kèm body, browser dùng cache cũ. Tiết kiệm băng thông.

---

## Quy Tắc Dùng Status Code (Best Practice)

1. **Đừng trả 200 cho mọi thứ.** "200 + body có error" là anti-pattern. Dùng đúng class.
2. **POST tạo mới → 201, không phải 200.** Kèm `Location` header.
3. **DELETE thành công → 204, không phải 200.** Không cần body.
4. **Auth fail = 401, perm fail = 403.** Đừng trộn lẫn.
5. **Validation fail = 422 hoặc 400.** Tuỳ project convention nhất quán.
6. **5xx phải có log trace.** Đừng để 500 không kèm thông tin debug.
7. **Trả ProblemDetails (RFC 9457)** cho 4xx/5xx, kèm `type`, `title`, `status`, `detail`, `traceId`.

---

## Tham Khảo

- RFC 9110 — HTTP Semantics: <https://datatracker.ietf.org/doc/html/rfc9110>
- RFC 9457 — ProblemDetails: <https://datatracker.ietf.org/doc/html/rfc9457>
- MDN HTTP status: <https://developer.mozilla.org/en-US/docs/Web/HTTP/Status>
- IANA HTTP Status Codes Registry: <https://www.iana.org/assignments/http-status-codes>

---

## Cheat Sheet Một Trang

```
1xx  Informational    → đang xử lý
   100 Continue
   101 Switching Protocols (WebSocket)
   103 Early Hints

2xx  Successful       → thành công
   200 OK             → generic success
   201 Created        → POST tạo mới
   202 Accepted       → async job đã queue
   204 No Content     → DELETE/PUT không body
   206 Partial        → range request

3xx  Redirection      → cần chuyển hướng
   301 Moved Permanently
   302 Found (tạm thời)
   303 See Other (POST→GET)
   304 Not Modified (cache hit)
   307 Temporary Redirect (giữ method)
   308 Permanent Redirect (giữ method)

4xx  Client Error     → client sai
   400 Bad Request    → cú pháp/data sai
   401 Unauthorized   → chưa auth
   403 Forbidden      → auth rồi nhưng không có quyền
   404 Not Found      → resource không tồn tại
   405 Method Not Allowed
   409 Conflict       → version mismatch
   410 Gone           → đã xoá vĩnh viễn
   413 Content Too Large
   415 Unsupported Media Type
   422 Unprocessable  → business rule fail
   429 Too Many Requests → rate limit

5xx  Server Error     → server sai
   500 Internal       → crash
   501 Not Implemented
   502 Bad Gateway    → downstream error
   503 Service Unavailable → quá tải / maintenance
   504 Gateway Timeout → downstream timeout
```
