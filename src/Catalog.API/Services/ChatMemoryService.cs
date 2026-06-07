using Microsoft.Azure.Cosmos;

namespace eShop.Catalog.API.Services;

public class ChatMemoryService : IChatMemoryService
{
    private readonly CosmosClient _cosmosClient;
    private Container? _container;
    private readonly ILogger<ChatMemoryService> _logger;

    public ChatMemoryService(CosmosClient cosmosClient, ILogger<ChatMemoryService> logger)
    {
        _cosmosClient = cosmosClient;
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_container != null)
            return;

        try
        {
            var databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync("chatdb");
            var containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(
                "sessions",
                "/userId"
            );
            _container = containerResponse.Container;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Cosmos DB container");
            throw;
        }
    }

    public async Task SaveSessionAsync(
        string sessionId,
        string userId,
        List<ChatMessageDto> messages
    )
    {
        await EnsureInitializedAsync();

        var document = new ChatSessionDocument
        {
            id = sessionId,
            userId = userId,
            messages = messages,
            timestamp = DateTime.UtcNow,
        };

        await _container!.UpsertItemAsync(document, new PartitionKey(userId));
    }

    public async Task<ChatSessionDto?> GetSessionAsync(string sessionId, string userId)
    {
        await EnsureInitializedAsync();
        try
        {
            var response = await _container!.ReadItemAsync<ChatSessionDocument>(
                sessionId,
                new PartitionKey(userId)
            );
            var doc = response.Resource;
            return new ChatSessionDto
            {
                SessionId = doc.id,
                UserId = doc.userId,
                Messages = doc.messages,
                Timestamp = doc.timestamp,
            };
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "failed to retrieve session {sessionId} for user {userId}",
                sessionId,
                userId
            );
            return null;
        }
    }
}
