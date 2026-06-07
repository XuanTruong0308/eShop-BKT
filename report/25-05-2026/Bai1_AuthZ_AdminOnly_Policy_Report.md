# Bài 1: Tạo Policy `AdminOnly` Cho Catalog API

> **Yêu cầu (deadline-25-05-2026.md):**
> - Tạo policy `AdminOnly` cho Catalog Create/Update/Delete.
> - Test đủ 3 case: không token → 401, token user thường → 403, token admin → 200.
> - Giải thích sự khác biệt 401 vs 403 và lý do.

---

## 1. Mục Tiêu & Hành Trình

Bài tập yêu cầu tưởng đơn giản nhưng đụng vào nhiều mảnh của hệ thống microservice eShop. Mình phải sửa **6 file** trong 2 service (Identity.API và Catalog.API), gặp 4 lỗi runtime liên tiếp trước khi pass cả 3 case. Báo cáo này tóm tắt toàn bộ hành trình.

```
┌────────────────────────────────────────────────────────────────────┐
│                     LUỒNG XÁC THỰC & PHÂN QUYỀN                    │
├────────────────────────────────────────────────────────────────────┤
│                                                                    │
│   Client (REST Client)                                             │
│       │                                                            │
│       │ 1. POST /connect/token                                     │
│       │    (username + password + scope=catalog)                   │
│       ▼                                                            │
│   ┌─────────────────────────────────────┐                          │
│   │   Identity.API  (IdentityServer)    │                          │
│   │   ─────────────────────────────────  │                         │
│   │   • Verify user trong DB Postgres   │                          │
│   │   • ProfileService nhồi claim       │                          │
│   │     (name, email, role, ...)        │                          │
│   │   • Phát JWT (HS256/RSA)            │                          │
│   └─────────────────────────────────────┘                          │
│       │                                                            │
│       │ 2. Trả về access_token (JWT)                               │
│       ▼                                                            │
│   Client                                                           │
│       │                                                            │
│       │ 3. POST /api/catalog/items?api-version=1.0                 │
│       │    Authorization: Bearer <token>                           │
│       ▼                                                            │
│   ┌──────────────────────────────────────────────────────┐         │
│   │              Catalog.API  Pipeline                   │         │
│   │   ────────────────────────────────────────────────   │         │
│   │   ① Routing + API Versioning   → 400 nếu thiếu ver   │         │
│   │   ② UseAuthentication           → 401 nếu token sai  │         │
│   │   ③ UseAuthorization (AdminOnly)→ 403 nếu role sai   │         │
│   │   ④ Endpoint MapPost /items     → 201 nếu pass hết   │         │
│   └──────────────────────────────────────────────────────┘         │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
```

Kiến thức quan trọng: **mỗi mã trạng thái HTTP đến từ 1 lớp middleware khác nhau**. Hiểu pipeline thì mới biết khi 403 trả về thì lỗi nằm đâu, khi 401 thì lỗi nằm đâu.

---

## 2. Sơ Đồ Sequence: Bắn 3 Request Thực Tế

Đây là trình tự bắn request mà mình test để pass cả 3 case:

```
┌────────────┐         ┌──────────────┐         ┌─────────────┐
│ REST Client│         │ Identity.API │         │ Catalog.API │
│ (.http)    │         │ :5243        │         │ :5222       │
└─────┬──────┘         └──────┬───────┘         └──────┬──────┘
      │                       │                        │
      │ ① Lấy token alice     │                        │
      │ POST /connect/token   │                        │
      │ user=alice            │                        │
      ├──────────────────────▶│                        │
      │                       │                        │
      │ access_token (alice)  │                        │
      │◀──────────────────────┤                        │
      │                       │                        │
      │ ② Lấy token admin     │                        │
      │ POST /connect/token   │                        │
      │ user=admin            │                        │
      ├──────────────────────▶│                        │
      │                       │                        │
      │ access_token (admin)  │                        │
      │◀──────────────────────┤                        │
      │                       │                        │
      │ ③ CASE 1: KHÔNG TOKEN                          │
      │ POST /api/catalog/items?api-version=1.0        │
      ├───────────────────────────────────────────────▶│
      │                       │                        │
      │                       │  401 Unauthorized      │
      │◀───────────────────────────────────────────────┤
      │                       │                        │
      │ ④ CASE 2: TOKEN ALICE (role=customer)          │
      │ POST /api/catalog/items?api-version=1.0        │
      │ Authorization: Bearer <alice_token>            │
      ├───────────────────────────────────────────────▶│
      │                       │                        │
      │                       │  403 Forbidden         │
      │◀───────────────────────────────────────────────┤
      │                       │                        │
      │ ⑤ CASE 3: TOKEN ADMIN (role=admin)             │
      │ POST /api/catalog/items?api-version=1.0        │
      │ Authorization: Bearer <admin_token>            │
      ├───────────────────────────────────────────────▶│
      │                       │                        │
      │                       │  201 Created           │
      │◀───────────────────────────────────────────────┤
      │                       │                        │
```

