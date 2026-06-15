using eShop.Basket.API.Grpc;
using eShop.WebApp.Services.OrderStatus.IntegrationEvents;
using eShop.WebAppComponents.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.AI;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Text.Json.Nodes;

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
        builder
            .Services.AddGrpcClient<Basket.BasketClient>(o => o.Address = new("http://basket-api"))
            .AddAuthToken();
        
        builder
            .Services.AddHttpClient<CatalogService>(o =>
                o.BaseAddress = new("https+http://catalog-api")
            )
            .AddApiVersion(2.0)
            .AddAuthToken();

        builder
            .Services.AddHttpClient<OrderingService>(o =>
                o.BaseAddress = new("https+http://ordering-api")
            )
            .AddApiVersion(1.0)
            .AddAuthToken();

        // Register DiscountService to Webapp Service Container
        builder
            .Services.AddHttpClient<DiscountService>(o =>
                o.BaseAddress = new("http://discount-api")
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

        // Add Authentication services
        services.AddAuthorization();
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
                options.ExpireTimeSpan = TimeSpan.FromMinutes(sessionCookieLifetime)
            )
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
            });

        // Blazor auth services
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
            builder.Services.Configure<Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions>("chat", options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
                options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(5);
            });
            chatClientBuilder = builder.AddOllamaApiClient("chat").AddChatClient();
        }
        else if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("chatModel")))
        {
            var connectionString = builder.Configuration.GetConnectionString("chatModel")!;
            var parts = connectionString.Split(';')
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

            if (parts.TryGetValue("Key", out var apiKey) && parts.TryGetValue("Endpoint", out var endpointUri))
            {
                parts.TryGetValue("Deployment", out var modelName);
                modelName ??= "gemini-2.5-flash";

                var clientOptions = new global::OpenAI.OpenAIClientOptions { Endpoint = new Uri(endpointUri) };
                clientOptions.AddPolicy(new LoggingPolicy(), System.ClientModel.Primitives.PipelinePosition.PerCall);
                if (apiKey.StartsWith("AQ.", StringComparison.OrdinalIgnoreCase))
                {
                    clientOptions.AddPolicy(new CustomHeaderPolicy(apiKey), System.ClientModel.Primitives.PipelinePosition.PerCall);
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

        chatClientBuilder?.UseFunctionInvocation();
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
            if (message.Request.Content == null) return;

            try
            {
                using var ms = new System.IO.MemoryStream();
                message.Request.Content.WriteTo(ms, default);
                var bytes = ms.ToArray();
                var json = System.Text.Encoding.UTF8.GetString(bytes);

                var root = JsonNode.Parse(json);
                if (root is JsonObject obj && 
                    obj.TryGetPropertyValue("messages", out var messagesNode) && 
                    messagesNode is JsonArray messagesArray)
                {
                    bool modified = false;
                    foreach (var messageNode in messagesArray)
                    {
                        if (messageNode is JsonObject msgObj)
                        {
                            if (msgObj.TryGetPropertyValue("role", out var roleVal) && roleVal?.ToString() == "assistant")
                            {
                                if (msgObj.TryGetPropertyValue("tool_calls", out var toolCalls) && 
                                    toolCalls is JsonArray tCalls && tCalls.Count > 0)
                                {
                                    if (!msgObj.ContainsKey("thought_signature"))
                                    {
                                        msgObj["thought_signature"] = "skip_thought_signature_validator";
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
            catch
            {
                // Ignore parsing errors to prevent crashing the request pipeline
            }
        }

        private async System.Threading.Tasks.ValueTask ModifyRequestContentAsync(System.ClientModel.Primitives.PipelineMessage message, System.Threading.CancellationToken cancellationToken)
        {
            if (message.Request.Content == null) return;

            try
            {
                using var ms = new System.IO.MemoryStream();
                await message.Request.Content.WriteToAsync(ms, cancellationToken).ConfigureAwait(false);
                var bytes = ms.ToArray();
                var json = System.Text.Encoding.UTF8.GetString(bytes);

                var root = JsonNode.Parse(json);
                if (root is JsonObject obj && 
                    obj.TryGetPropertyValue("messages", out var messagesNode) && 
                    messagesNode is JsonArray messagesArray)
                {
                    bool modified = false;
                    foreach (var messageNode in messagesArray)
                    {
                        if (messageNode is JsonObject msgObj)
                        {
                            if (msgObj.TryGetPropertyValue("role", out var roleVal) && roleVal?.ToString() == "assistant")
                            {
                                if (msgObj.TryGetPropertyValue("tool_calls", out var toolCalls) && 
                                    toolCalls is JsonArray tCalls && tCalls.Count > 0)
                                {
                                    if (!msgObj.ContainsKey("thought_signature"))
                                    {
                                        msgObj["thought_signature"] = "skip_thought_signature_validator";
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
            catch
            {
                // Ignore parsing errors
            }
        }

        public override void Process(System.ClientModel.Primitives.PipelineMessage message, System.Collections.Generic.IReadOnlyList<System.ClientModel.Primitives.PipelinePolicy> pipeline, int currentIndex)
        {
            message.Request.Headers.Set("x-goog-api-key", _apiKey);
            message.Request.Headers.Remove("Authorization");
            ModifyRequestContent(message);
            ProcessNext(message, pipeline, currentIndex);
        }

        public override async System.Threading.Tasks.ValueTask ProcessAsync(System.ClientModel.Primitives.PipelineMessage message, System.Collections.Generic.IReadOnlyList<System.ClientModel.Primitives.PipelinePolicy> pipeline, int currentIndex)
        {
            message.Request.Headers.Set("x-goog-api-key", _apiKey);
            message.Request.Headers.Remove("Authorization");
            await ModifyRequestContentAsync(message, message.CancellationToken).ConfigureAwait(false);
            await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
        }
    }

    private class LoggingPolicy : System.ClientModel.Primitives.PipelinePolicy
    {
        public override void Process(System.ClientModel.Primitives.PipelineMessage message, System.Collections.Generic.IReadOnlyList<System.ClientModel.Primitives.PipelinePolicy> pipeline, int currentIndex)
        {
            LogRequest(message);
            ProcessNext(message, pipeline, currentIndex);
            LogResponse(message);
        }

        public override async System.Threading.Tasks.ValueTask ProcessAsync(System.ClientModel.Primitives.PipelineMessage message, System.Collections.Generic.IReadOnlyList<System.ClientModel.Primitives.PipelinePolicy> pipeline, int currentIndex)
        {
            await LogRequestAsync(message).ConfigureAwait(false);
            await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
            await LogResponseAsync(message).ConfigureAwait(false);
        }

        private void LogRequest(System.ClientModel.Primitives.PipelineMessage message)
        {
            if (message.Request.Content == null) return;
            try
            {
                using var ms = new System.IO.MemoryStream();
                message.Request.Content.WriteTo(ms, default);
                var json = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine("=== GROQ HTTP REQUEST ===");
                Console.WriteLine(json);
            }
            catch {}
        }

        private async System.Threading.Tasks.ValueTask LogRequestAsync(System.ClientModel.Primitives.PipelineMessage message)
        {
            if (message.Request.Content == null) return;
            try
            {
                using var ms = new System.IO.MemoryStream();
                await message.Request.Content.WriteToAsync(ms, default).ConfigureAwait(false);
                var json = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                Console.WriteLine("=== GROQ HTTP REQUEST ===");
                Console.WriteLine(json);
            }
            catch {}
        }

        private void LogResponse(System.ClientModel.Primitives.PipelineMessage message)
        {
            if (message.Response == null) return;
            try
            {
                using var ms = new System.IO.MemoryStream();
                message.Response.ContentStream?.CopyTo(ms);
                var bytes = ms.ToArray();
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                Console.WriteLine("=== GROQ HTTP RESPONSE ===");
                Console.WriteLine(json);
                message.Response.ContentStream = new System.IO.MemoryStream(bytes);
            }
            catch {}
        }

        private async System.Threading.Tasks.ValueTask LogResponseAsync(System.ClientModel.Primitives.PipelineMessage message)
        {
            if (message.Response == null) return;
            try
            {
                using var ms = new System.IO.MemoryStream();
                if (message.Response.ContentStream != null)
                {
                    await message.Response.ContentStream.CopyToAsync(ms).ConfigureAwait(false);
                }
                var bytes = ms.ToArray();
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                Console.WriteLine("=== GROQ HTTP RESPONSE ===");
                Console.WriteLine(json);
                message.Response.ContentStream = new System.IO.MemoryStream(bytes);
            }
            catch {}
        }
    }
}
