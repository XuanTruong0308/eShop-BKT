using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using eShop.WebAppComponents.Services;
using Microsoft.Extensions.AI;

namespace eShop.WebApp.Chatbot;

public class ChatState
{
    private readonly ICatalogService _catalogService;
    private readonly IBasketState _basketState;
    private readonly ClaimsPrincipal _user;
    private readonly ILogger _logger;
    private readonly IProductImageUrlProvider _productImages;
    private readonly IChatClient _chatClient;
    private readonly ChatOptions _chatOptions;
    private IEnumerable<eShop.WebAppComponents.Catalog.CatalogBrand>? _brands;
    private IEnumerable<eShop.WebAppComponents.Catalog.CatalogItemType>? _types;
    private string _systemPrompt = string.Empty;

    public string SessionId { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;

    private string GetSystemPrompt(string brands, string types) =>
        $$"""
            You are an AI customer service agent for AdventureWorks (an outdoor clothing and equipment retailer).
            Only answer questions related to AdventureWorks catalog, brands, types, or user accounts. Refuse other topics.

            CRITICAL RULES:
            - Catalog search (`SearchCatalog`) only supports English. If the user asks in another language (like Vietnamese), you MUST translate their search query or product description to English first before calling `SearchCatalog`.
            - Only call `AddToCart` if the user explicitly wants to purchase or order.
            - If the user only asks about product details, descriptions, recommendations, or general information, call `SearchCatalog` to show details. NEVER call `AddToCart` for these.
            - In Vietnamese, do not confuse "thêm" (show more models/info/options) with "mua/thêm vào giỏ" (purchase).
            - Present product choices as a bulleted list. Each product MUST be formatted exactly as:
              * **[Product Name](/item/ProductId)** by BrandName for $Price - [Add to Cart](submit:add-to-cart:ProductId): [Description]
              (Use the asterisk symbol `*` for list items. NEVER use numbers or dashes).
            - For Vietnamese responses, keep the technical markdown link structure exactly the same:
              * **[Tên Sản Phẩm](/item/ProductId)** by BrandName for $Price - [Add to Cart](submit:add-to-cart:ProductId): [Vietnamese description here]
            - Never repeat a tool call with the exact same arguments in a single turn.

            Available Catalog Categories (Types):
            {{types}}

            Available Catalog Brands:
            {{brands}}
            """;

    public ChatState(
        ICatalogService catalogService,
        IBasketState basketState,
        ClaimsPrincipal user,
        IProductImageUrlProvider productImages,
        ILoggerFactory loggerFactory,
        IChatClient chatClient
    )
    {
        _catalogService = catalogService;
        _basketState = basketState;
        _user = user;
        _productImages = productImages;
        _logger = loggerFactory.CreateLogger(typeof(ChatState));

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "ChatModel: {model}",
                chatClient.GetService<ChatClientMetadata>()?.DefaultModelId
            );
        }

        _chatClient = chatClient;
        _chatOptions = new()
        {
            Temperature = 0.0f,
            Tools =
            [
                AIFunctionFactory.Create(GetUserInfo),
                AIFunctionFactory.Create(SearchCatalog),
                AIFunctionFactory.Create(AddToCart),
                AIFunctionFactory.Create(GetCartContents),
            ],
        };

