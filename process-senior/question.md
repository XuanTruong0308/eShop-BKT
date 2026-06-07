gân Hàng 100 Câu Hỏi — Lộ Trình Senior .NET
100 câu hỏi được phân nhóm rõ ràng, tăng dần độ khó.
Học theo thứ tự nhóm → mở `dotnet/eshop` xem code thực tế → tự trả lời câu hỏi.
Cách này giúp bạn nắm toàn bộ hệ sinh thái và sẵn sàng phỏng vấn Senior.
🟦 Nhóm 1: .NET & C# Fundamentals (Câu 1–10)
# Câu hỏi
Q1 .NET Framework, .NET Core, .NET 5+ và .NET
10 khác nhau như thế nào?
Q2 CLR, CTS, CLS là gì? Vai trò của chúng trong
.NET runtime?
Q3 Assembly, Metadata, Manifest là gì? Cách
.NET load assembly?
Q4 Garbage Collector (GC) hoạt động ra sao? Có
mấy Generation?
Q5 Value Type và Reference Type khác nhau ở
đâu? Khi nào dùng cái nào?
Q6 Boxing/Unboxing là gì? Tại sao nó ảnh hưởng
performance?
Q7 C# có mấy kiểu String? String vs StringBuilder
vs ReadOnlySpan\<char\>?
Q8 Async/Await hoạt động như thế nào dưới
hood? Task vs ValueTask?
Q9 Dependency Injection (DI) trong .NET là gì?
Built-in DI container có giới hạn gì?
Q10 .NET 10 có gì mới so với .NET 8 về SDK, CLI và
file-based apps?
🟢 Nhóm 2: Advanced C# & .NET 10 Features (Câu 11–22)
# Câu hỏiQ11 C# 14 có những tính năng mới nào quan trọng
nhất?
Q12 field keyword trong C# 14 dùng để làm gì? Ví
dụ thực tế?
Q13 Extension Members (extension
properties/methods) trong C# 14 hoạt động ra
sao?
Q14 Null-Conditional Assignment (??=) và các cải
tiến null-safety mới?
Q15 Partial constructors, partial events trong C# 14
dùng khi nào?
Q16 Lambda parameters với modifiers (ref, in,
out) có lợi ích gì?
Q17 Span<T>, ReadOnlySpan<T>, Memory<T> dùng
để tối ưu gì?
Q18 Source Generators trong C# hoạt động như
thế nào? Ưu nhược điểm?
Q19 Records, init-only properties, with-expression
dùng trong DDD?
Q20 Pattern matching nâng cao trong C# 14 (list
patterns, relational patterns…)?
Q21 Native AOT trong .NET 10 cải thiện những gì
so với trước?
Q22 File-based apps (single .cs file) trong .NET 10
dùng để làm gì thực tế?
🟡 Nhóm 3: .NET Runtime Internals & Performance (Câu 23–32)
# Câu hỏi
Q23 JIT Compiler trong .NET 10 có những tối ưu
mới nào (inlining, devirtualization…)?
Q24 AVX10.2, Arm64 SVE hỗ trợ như thế nào trong
.NET 10?
Q25 GC modes (Workstation vs Server vs
Background) dùng khi nào?
Q26 Memory diagnostics: dotnet-counters,
dotnet-trace, dotnet-dump dùng ra sao?
Q27 BenchmarkDotNet dùng để đo performance
như thế nào trong thực tế?Q28 Stackalloc, ArrayPool, MemoryPool dùng để
giảm GC pressure?
Q29 Physical promotion và escape analysis trong
.NET 10 runtime?
Q30 Hot Path optimization: loop inversion, bounds
check elimination?
Q31 Cách debug memory leak trong production
.NET 10 app?
Q32 Performance improvements của .NET 10 so
với .NET 8 (có số liệu cụ thể)?
🟠 Nhóm 4: ASP.NET Core 10 & Web/API Development (Câu 33–44)
# Câu hỏi
Q33 Minimal APIs trong ASP.NET Core 10 có những
cải tiến gì (validation built-in)?
Q34 OpenAPI 3.1 support mặc định trong ASP.NET
Core 10?
Q35 YARP (Yet Another Reverse Proxy) dùng làm
API Gateway như thế nào?
Q36 Middleware pipeline hoạt động ra sao? Thứ tự
execution?
Q37 Health Checks, Rate Limiting, Request
Decompression trong production?
Q38 gRPC vs REST vs GraphQL: khi nào dùng cái
nào trong microservices?
Q39 SignalR và Server-Sent Events (SSE) mới trong
ASP.NET Core 10?
Q40 Blazor 10 có những cải tiến nào
(WebAssembly preloading, Hot Reload…)?
Q41 Authentication & Authorization trong ASP.NET
Core 10 (Entra ID, passkey)?
Q42 Problem Details (RFC 9457) và exception
handling hiện đại?
Q43 Cách implement versioning cho API trong
ASP.NET Core 10?
Q44 Native AOT cho ASP.NET Core app có khả thi
không? Trade-off?🔴 Nhóm 5: Entity Framework Core 10 (Câu 45–54)
# Câu hỏi
Q45 EF Core 10 có những tính năng mới quan
trọng nhất?
Q46 Complex Types (owned entities) và JSON
mapping trong EF Core 10?
Q47 LeftJoin / RightJoin operator trong LINQ
của EF Core 10?
Q48 ExecuteUpdate / ExecuteDelete cho JSON
columns?
Q49 Named Query Filters và cách disable selective?
Q50 Split queries vs Single query: khi nào dùng cái
nào?
Q51 Dapper + EF Core hybrid dùng khi nào để tối
ưu performance?
Q52 Database-per-service pattern trong
microservices?
Q53 Vector search (AI/RAG) trong EF Core 10 với
Cosmos DB?
Q54 Cách migrate EF Core 8 → EF Core 10 trong dự
án lớn?
🟣 Nhóm 6: Architecture Patterns (Câu 55–64)
# Câu hỏi
Q55 Clean Architecture, Vertical Slice Architecture
và Onion Architecture khác nhau?
Q56 DDD (Domain-Driven Design): Entity, Value
Object, Aggregate, Repository?
Q57 CQRS + MediatR: Command/Query separation
lợi ích gì?
Q58 Event Sourcing + Outbox Pattern dùng để làm
gì?
Q59 Saga Pattern cho distributed transaction trong
microservices?
Q60 Vertical Slice Architecture trong eShop được
implement như thế nào?Q61 Trade-off giữa Monolith và Microservices?
Q62 Feature folders vs Traditional layered
structure?
Q63 Domain Events handling trong .NET (MediatR
notifications)?
Q64 Cách áp dụng SOLID principles trong realworld .NET project?
🔷 Nhóm 7: Microservices & .NET Aspire 13 (Câu 65–79) — Tập trung eShop
# Câu hỏi
Q65 .NET Aspire 13.2 là gì? Khác với Docker
Compose/Kubernetes ra sao?
Q66 AppHost project trong Aspire làm gì? Cách
orchestrate services?
Q67 Resource types trong Aspire (Project,
Container, Executable…)?
Q68 Service discovery, connection string wiring
trong Aspire?
Q69 Resilience (Polly + Aspire) và Health Checks
trong eShop?
Q70 Observability stack trong Aspire
(OpenTelemetry, dashboard)?
Q71 Cách deploy Aspire app lên Azure Container
Apps?
Q72 TypeScript AppHost (preview) trong Aspire
13.2 dùng khi nào?
Q73 AI-native CLI trong Aspire 13.2 là gì?
Q74 Trong eShop, Catalog Service, Ordering
Service, Basket Service giao tiếp nhau bằng gì?
Q75 Event Bus (RabbitMQ/Dapr) trong eShop
implement như thế nào?
Q76 Database-per-service trong eShop dùng EF
Core ra sao?
Q77 API Gateway trong eShop (YARP) cấu hình thế
nào?
Q78 Background Services / Hangfire / Quartz trong
microservices?
Q79 Migration từ monolith sang microservices vớiAspire có bước nào?
☁️ Nhóm 8: Cloud & Azure Integration (Câu 80–87)
# Câu hỏi
Q80 .NET Aspire + Azure Container Apps / AKS khác
nhau?
Q81 Azure Key Vault, Entra ID, Managed Identity
trong .NET 10?
Q82 Cosmos DB, Azure SQL, Redis integration với
Aspire?
Q83 Azure Service Bus vs RabbitMQ vs Azure Event
Grid?
Q84 Bicep / Terraform + Aspire deployment?
Q85 Environment-specific configuration trong
Aspire (dev/staging/prod)?
Q86 Scaling strategies cho .NET microservices trên
Azure?
Q87 Cost optimization khi dùng Aspire + Azure?
🛡️ Nhóm 9: Security, Observability & Resilience (Câu 88–95)
# Câu hỏi
Q88 OWASP Top 10 cho .NET 10 app và cách
mitigate?
Q89 Rate limiting, CORS, Anti-forgery, Data
Protection?
Q90 OpenTelemetry + Application Insights +
Grafana trong Aspire?
Q91 Circuit Breaker, Retry, Timeout policy với
Polly?
Q92 Logging best practices (Serilog + structured
logging)?
Q93 Security scanning (NuGet audit, Trivy) trong
CI/CD?
Q94 AuthN/AuthZ hiện đại (passkey, OAuth2,
OpenID Connect)?Q95 Zero-trust architecture trong .NET
microservices?
⚙️ Nhóm 10: DevOps, Testing & Production Best Practices (Câu 96–100) —
Senior
# Câu hỏi
Q96 CI/CD pipeline cho .NET Aspire app (GitHub
Actions/Azure DevOps)?
Q97 xUnit + Testcontainers + Integration testing
trong eShop?
Q98 Load testing, chaos engineering cho
microservices?
Q99 Monitoring metrics quan trọng nhất (Golden
Signals)?
Q100 Là Senior .NET, bạn migrate dự án .NET 8 sang
.NET 10 + Aspire 13 như thế nào? (kế hoạch
chi tiết)