---

## 3. Code Đã Sửa

### 3.1. Bên Identity.API (Phát Token Đúng Format)

| File | Vai trò sửa |
|------|-------------|
| `Configuration/Config.cs` | Thêm ApiResource `catalog`, ApiScope `catalog`, scope vào client `webapp`/`maui`, thêm client mới `catalogtest` (ROPC) cho test |
| `UsersSeed.cs` | Seed user `admin` + claim `role=admin`, gán `role=customer` cho alice |
| `Services/ProfileService.cs` | Bổ sung `GetClaimsAsync` để đọc custom claim từ DB → nhồi vào access token |

#### `Config.cs` — ApiResource Có UserClaims

```csharp
new ApiResource("catalog", "Catalog Service")
{
    UserClaims = { "role" } // báo Identity nhét claim "role" vào token cho audience catalog
},
```

#### `UsersSeed.cs` — Seed Admin + Claim

```csharp
var admin = await userManager.FindByNameAsync("admin");
if (admin == null)
{
    admin = new ApplicationUser { UserName = "admin", Email = "admin@gmail.com", ... };
    var result = await userManager.CreateAsync(admin, "Pass0308@");
    await userManager.AddClaimAsync(admin, new Claim("role", "admin"));
}

if (alice != null && !aliceClaims.Any(c => c.Type == "role"))
{
    await userManager.AddClaimAsync(alice, new Claim("role", "customer"));
}
```

#### `ProfileService.cs` — Đọc Claim Từ DB

```csharp
var claims = GetClaimsFromUser(user).ToList();

// Bổ sung custom claim đã lưu trong AspNetUserClaims (ví dụ "role")
var dbClaims = await _userManager.GetClaimsAsync(user);
claims.AddRange(dbClaims);

context.IssuedClaims = claims;
```

### 3.2. Bên Catalog.API (Bật Auth + Áp Policy)

| File | Vai trò sửa |
|------|-------------|
| `Program.cs` | Bật JWT auth, định nghĩa policy `AdminOnly`, thêm middleware `UseAuthentication`/`UseAuthorization` |
| `appsettings.json` | Thêm section `Identity { Url, Audience }` |
| `Apis/CatalogApi.cs` | Áp `RequireAuthorization("AdminOnly")` lên 4 endpoint mutation (PUT v1, PUT v2, POST, DELETE) |
| `eShop.AppHost/Program.cs` | Inject `Identity__Url` env vào catalog-api để issuer match |

#### `Program.cs` Catalog — Policy & Middleware

```csharp
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddApplicationServices();
builder.Services.AddProblemDetails();

builder.AddDefaultAuthentication();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly",
        policy => policy.RequireAuthenticatedUser().RequireRole("admin"));

// ...

app.UseAuthentication();
app.UseAuthorization();
app.MapCatalogApi();
```

#### `CatalogApi.cs` — Bọc Group AdminOnly

```csharp
var adminV1  = v1.MapGroup("").RequireAuthorization("AdminOnly");
var adminV2  = v2.MapGroup("").RequireAuthorization("AdminOnly");
var adminApi = api.MapGroup("").RequireAuthorization("AdminOnly");

adminV1.MapPut("/items", UpdateItemV1)...;
adminV2.MapPut("/items/{id:int}", UpdateItem)...;
adminApi.MapPost("/items", CreateItem)...;
adminApi.MapDelete("/items/{id:int}", DeleteItemById)...;
```

