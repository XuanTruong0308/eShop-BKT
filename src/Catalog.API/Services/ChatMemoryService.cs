using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;

namespace eShop.Catalog.API.Services;

public class ChatMemoryService : IChatMemoryService
{
    private readonly CosmosClient _cosmosClient;
    private Container? _container;
    private readonly ILogger<ChatMemoryService> _logger;
    private bool _useInMemoryFallback = true; // Set to true by default to bypass Cosmos DB Emulator for maximum local performance and RAM savings
    private static readonly ConcurrentDictionary<string, ChatSessionDto> _inMemorySessions = new();

    public ChatMemoryService(CosmosClient cosmosClient, ILogger<ChatMemoryService> logger)
    {
        _cosmosClient = cosmosClient;
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_container != null || _useInMemoryFallback)
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
            _logger.LogWarning(ex, "Failed to initialize Cosmos DB container. Falling back to In-Memory session storage.");
            _useInMemoryFallback = true;
        }
    }

    public async Task SaveSessionAsync(
        string sessionId,
        string userId,
        List<ChatMessageDto> messages
    )
    {
        await EnsureInitializedAsync();

        if (_useInMemoryFallback)
        {
            var session = new ChatSessionDto
            {
                SessionId = sessionId,
                UserId = userId,
                Messages = messages,
                Timestamp = DateTime.UtcNow
            };
            _inMemorySessions[$"{userId}:{sessionId}"] = session;
            return;
        }

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

        if (_useInMemoryFallback)
        {
            if (_inMemorySessions.TryGetValue($"{userId}:{sessionId}", out var session))
            {
                return session;
            }
            return null;
        }

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
