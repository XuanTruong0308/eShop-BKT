using Microsoft.Azure.Cosmos;

namespace eShop.Catalog.API.Services;

public interface IChatMemoryService
{
    Task SaveSessionAsync(string sessionId, string userId, List<ChatMessageDto> messages);
    Task<ChatSessionDto?> GetSessionAsync(string sessionId, string userId);
}