---

## 4. Kết Quả Test 3 Case

### Case 1: Không Token → 401 Unauthorized (PASS)

**Request:**
```http
POST http://localhost:5222/api/catalog/items?api-version=1.0
Content-Type: application/json
{ ... }
```

**Response:**
```http
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401
}
```

→ Authentication middleware không tìm thấy header `Authorization` → reject ngay.

### Case 2: Token User Thường → 403 Forbidden (PASS)

**Request:**
```http
POST http://localhost:5222/api/catalog/items?api-version=1.0
Authorization: Bearer <alice_token>
```

**Response:**
```http
HTTP/1.1 403 Forbidden
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403
}
```

→ Token alice hợp lệ, authentication pass, nhưng claim `role=customer` không thoả `RequireRole("admin")` → authorization reject với 403.

### Case 3: Token Admin → 201 Created (PASS)

**Request:**
```http
POST http://localhost:5222/api/catalog/items?api-version=1.0
Authorization: Bearer <admin_token>
{ "name": "Admin Test Item", "price": 19.99, ... }
```

**Response:**
```http
HTTP/1.1 201 Created
Location: /api/catalog/items/9997
```

→ Token admin có `role=admin` → pass cả authentication lẫn authorization → endpoint CreateItem chạy → trả 201.

---

## 5. Phân Biệt 401 vs 403 (Phần Lý Thuyết Quan Trọng)

| Mã | Tên gọi RFC | Ý nghĩa thực sự | Khi nào trả ra |
|----|-------------|------------------|----------------|
| **401** Unauthorized | "Unauthenticated" (sai tên) | "Tôi không biết bạn là ai" | Không gửi token, token sai chữ ký, expired, audience/issuer không khớp |
| **403** Forbidden | "Authenticated but not allowed" | "Tôi biết bạn là ai, nhưng không cho" | Token hợp lệ nhưng thiếu role/claim/scope mà policy yêu cầu |

**Cách dễ nhớ:** "401 = ai đó? 403 = biết rồi, nhưng không cho."

**Vì sao thiết kế tách 2 mã?**

- Client nhận **401** sẽ redirect user đi đăng nhập lại — vì có thể token đã hết hạn.
- Client nhận **403** sẽ hiển thị "Bạn không đủ quyền, liên hệ admin" — vì đăng nhập lại cũng vô ích, role không thay đổi.
- Trộn lẫn 2 mã sẽ gây bug UX: user đã đăng nhập đúng vẫn bị bắt đăng nhập lại vô hạn.

**Ánh xạ vào pipeline ASP.NET Core:**

```
Request
   │
   ▼
┌────────────────────────────┐
│ Routing + ApiVersioning    │ ──→ thiếu ?api-version → 400 Bad Request
└──────────────┬─────────────┘
               ▼
┌────────────────────────────┐
│ UseAuthentication          │ ──→ thiếu/sai token → 401 Unauthorized
└──────────────┬─────────────┘
               ▼
┌────────────────────────────┐
│ UseAuthorization (policy)  │ ──→ thiếu role/claim → 403 Forbidden
└──────────────┬─────────────┘
               ▼
┌────────────────────────────┐
│ Endpoint Handler           │ ──→ chạy logic → 200/201/4xx
└────────────────────────────┘
```

Bạn không phải code logic phân biệt — framework tự xử dựa trên thứ tự middleware và kết quả của từng tầng.

---

## 6. Nhật Ký Debug — 5 Lỗi Đã Fix Trên Đường Đi

Đây là phần quan trọng nhất của báo cáo. Mỗi lỗi dưới đây đều là vấn đề thực tế gặp khi test, kèm response gốc (paste từ REST Client output), phân tích nguyên nhân và fix.

---

### Lỗi #1: Build Fail Sau Khi Refactor `CatalogApi.cs`

**Triệu chứng:** Khi `dotnet run`, hàng loạt lỗi compile.