        Messages = [];
    }

    public IList<ChatMessage> Messages { get; }

    public async Task InitializeAsync()
    {
        UserId = _user.Claims.FirstOrDefault(x => x.Type == "sub")?.Value ?? "guest";
        SessionId = UserId != "guest" ? UserId : "guest-" + Guid.NewGuid().ToString("N");

        try
        {
            _brands = await _catalogService.GetBrands();
            _types = await _catalogService.GetTypes();

            var brandsStr = string.Join(
                ", ",
                _brands?.Select(b => $"{b.Brand} (Id: {b.Id})") ?? Array.Empty<string>()
            );
            var typesStr = string.Join(
                ", ",
                _types?.Select(t => $"{t.Type} (Id: {t.Id})") ?? Array.Empty<string>()
            );
            _systemPrompt = GetSystemPrompt(brandsStr, typesStr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load brands/types for system prompt.");
            _systemPrompt = GetSystemPrompt(string.Empty, string.Empty);
        }

        Messages.Clear();
        Messages.Add(new ChatMessage(ChatRole.System, _systemPrompt));

        try
        {
            var savedSession = await _catalogService.GetChatSession(SessionId, UserId);
            if (savedSession != null && savedSession.Messages.Any())
            {
                foreach (var msg in savedSession.Messages)
                {
                    var role = msg.Role.ToLower() switch
                    {
                        "user" => ChatRole.User,
                        "assistant" => ChatRole.Assistant,
                        _ => (ChatRole?)null,
                    };

                    // Chỉ load User và Assistant text, bỏ qua System và mọi role khác
                    if (role is null || string.IsNullOrWhiteSpace(msg.Content))
                        continue;

                    Messages.Add(new ChatMessage(role.Value, msg.Content));
                }
            }
            else
            {
                Messages.Add(
                    new ChatMessage(
                        ChatRole.Assistant,
                        "Hi! I'm the AdventureWorks Concierge. How can I help?"
                    )
                );
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // 404 = session chưa tồn tại, đây là trường hợp bình thường cho user mới
            _logger.LogInformation(
                "No existing chat session found for {SessionId}, starting fresh.",
                SessionId
            );
            Messages.Add(
                new ChatMessage(
                    ChatRole.Assistant,
                    "Hi! I'm the AdventureWorks Concierge. How can I help?"
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load chat session.");
            Messages.Add(
                new ChatMessage(
                    ChatRole.Assistant,
                    "Hi! I'm the AdventureWorks Concierge. How can I help?"
                )
            );
        }
    }

    public async Task AddUserMessageAsync(
        string userText,
        Action onMessageAdded,
        CancellationToken cancellationToken = default
    )
    {
        Messages.Add(new ChatMessage(ChatRole.User, userText));
        onMessageAdded();

        try
        {
            // Trước khi gọi API, làm sạch Messages:
            // Xóa toàn bộ tool call và tool result từ các lượt trước.
            // Lý do: tool messages chứa metadata nội bộ (tool call id, function name...)
            // Nếu để lại trong Messages và gửi lên API lần sau → HTTP 400 tool_use_failed.
            // Chỉ giữ: System + User text + Assistant text thuần túy.
            var cleanMessages = Messages
                .Where(m =>
                    m.Role == ChatRole.System
                    || (
                        (m.Role == ChatRole.User || m.Role == ChatRole.Assistant)
                        && !string.IsNullOrWhiteSpace(m.Text)
                    )
                )
                .Select(m => new ChatMessage(m.Role, m.Text!))
                .ToList();

            var response = await _chatClient.GetResponseAsync(
                cleanMessages,
                _chatOptions,
                cancellationToken
            );

            // Chỉ add Assistant text cuối cùng vào Messages (bỏ qua tool call/result)
            var finalAssistant = response.Messages.LastOrDefault(m =>
                m.Role == ChatRole.Assistant && !string.IsNullOrWhiteSpace(m.Text)
            );

            if (finalAssistant is not null)
                Messages.Add(new ChatMessage(ChatRole.Assistant, finalAssistant.Text!));

            onMessageAdded();
            _ = SaveSessionHistoryAsync();
        }
        catch (OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Chat response generation was cancelled by the user.");

            _ = SaveSessionHistoryAsync();
            throw;
        }
        catch (Exception e)
        {
            if (e.ToString().Contains("429") || e.Message.Contains("Too Many Requests"))
                _logger.LogWarning("Gemini API Rate Limit exceeded (429 Too Many Requests).");
            else if (_logger.IsEnabled(LogLevel.Error))
                _logger.LogError(e, "Error getting chat completions.");

            Messages.Add(
                new ChatMessage(
                    ChatRole.Assistant,
                    "My apologies, but I encountered an unexpected error."
                )
            );
        }
        onMessageAdded();
    }

    [Description("Gets information about the chat user")]
    private string GetUserInfo()
    {
        var claims = _user.Claims;
        return JsonSerializer.Serialize(
            new
            {
                Name = GetValue(claims, "name"),
                LastName = GetValue(claims, "last_name"),
                Street = GetValue(claims, "address_street"),
                City = GetValue(claims, "address_city"),
                State = GetValue(claims, "address_state"),
                ZipCode = GetValue(claims, "address_zip_code"),
                Country = GetValue(claims, "address_country"),
                Email = GetValue(claims, "email"),
                PhoneNumber = GetValue(claims, "phone_number"),
            }
        );

        static string GetValue(IEnumerable<Claim> claims, string claimType) =>
            claims.FirstOrDefault(x => x.Type == claimType)?.Value ?? "";
    }

    [Description("Searches the AdventureWorks catalog for a provided product description")]
    private async Task<string> SearchCatalog(
        [Description("The product description for which to search")] string productDescription
    )
    {
        try
        {
            var results = await _catalogService.GetCatalogItemsWithSemanticRelevance(
                0,
                8,
                productDescription!
            );
            for (int i = 0; i < results.Data.Count; i++)
            {
                results.Data[i] = results.Data[i] with
                {
                    PictureUrl = _productImages.GetProductImageUrl(results.Data[i].Id),
                };
            }

            return JsonSerializer.Serialize(results);
        }
        catch (HttpRequestException e)
        {
            return Error(e, "Error accessing catalog.");
        }
    }

    [Description("Adds a product to the user's shopping cart.")]
    private async Task<string> AddToCart(
        [Description("The id of the product to add to the shopping cart (basket)")] int itemId,
        [Description("The quantity of the product to add to the shopping cart (basket)")]
            int quantity = 1
    )
    {
        try
        {
            var item = await _catalogService.GetCatalogItem(itemId);
            for (int i = 0; i < quantity; i++)
                await _basketState.AddAsync(item!);
            return "Item added to shopping cart.";
        }
        catch (Grpc.Core.RpcException e) when (e.StatusCode == Grpc.Core.StatusCode.Unauthenticated)
        {
            return "Unable to add an item to the cart. You must be logged in.";
        }
        catch (Exception e)
        {
            return Error(e, "Unable to add the item to the cart.");
        }
    }

    [Description("Gets information about the contents of the user's shopping cart (basket)")]
    private async Task<string> GetCartContents()
    {
        try
        {
            var basketItems = await _basketState.GetBasketItemsAsync();
            return JsonSerializer.Serialize(basketItems);
        }
        catch (Exception e)
        {
            return Error(e, "Unable to get the cart's contents.");
        }
    }

    private string Error(Exception e, string message)
    {
        if (_logger.IsEnabled(LogLevel.Error))
            _logger.LogError(e, message);

        return message;
    }

    private async Task SaveSessionHistoryAsync()
    {
        try
        {
            // Chỉ lưu User + Assistant text có nội dung.
            // Không lưu tool call/result vì không thể restore đúng cách.
            var dtoList = Messages
                .Where(m =>
                    (m.Role == ChatRole.User || m.Role == ChatRole.Assistant)
                    && !string.IsNullOrWhiteSpace(m.Text)
                )
                .Select(m => new ChatMessageDto { Role = m.Role.ToString(), Content = m.Text! })
                .ToList();

            await _catalogService.SaveChatSession(SessionId, UserId, dtoList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save chat history.");
        }
    }
}
