using System.Text.Json.Nodes;
using eShop.Basket.API.Grpc;
using eShop.WebApp.Services.OrderStatus.IntegrationEvents;
using eShop.WebAppComponents.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.AI;
using Microsoft.IdentityModel.JsonWebTokens;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddAuthenticationServices();

        builder.AddRabbitMqEventBus("EventBus").AddEventBusSubscriptions();

        builder.Services.AddHttpForwarderWithServiceDiscovery();

        // Application services
        builder.Services.AddScoped<BasketState>();
        builder.Services.AddScoped<LogOutService>();
        builder.Services.AddSingleton<BasketService>();
        builder.Services.AddSingleton<OrderStatusNotificationService>();
        builder.Services.AddSingleton<IProductImageUrlProvider, ProductImageUrlProvider>();
        builder.Services.AddSingleton<eShop.WebApp.Services.BasketUpdateNotifier>();
        builder.AddAIServices();

        // HTTP and GRPC client registrations
        var basketGrpcAddress = builder.Configuration["services:basket-api:grpc:0"]
            ?? builder.Configuration["services:basket_api:grpc:0"]
            ?? "http://basket-api:5021";

        if (basketGrpcAddress.StartsWith("grpc://", StringComparison.OrdinalIgnoreCase))
        {
            basketGrpcAddress = "http://" + basketGrpcAddress.Substring(7);
        }
        else if (basketGrpcAddress.StartsWith("grpcs://", StringComparison.OrdinalIgnoreCase))
        {
            basketGrpcAddress = "https://" + basketGrpcAddress.Substring(8);
        }

        builder
            .Services.AddGrpcClient<Basket.BasketClient>(o => o.Address = new(basketGrpcAddress))
            .AddAuthToken();

        builder
            .Services.AddHttpClient<CatalogService>(o =>
                o.BaseAddress = new("http://catalog-api:8080")
            )
            .AddApiVersion(2.0)
            .AddAuthToken();

        builder
            .Services.AddHttpClient<OrderingService>(o =>
                o.BaseAddress = new("http://ordering-api:8080")
            )
            .AddApiVersion(1.0)
            .AddAuthToken();

        builder
            .Services.AddHttpClient<DiscountService>(o =>
                o.BaseAddress = new("http://discount-api:8080")
            )
            .AddAuthToken();
    }

    public static void AddEventBusSubscriptions(this IEventBusBuilder eventBus)
    {
        eventBus.AddSubscription<
            OrderStatusChangedToAwaitingValidationIntegrationEvent,
            OrderStatusChangedToAwaitingValidationIntegrationEventHandler
        >();
        eventBus.AddSubscription<
            OrderStatusChangedToPaidIntegrationEvent,
            OrderStatusChangedToPaidIntegrationEventHandler
        >();
        eventBus.AddSubscription<
            OrderStatusChangedToStockConfirmedIntegrationEvent,
            OrderStatusChangedToStockConfirmedIntegrationEventHandler
        >();
        eventBus.AddSubscription<
            OrderStatusChangedToShippedIntegrationEvent,
            OrderStatusChangedToShippedIntegrationEventHandler
        >();
        eventBus.AddSubscription<
            OrderStatusChangedToCancelledIntegrationEvent,
            OrderStatusChangedToCancelledIntegrationEventHandler
        >();
        eventBus.AddSubscription<
            OrderStatusChangedToSubmittedIntegrationEvent,
            OrderStatusChangedToSubmittedIntegrationEventHandler
        >();
    }

    public static void AddAuthenticationServices(this IHostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var services = builder.Services;

        JsonWebTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");

        var identityUrl = configuration.GetRequiredValue("IdentityUrl");
        var callBackUrl = configuration.GetRequiredValue("CallBackUrl");
        var sessionCookieLifetime = configuration.GetValue("SessionCookieLifetimeMinutes", 60);

        services.AddAuthorization();
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromMinutes(sessionCookieLifetime);
                options.Cookie.SecurePolicy = CookieSecurePolicy.None;
                options.Cookie.SameSite = SameSiteMode.Lax;
            })
            .AddOpenIdConnect(options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.Authority = identityUrl;
                options.SignedOutRedirectUri = callBackUrl;
                options.ClientId = "webapp";
                options.ClientSecret = "secret";
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.RequireHttpsMetadata = false;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("orders");
                options.Scope.Add("basket");
                options.NonceCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.None;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.None;
                options.ResponseMode = "query";
            });

        services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
        services.AddCascadingAuthenticationState();
    }

    private static void AddAIServices(this IHostApplicationBuilder builder)
    {
        ChatClientBuilder? chatClientBuilder = null;
        if (
            builder.Configuration["OllamaEnabled"] is string ollamaEnabled
            && bool.Parse(ollamaEnabled)
        )
        {
            builder.Services.Configure<Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions>(
                "chat",
                options =>
                {
                    options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
                    options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(5);
                }
            );
            chatClientBuilder = builder.AddOllamaApiClient("chat").AddChatClient();
        }
        else if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("chatModel")))
        {
            var connectionString = builder.Configuration.GetConnectionString("chatModel")!;
            var parts = connectionString
                .Split(';')
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

            if (
                parts.TryGetValue("Key", out var apiKey)
                && parts.TryGetValue("Endpoint", out var endpointUri)
            )
            {
                parts.TryGetValue("Deployment", out var modelName);
                modelName ??= "gemini-2.5-flash";

                var clientOptions = new global::OpenAI.OpenAIClientOptions
                {
                    Endpoint = new Uri(endpointUri),
                };
                if (apiKey.StartsWith("AQ.", StringComparison.OrdinalIgnoreCase))
                {
                    clientOptions.AddPolicy(
                        new CustomHeaderPolicy(apiKey),
                        System.ClientModel.Primitives.PipelinePosition.PerCall
                    );
                }

                var openAIClient = new global::OpenAI.OpenAIClient(
                    new System.ClientModel.ApiKeyCredential(apiKey),
                    clientOptions
                );

                builder.Services.AddSingleton(openAIClient);
                chatClientBuilder = builder.Services.AddChatClient(sp =>
                    openAIClient.GetChatClient(modelName).AsIChatClient()
                );
            }
            else
            {
                chatClientBuilder = builder
                    .AddOpenAIClientFromConfiguration("chatModel")
                    .AddChatClient();
            }
        }

        // ✅ BỎ UseFunctionInvocation() — FunctionInvokingChatClient không tương thích
        // với Groq API khi build tool result messages nội bộ → HTTP 400 tool_use_failed.
        // Tool calling được xử lý thủ công trong ChatState.AddUserMessageAsync.
        // chatClientBuilder?.UseFunctionInvocation();
    }

    public static async Task<string?> GetBuyerIdAsync(
        this AuthenticationStateProvider authenticationStateProvider
    )
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        return user.FindFirst("sub")?.Value;
    }

    public static async Task<string?> GetUserNameAsync(
        this AuthenticationStateProvider authenticationStateProvider
    )
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        return user.FindFirst("name")?.Value;
    }

    private class CustomHeaderPolicy : System.ClientModel.Primitives.PipelinePolicy
    {
        private readonly string _apiKey;

        public CustomHeaderPolicy(string apiKey)
        {
            _apiKey = apiKey;
        }

        private void ModifyRequestContent(System.ClientModel.Primitives.PipelineMessage message)
        {
            if (message.Request.Content == null)
                return;
            try
            {
                using var ms = new System.IO.MemoryStream();
                message.Request.Content.WriteTo(ms, default);
                var bytes = ms.ToArray();
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                var root = JsonNode.Parse(json);
                if (
                    root is JsonObject obj
                    && obj.TryGetPropertyValue("messages", out var messagesNode)
                    && messagesNode is JsonArray messagesArray
                )
                {
                    bool modified = false;
                    foreach (var messageNode in messagesArray)
                    {
                        if (messageNode is JsonObject msgObj)
                        {
                            if (
                                msgObj.TryGetPropertyValue("role", out var roleVal)
                                && roleVal?.ToString() == "assistant"
                            )
                            {
                                if (
                                    msgObj.TryGetPropertyValue("tool_calls", out var toolCalls)
                                    && toolCalls is JsonArray tCalls
                                    && tCalls.Count > 0
                                )
                                {
                                    if (!msgObj.ContainsKey("thought_signature"))
                                    {
                                        msgObj["thought_signature"] =
                                            "skip_thought_signature_validator";
                                        modified = true;
                                    }
                                }
                            }
                        }
                    }
                    if (modified)
                    {
                        var modifiedJson = root.ToJsonString();
                        message.Request.Content = System.ClientModel.BinaryContent.Create(
                            System.BinaryData.FromString(modifiedJson)
                        );
                    }
                }
            }
            catch { }
        }

        private async System.Threading.Tasks.ValueTask ModifyRequestContentAsync(
            System.ClientModel.Primitives.PipelineMessage message,
            System.Threading.CancellationToken cancellationToken
        )
        {
            if (message.Request.Content == null)
                return;
            try
            {
                using var ms = new System.IO.MemoryStream();
                await message
                    .Request.Content.WriteToAsync(ms, cancellationToken)
                    .ConfigureAwait(false);
                var bytes = ms.ToArray();
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                var root = JsonNode.Parse(json);
                if (
                    root is JsonObject obj
                    && obj.TryGetPropertyValue("messages", out var messagesNode)
                    && messagesNode is JsonArray messagesArray
                )
                {
                    bool modified = false;
                    foreach (var messageNode in messagesArray)
                    {
                        if (messageNode is JsonObject msgObj)
                        {
                            if (
                                msgObj.TryGetPropertyValue("role", out var roleVal)
                                && roleVal?.ToString() == "assistant"
                            )
                            {
                                if (
                                    msgObj.TryGetPropertyValue("tool_calls", out var toolCalls)
                                    && toolCalls is JsonArray tCalls
                                    && tCalls.Count > 0
                                )
                                {
                                    if (!msgObj.ContainsKey("thought_signature"))
                                    {
                                        msgObj["thought_signature"] =
                                            "skip_thought_signature_validator";
                                        modified = true;
                                    }
                                }
                            }
                        }
                    }
                    if (modified)
                    {
                        var modifiedJson = root.ToJsonString();
                        message.Request.Content = System.ClientModel.BinaryContent.Create(
                            System.BinaryData.FromString(modifiedJson)
                        );
                    }
                }
            }
            catch { }
        }

        public override void Process(
            System.ClientModel.Primitives.PipelineMessage message,
            System.Collections.Generic.IReadOnlyList<System.ClientModel.Primitives.PipelinePolicy> pipeline,
            int currentIndex
        )
        {
            message.Request.Headers.Set("x-goog-api-key", _apiKey);
            message.Request.Headers.Remove("Authorization");
            ModifyRequestContent(message);
            ProcessNext(message, pipeline, currentIndex);
        }

        public override async System.Threading.Tasks.ValueTask ProcessAsync(
            System.ClientModel.Primitives.PipelineMessage message,
            System.Collections.Generic.IReadOnlyList<System.ClientModel.Primitives.PipelinePolicy> pipeline,
            int currentIndex
        )
        {
            message.Request.Headers.Set("x-goog-api-key", _apiKey);
            message.Request.Headers.Remove("Authorization");
            await ModifyRequestContentAsync(message, message.CancellationToken)
                .ConfigureAwait(false);
            await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
        }
    }
}