```
D:\...\Catalog.API\Apis\CatalogApi.cs(139,13): error CS1519: Invalid token '.' in a member declaration
D:\...\Catalog.API\Apis\CatalogApi.cs(139,30): error CS1001: Identifier expected
D:\...\Catalog.API\Apis\CatalogApi.cs(141,18): error CS1519: Invalid token '(' in a member declaration
... (~50 errors)
```

**Phân tích:** Khi xoá khối `// Routes for modifying catalog items` cũ, formatter của VS Code đã đụng vào file song song và để lại 14 dòng rác sau dấu `}` đóng method. Compiler thấy `.WithDescription` nằm ngoài method nên báo "Invalid token".

```csharp
return app;
}       .WithDescription("Create or replace a catalog item")   // ← rác sau }
        .WithTags("Items");
v2.MapPut("/items/{id:int}", UpdateItem)                         // ← rác
        .WithName("UpdateItem-V2") ...
```

**Fix:** Xoá toàn bộ 14 dòng rác sau dấu `}` đóng method `MapCatalogApi`.

**Bài học:** Khi refactor xoá một khối lớn, đọc lại vài chục dòng quanh vùng vừa sửa để chắc không có rác sót lại — đặc biệt khi formatter chạy nền.

---

### Lỗi #2: Case 1 Trả 400 Bad Request Thay Vì 401 Unauthorized

**Triệu chứng:**

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "type": "https://docs.api-versioning.org/problems#unspecified",
  "title": "Unspecified API version",
  "status": 400,
  "detail": "An API version is required, but was not specified.",
  "code": "ApiVersionUnspecified",
  "traceId": "00-822940734644ebb56c026c0b90c83ef5-2f9fa940cb6cd0ce-01"
}
```

**Phân tích:** API Versioning middleware kiểm tra **trước** Authentication middleware. URL `/api/catalog/items` thiếu phần version → versioning reject với 400 ngay lập tức, request không bao giờ chạm tới authentication để có cơ hội trả 401.

Đây là lý do thiết kế hợp lý của framework: nếu request không xác định được endpoint nào nhận, không có ý nghĩa gì để check auth.

**Fix:** Thêm `?api-version=1.0` vào URL của 3 case POST.

```http
POST {{catalogUrl}}/api/catalog/items?api-version=1.0
```

**Bài học:** Đọc kỹ status code và phân tích middleware nào trả ra. Mỗi mã đến từ một tầng khác nhau:
- 400 → routing/versioning/model binding.
- 401 → authentication.
- 403 → authorization.
- 404 → routing không tìm thấy endpoint.

---

### Lỗi #3: Case 2 Trả 401 Với "Issuer Is Invalid"

**Triệu chứng:** Sau khi fix lỗi #2, bắn token alice vẫn ra 401 thay vì 403.

```http
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer error="invalid_token", 
                  error_description="The issuer 'https://localhost:5243' is invalid"
