namespace eShop.Catalog.API.Services;

public class ChatMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ChatSessionDto
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public List<ChatMessageDto> Messages { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class ChatSessionDocument
{
    //Cosmos DB ID (SessionId)
    public string id { get; set; } = string.Empty;

    //Partition Key
    public string userId  { get; set; } = string.Empty;
    public List<ChatMessageDto> messages { get; set; } = new();
    public DateTime timestamp { get; set; } = DateTime.UtcNow;
}