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

        // REVIEW: This is done for development ease but shouldn't be here in production
        builder.Services.AddMigration<CatalogContext, CatalogContextSeed>();

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
            "cosmos",
            configureClientOptions: clientOptions =>
            {
                //Config bypass check SSL Certificate when run with Local by Emulator
                clientOptions.HttpClientFactory = () =>
                {
                    var httpHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                    };
                    return new HttpClient(httpHandler);
                };

                //Gateway mode is required to ensure stable communication through the Docker Bridge network on Windows.
                clientOptions.ConnectionMode = ConnectionMode.Gateway;
            }
        );
        builder.Services.AddOptions<CatalogOptions>().BindConfiguration(nameof(CatalogOptions));

        if (
            builder.Configuration["OllamaEnabled"] is string ollamaEnabled
            && bool.Parse(ollamaEnabled)
        )
        {
            builder.AddOllamaApiClient("embedding").AddEmbeddingGenerator();
        }
        else if (
            !string.IsNullOrWhiteSpace(
                builder.Configuration.GetConnectionString("textEmbeddingModel")
            )
        )
        {
            builder.AddOpenAIClientFromConfiguration("textEmbeddingModel").AddEmbeddingGenerator();
        }

        builder.Services.AddScoped<ICatalogAI, CatalogAI>();
        builder.Services.AddSingleton<IChatMemoryService, ChatMemoryService>();
    }
}