```

**Phân tích:** JWT Bearer middleware validate `iss` (issuer) trong token so với `Authority` được config. Catalog đang đọc `Authority = "http://identity-api"` (giá trị mặc định trong `appsettings.json`), nhưng token có `iss = "https://localhost:5243"` → mismatch → reject với 401.

Nguyên nhân gốc: Trong `eShop.AppHost/Program.cs`, các service khác như `ordering-api` được inject env var `Identity__Url` để override appsettings, nhưng **catalog-api không có dòng đó**:

```csharp
// Ordering — đúng
var orderingApi = builder.AddProject<Projects.Ordering_API>("ordering-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(orderDb).WaitFor(orderDb)
    .WithEnvironment("Identity__Url", identityEndpoint);   // ← có

// Catalog — thiếu
var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(catalogDb);
    // ← không có Identity__Url
```

**Fix:** Thêm `.WithEnvironment("Identity__Url", identityEndpoint)` cho catalog-api trong AppHost.

```csharp
var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(catalogDb)
    .WithEnvironment("Identity__Url", identityEndpoint);
```

**Bài học:** Aspire dùng convention `__` (double underscore) để map env var sang config key:
- `Identity__Url` → `Identity:Url`
- `Identity__Audience` → `Identity:Audience`

Khi service không pickup config từ AppHost, kiểm 2 chỗ: appsettings.json và `.WithEnvironment()` trong AppHost.

---

### Lỗi #4: Case 3 Trả 403 Dù Token Của Admin Đã Hợp Lệ

**Triệu chứng (lần 1):** Token admin hợp lệ, qua được auth, nhưng vẫn 403.

```http
HTTP/1.1 403 Forbidden
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403
}
```

**Phân tích bước 1 — Decode token trên jwt.io:**

```json
{
  "iss": "https://localhost:5243",
  "scope": ["catalog", "openid", "profile"],
  "client_id": "catalogtest",
  "sub": "a1a7cfcf-433d-4c17-a3ce-e121c37e2c24",
  "preferred_username": "admin",
  "name": "Admin",
  "email": "admin@gmail.com",
  "address_city": "Da Nang",
  ...
  // ❌ KHÔNG có claim "role" ở bất kỳ tên nào
}
```

Token có đầy đủ `name`, `email`, `address`... nhưng thiếu `role`. Kiểm chứng claim đã được seed đúng (vì các property khác đến từ `UsersSeed.cs` xuất hiện đầy đủ) → vấn đề ở bước phát token.

**Nguyên nhân:** eShop có custom `ProfileService` trong `Identity.API/Services/ProfileService.cs`. Method `GetClaimsFromUser` hard-code danh sách claim từ properties của `ApplicationUser` — **không đọc** bảng `AspNetUserClaims` (nơi `AddClaimAsync` lưu claim).

```csharp
// Code cũ — chỉ đọc properties hard-code
var claims = GetClaimsFromUser(user);
context.IssuedClaims = claims.ToList();
```

**Fix:** Sửa `ProfileService.cs` đọc thêm claim từ DB:

```csharp
var claims = GetClaimsFromUser(user).ToList();

// Bổ sung custom claim đã lưu trong AspNetUserClaims
var dbClaims = await _userManager.GetClaimsAsync(user);
claims.AddRange(dbClaims);

context.IssuedClaims = claims;
```

---

### Lỗi #5: Case 3 VẪN Trả 403 Sau Khi Token Đã Có Role

**Triệu chứng (lần 2):** Sau fix lỗi #4, token đã có `"role": "admin"` (verify trên jwt.io), nhưng Case 3 vẫn 403.

```json
{
  ...
  "preferred_username": "admin",
  "role": "admin",          // ← đã có rồi
  "jti": "6675B378C3C0B6DDEEC7937A2C8A35B7"
}
```

**Phân tích — Thêm endpoint `/debug/claims` để xem Catalog đang thấy gì:**

```csharp
app.MapGet("/debug/claims", (HttpContext ctx) => Results.Ok(new {
    IsAuthenticated = ctx.User.Identity?.IsAuthenticated,
    Claims = ctx.User.Claims.Select(c => new { c.Type, c.Value }).ToArray()
})).RequireAuthorization();
```

Kết quả response:

```json
{
  "isAuthenticated": true,
  "authenticationType": "AuthenticationTypes.Federation",
  "claims": [
    { "type": "iss", "value": "https://localhost:5243" },
    { "type": "sub", "value": "a1a7cfcf-433d-4c17-a3ce-e121c37e2c24" },
    { "type": "preferred_username", "value": "admin" },
    { "type": "name", "value": "Admin" },
    ...
    {
      "type": "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
      "value": "admin"
    },
    ...
  ]
}
```

**Tìm ra nguyên nhân thực sự:** Catalog **có thấy claim role** nhưng **dưới dạng URI dài** `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`, không phải tên ngắn `role`.

Lý do: JWT Bearer middleware (cụ thể là `JsonWebTokenHandler`) tự động map claim ngắn `role` trong JWT thành URI dài bên trong `ClaimsIdentity`. Đây là di sản từ thời WS-Federation/SAML.

Policy của mình viết là:
```csharp
.RequireClaim("role", "admin")     // match literal type = "role"
```
→ Không khớp với URI dài → 403.

**Fix:** Đổi `RequireClaim` sang `RequireRole`:

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly",
        policy => policy.RequireAuthenticatedUser().RequireRole("admin"));
```

**Khác biệt:**

