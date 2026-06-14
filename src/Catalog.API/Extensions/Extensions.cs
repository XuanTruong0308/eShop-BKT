using System.Linq;
using Microsoft.Extensions.AI;
using eShop.Catalog.API.Services;
using Microsoft.Azure.Cosmos;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        // Avoid loading full database config and migrations if startup
        // is being invoked from build-time OpenAPI generation
        if (builder.Environment.IsBuild())
        {
            builder.Services.AddDbContext<CatalogContext>();
            return;
        }

        builder.AddNpgsqlDbContext<CatalogContext>(
            "catalogdb",
            configureDbContextOptions: dbContextOptionsBuilder =>
            {
                dbContextOptionsBuilder.UseNpgsql(builder =>
                {
                    builder.UseVector();
                });
            }
        );

        builder.Services.AddMigration<CatalogContext, CatalogContextSeed>();
        builder.Services.AddHostedService<CatalogEmbeddingBackfiller>();

        // Add the integration services that consume the DbContext
        builder.Services.AddTransient<
            IIntegrationEventLogService,
            IntegrationEventLogService<CatalogContext>
        >();

        builder.Services.AddTransient<
            ICatalogIntegrationEventService,
            CatalogIntegrationEventService
        >();

        builder
            .AddRabbitMqEventBus("eventbus")
            .AddSubscription<
                OrderStatusChangedToAwaitingValidationIntegrationEvent,
                OrderStatusChangedToAwaitingValidationIntegrationEventHandler
            >()
            .AddSubscription<
                OrderStatusChangedToPaidIntegrationEvent,
                OrderStatusChangedToPaidIntegrationEventHandler
            >();

        //Register Cosmos DB Client(connect with cosmos database)
        builder.AddAzureCosmosClient(
            "chatdb",
            configureClientOptions: clientOptions =>
            {
                //Config bypass check SSL Certificate when run with Local by Emulator
                clientOptions.HttpClientFactory = () =>
                {
                    var httpHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };
                    return new HttpClient(httpHandler);
                };
                clientOptions.ConnectionMode = ConnectionMode.Gateway;
            }
        );
        builder.Services.AddOptions<CatalogOptions>().BindConfiguration(nameof(CatalogOptions));

        if (
            builder.Configuration["OllamaEnabled"] is string ollamaEnabled
            && bool.Parse(ollamaEnabled)
        )
        {
            builder.Services.Configure<Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions>("embedding", options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
                options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(5);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(10);
            });
            builder.AddOllamaApiClient("embedding").AddEmbeddingGenerator();
        }
        else if (
            !string.IsNullOrWhiteSpace(
                builder.Configuration.GetConnectionString("textEmbeddingModel")
            )
        )
        {
            var connectionString = builder.Configuration.GetConnectionString("textEmbeddingModel")!;
            var parts = connectionString.Split(';')
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

            if (parts.TryGetValue("Key", out var apiKey) && parts.TryGetValue("Endpoint", out var endpointUri))
            {
                parts.TryGetValue("Deployment", out var modelName);
                modelName ??= "text-embedding-004";

                var clientOptions = new global::OpenAI.OpenAIClientOptions { Endpoint = new Uri(endpointUri) };
                if (apiKey.StartsWith("AQ.", StringComparison.OrdinalIgnoreCase))
                {
                    clientOptions.AddPolicy(new CustomHeaderPolicy(apiKey), System.ClientModel.Primitives.PipelinePosition.PerCall);
                }

                var openAIClient = new global::OpenAI.OpenAIClient(
                    new System.ClientModel.ApiKeyCredential(apiKey),
                    clientOptions
                );

                builder.Services.AddSingleton(openAIClient);
                builder.Services.AddEmbeddingGenerator(sp =>
                    openAIClient.GetEmbeddingClient(modelName).AsIEmbeddingGenerator()
                );
            }
            else
            {
                builder.AddOpenAIClientFromConfiguration("textEmbeddingModel").AddEmbeddingGenerator();
            }
        }

        builder.Services.AddScoped<ICatalogAI, CatalogAI>();
        builder.Services.AddSingleton<IChatMemoryService, ChatMemoryService>();
    }

    private class CustomHeaderPolicy : System.ClientModel.Primitives.PipelinePolicy
    {
        private readonly string _apiKey;

        public CustomHeaderPolicy(string apiKey)
        {
            _apiKey = apiKey;
        }

        public override void Process(System.ClientModel.Primitives.PipelineMessage message, System.Collections.Generic.IReadOnlyList<System.ClientModel.Primitives.PipelinePolicy> pipeline, int currentIndex)
        {
            message.Request.Headers.Set("x-goog-api-key", _apiKey);
            message.Request.Headers.Remove("Authorization");
            ProcessNext(message, pipeline, currentIndex);
        }

        public override async System.Threading.Tasks.ValueTask ProcessAsync(System.ClientModel.Primitives.PipelineMessage message, System.Collections.Generic.IReadOnlyList<System.ClientModel.Primitives.PipelinePolicy> pipeline, int currentIndex)
        {
            message.Request.Headers.Set("x-goog-api-key", _apiKey);
            message.Request.Headers.Remove("Authorization");
            await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
        }
    }
}
