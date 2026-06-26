using eShop.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var jaeger = builder
    .AddContainer("jaeger", "jaegertracing/all-in-one")
    .WithEndpoint(port: 4317, targetPort: 4317, name: "otlp")
    .WithEndpoint(port: 16686, targetPort: 16686, name: "ui");

builder.AddForwardedHeaders();

var redis = builder.AddRedis("redis");
var rabbitMq = builder.AddRabbitMQ("eventbus").WithLifetime(ContainerLifetime.Persistent);
var postgres = builder
    .AddPostgres("postgres")
    .WithImage("ankane/pgvector")
    .WithImageTag("latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume() // Lưu trữ dữ liệu cơ sở dữ liệu vĩnh viễn trên ổ đĩa cứng thông qua Docker Volume
    .WithPgAdmin(); // pgAdmin web UI để xem và quản lý database

var catalogDb = postgres.AddDatabase("catalogdb");
var identityDb = postgres.AddDatabase("identitydb");
var orderDb = postgres.AddDatabase("orderingdb");
var webhooksDb = postgres.AddDatabase("webhooksdb");
var discountDb = postgres.AddDatabase("discountdb");
var cosmos = builder.AddAzureCosmosDB("cosmos").RunAsEmulator();
var chatDb = cosmos.AddCosmosDatabase("chatdb");

var launchProfileName = ShouldUseHttpForEndpoints() ? "http" : "https";

var discountApi = builder
    .AddProject<Projects.Discount_API>("discount-api")
    .WithReference(discountDb) //Connect DB
    .WithReference(redis) //Connect cache
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq);

// Services
var identityApi = builder
    .AddProject<Projects.Identity_API>("identity-api", launchProfileName)
    .WithExternalHttpEndpoints()
    .WithReference(identityDb)
    .WithHttpHealthCheck("/health");

var identityEndpoint = identityApi.GetEndpoint(launchProfileName);

var basketApi = builder
    .AddProject<Projects.Basket_API>("basket-api")
    .WithEndpoint(targetPort: 5021, name: "grpc", scheme: "grpc")
    .WithReference(redis)
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq)
    .WithEnvironment("Identity__Url", identityEndpoint);
redis.WithParentRelationship(basketApi);

var catalogApi = builder
    .AddProject<Projects.Catalog_API>("catalog-api")
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq)
    .WithReference(catalogDb)
    .WithReference(chatDb)
    .WithEnvironment("Identity__Url", identityEndpoint);

var orderingApi = builder
    .AddProject<Projects.Ordering_API>("ordering-api")
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq)
    .WithReference(orderDb)
    .WaitFor(orderDb)
    .WithHttpHealthCheck("/health")
    .WithEnvironment("Identity__Url", identityEndpoint)
    .WithReference(discountApi);

builder
    .AddProject<Projects.OrderProcessor>("order-processor")
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq)
    .WithReference(orderDb)
    .WaitFor(orderingApi); // wait for the orderingApi to be ready because that contains the EF migrations

builder
    .AddProject<Projects.PaymentProcessor>("payment-processor")
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq);

var webHooksApi = builder
    .AddProject<Projects.Webhooks_API>("webhooks-api")
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq)
    .WithReference(webhooksDb)
    .WithEnvironment("Identity__Url", identityEndpoint);

// Reverse proxies
var mobileBff = builder.AddYarp("mobile-bff");
if (builder.ExecutionContext.IsPublishMode)
{
    mobileBff.WithEndpoint("http", _ => { });
}
else
{
    mobileBff.WithEndpoint("http", endpoint => endpoint.Port = 5222);
}
mobileBff.WithExternalHttpEndpoints()
    .ConfigureMobileBffRoutes(catalogApi, orderingApi, identityApi, basketApi);

// Apps
var webhooksClient = builder
    .AddProject<Projects.WebhookClient>("webhooksclient", launchProfileName)
    .WithReference(webHooksApi)
    .WithEnvironment("IdentityUrl", identityEndpoint);

var webApp = builder
    .AddProject<Projects.WebApp>("webapp", launchProfileName)
    .WithExternalHttpEndpoints()
    .WithUrls(c =>
        c.Urls.ForEach(u => u.DisplayText = $"Online Store ({u.Endpoint?.EndpointName})")
    )
    .WithReference(basketApi)
    .WithReference(catalogApi)
    .WithReference(orderingApi)
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq)
    .WaitFor(identityApi)
    .WithEnvironment("IdentityUrl", identityEndpoint)
    .WithReference(discountApi);

// set to true if you want to use OpenAI
bool useOpenAI = true;
if (useOpenAI)
{
    builder.AddOpenAI(catalogApi, webApp, OpenAITarget.AzureOpenAIExistingWithKey); // Set to AzureOpenAIExistingWithKey to support custom endpoint + key (Gemini)
}

bool useOllama = true;
if (useOllama)
{
    builder.AddOllama(catalogApi, webApp);
}

// Wire up the callback urls (self referencing)
webApp.WithEnvironment("CallBackUrl", webApp.GetEndpoint(launchProfileName));
webhooksClient.WithEnvironment("CallBackUrl", webhooksClient.GetEndpoint(launchProfileName));

// Identity has a reference to all of the apps for callback urls, this is a cyclic reference
identityApi
    .WithEnvironment("BasketApiClient", basketApi.GetEndpoint("http"))
    .WithEnvironment("OrderingApiClient", orderingApi.GetEndpoint("http"))
    .WithEnvironment("WebhooksApiClient", webHooksApi.GetEndpoint("http"))
    .WithEnvironment("WebhooksWebClient", webhooksClient.GetEndpoint(launchProfileName))
    .WithEnvironment("WebAppClient", webApp.GetEndpoint(launchProfileName));

builder.Build().Run();

// Trigger watch restart.

// For test use only.
// Looks for an environment variable that forces the use of HTTP for all the endpoints. We
// are doing this for ease of running the Playwright tests in CI.
static bool ShouldUseHttpForEndpoints()
{
    const string EnvVarName = "ESHOP_USE_HTTP_ENDPOINTS";
    var envValue = Environment.GetEnvironmentVariable(EnvVarName);

    // Attempt to parse the environment variable value; return true if it's exactly "1".
    return int.TryParse(envValue, out int result) && result == 1;
}