| Method | Hoạt động ra sao |
|--------|------------------|
| `RequireClaim("role", "admin")` | So sánh **literal**: tìm claim có Type **đúng bằng** `"role"` → fail vì Type là URI dài |
| `RequireRole("admin")` | Gọi `ClaimsPrincipal.IsInRole(...)` → đọc claim theo `RoleClaimType` của `ClaimsIdentity` (mặc định = URI dài) → pass |

**Bài học:**
1. Khi check role, luôn dùng `RequireRole` thay vì `RequireClaim("role", ...)`.
2. JWT claim mapping là "trap" kinh điển — claim ngắn trong JWT có thể bị middleware map thành URI dài. Tắt mapping bằng `options.MapInboundClaims = false` nếu cần.
3. Khi nghi ngờ claim, **đừng đoán** — viết endpoint `/debug/claims` để xem chính xác middleware đang thấy gì. 5 phút debug visual hơn 30 phút đoán mò.

---

### Tổng Kết Hành Trình Debug

```
Bắt đầu test
   │
   ├─ Lỗi #1: Build fail (rác trong CatalogApi.cs)
   │  └─ Fix: Xoá 14 dòng rác sau }
   │
   ├─ Case 1 → 400 (thiếu api-version)
   │  └─ Lỗi #2 fix: thêm ?api-version=1.0
   │
   ├─ Case 2 → 401 (issuer mismatch)
   │  └─ Lỗi #3 fix: WithEnvironment("Identity__Url", ...)
   │
   ├─ Case 3 → 403 (token thiếu role)
   │  ├─ Lỗi #4 fix: ProfileService đọc GetClaimsAsync
   │  │
   │  └─ Case 3 vẫn → 403 (role bị map URI dài)
   │     └─ Lỗi #5 fix: RequireRole thay vì RequireClaim
   │
   └─ ✅ Pass cả 3 case
```

5 lỗi, 5 lần fix, mỗi lần học được một khía cạnh khác của ASP.NET Core:

| Lỗi | Khía cạnh học được |
|-----|---------------------|
| #1 | Refactor cẩn thận khi formatter chạy song song |
| #2 | Thứ tự middleware quyết định status code nào trả ra |
| #3 | Aspire env injection convention `__` → `:` |
| #4 | IdentityServer custom ProfileService không tự đọc DB claim |
| #5 | JWT claim mapping URI dài vs `RequireRole` vs `RequireClaim` |

---

## 7. Cách Reproduce (Cho Người Đọc)

### 7.1. Prerequisites

- Docker Desktop chạy.
- .NET SDK 10 trên máy.
- Đã clone eShop về `eShop-main/`.
- Cài extension **REST Client** (Huachao Mao) cho VS Code.

### 7.2. Chạy Stack

```cmd
cd eShop-main
dotnet run --project src\eShop.AppHost\eShop.AppHost.csproj
```

Mở Aspire Dashboard tại `http://localhost:19888/...`. Lấy port của `identity-api` và `catalog-api`.

### 7.3. Cập Nhật `Test-AdminOnly.http`

```http
@identityUrl = https://localhost:<IDENTITY_PORT>
@catalogUrl  = http://localhost:<CATALOG_PORT>
```

### 7.4. Bắn Request

Theo thứ tự:

1. Bước 1: `getUserToken` — lấy token alice.
2. Bước 2: `getAdminToken` — lấy token admin.
3. Case 1 → 401.
4. Case 2 → 403.
5. Case 3 → 201.
6. Bonus GET → 200.

---

## 8. Tổng Kết

| Tiêu chí | Trạng thái |
|----------|------------|
| Policy `AdminOnly` áp đúng 4 endpoint mutation | ✅ |
| Case 1 (no token) | ✅ 401 |
| Case 2 (user thường) | ✅ 403 |
| Case 3 (admin) | ✅ 201 |
| Giải thích 401 vs 403 | ✅ Mục 5 |
| Mapping pipeline → status code | ✅ Mục 5 |
| Bài học rút ra | ✅ Mục 6 |

**Kết luận:** Bài 1 đã pass đầy đủ 3 case. Hành trình debug 4 lỗi liên tiếp giúp hiểu sâu pipeline ASP.NET Core, claim mapping JWT, và cơ chế Aspire wire connection string giữa các microservice.
