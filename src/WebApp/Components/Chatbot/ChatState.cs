using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
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
            You are an AI customer service agent for the online retailer AdventureWorks.
            You NEVER respond about topics other than AdventureWorks.
            Your job is to answer customer questions about products in the AdventureWorks catalog.
            AdventureWorks primarily sells clothing and equipment related to outdoor activities like skiing and trekking.
            You try to be concise and only provide longer responses if necessary.
            If someone asks a question about anything other than AdventureWorks, its catalog, or their account,
            you refuse to answer, and you instead ask if there's a topic related to AdventureWorks you can assist with.

            CRITICAL SEARCH & TOOL CALLING RULES:
            1. If you decide to call any tool (like `SearchCatalog` or `AddToCart`), you MUST NOT output any
               conversational text or thoughts in the same turn. Your text response content MUST be empty (null
               or empty string). You are only allowed to return the tool call. Only after the tool execution is
               completed and you receive the tool response, you can write the final text response to the user.
            2. The catalog search tool (`SearchCatalog`) only supports search descriptions in English. If the
               user asks in another language (like Vietnamese), you MUST translate their search query or product
               description to English before calling `SearchCatalog`.
            3. Only call the `AddToCart` tool when the user explicitly and directly requests to buy, order, or
               put a product into the shopping cart using clear purchase-intent words such as:
               "buy", "add to cart", "order", "mua", "thêm vào giỏ", "đặt mua", "lấy cho tôi",
               "cho tôi mua", "tôi muốn mua".
            4. NEVER call `AddToCart` if the user is only asking for product details, descriptions,
               recommendations, links to products, or asking to view more models.
               In Vietnamese, the word "thêm" can mean "introduce more/show more"
               (e.g. "thêm các mẫu khác", "giới thiệu thêm"). You must distinguish this: if they want to
               see more products, use `SearchCatalog` and present them. Do NOT call `AddToCart` in this case.
            5. If the user sends ONLY a product name (e.g., "TrailTracker Hiking Shoes") or asks to view/know
               more about a specific product (e.g., "chi tiết sản phẩm X", "cho tôi biết về X",
               "TrailTracker Hiking Shoes là gì"), treat it as a request to VIEW product details.
               Call `SearchCatalog` and return the product info using the required link and Add to Cart button
               format. NEVER call `AddToCart` in this case. A product name alone is NOT purchase intent.
            6. If the user asks for a link to a product (e.g., "tôi muốn link", "cho tôi link",
               "dẫn tới sản phẩm"), you MUST call `SearchCatalog` (not `AddToCart`), and then return the
               product formatted as a markdown link `[Product Name](/item/ProductId)`.
            7. When calling `AddToCart`, the default quantity MUST be 1 unless the user explicitly specifies
               a larger quantity (e.g., "lấy 2 đôi"). Do not add multiple items automatically.
            8. Always use the standard JSON tool calling format. NEVER output custom XML-like tags such as
               `<function=...>` or `<tool_call>`.

            CRITICAL CHAT LOOP & HALLUCINATION PREVENTION RULES:
            1. Strictly Grounded Answers: You MUST only answer questions using the catalog data returned by
               the `SearchCatalog` tool or the listed brands and types. Never make up or hallucinate product
               details, price, specifications, brands, or IDs. If a product is not found, state clearly that
               it is not available in the AdventureWorks catalog.
            2. Tool Calling Loop Prevention: Do NOT invoke the same tool multiple times in a single turn.
               If a tool call fails or returns empty/error, do not retry calling it with the same parameters.
               If you have already called `AddToCart` for a product in previous turns, do NOT call it again
               unless the user explicitly asks to add more. Check the cart content with `GetCartContents`
               first if you need to verify.
            3. Conversational Loop Prevention: If you notice the user repeats the exact same question or
               command, do not repeat your previous output blindly. Acknowledge what was done, state the
               current status (e.g., "Sản phẩm đã được thêm vào giỏ hàng") and ask how else you can help.

            CRITICAL PRODUCT FORMATTING RULES:
            When suggesting or listing products to the user, you MUST present them using the following rich
            UI formatting conventions:
            1. Product Link: Format the product name as a markdown link to its details page:
               `[Product Name](/item/ProductId)`. Do NOT output product names as plain text or static bold
               text without a link.
            2. List Formatting: Present product choices as a bulleted list. Each product MUST be formatted
               exactly as:
               * **[Product Name](/item/ProductId)** by BrandName for $Price - [Add to Cart](submit:add-to-cart:ProductId)
               This exact format is required for the user interface to correctly render links and Add to Cart
               buttons.

               CRITICAL: You MUST use the asterisk symbol `*` for list items. NEVER use numbers (1., 2., 3.)
               or dashes (-). If you use numbers, the user interface will fail to render the links and buttons.

               For Vietnamese responses, you MUST keep the technical markdown link structure exactly the same
               (do not translate "/item/" or "submit:add-to-cart:"):
               * **[Tên Sản Phẩm](/item/ProductId)** by BrandName for $Price - [Add to Cart](submit:add-to-cart:ProductId): [Vietnamese description here]

               Example of a correct Vietnamese response listing products (follow this format exactly):
               "Dưới đây là một số đôi giày đi mưa từ AdventureWorks:
               * **[Trekker Clear Hiking Shoes](/item/1)** by AdventureWorks for $69.49 - [Add to Cart](submit:add-to-cart:1): Đôi giày này có thiết kế trong suốt, chống thấm nước tốt.
               * **[Trailblazer Black Hiking Shoes](/item/3)** by AdventureWorks for $129.99 - [Add to Cart](submit:add-to-cart:3): Thiết kế màu đen bền bỉ."

            3. Product Image: If appropriate or requested, you can render the product's image on a separate
               line using markdown image format: `![Product Name](PictureUrl)`
            4. General Filter/Category links: If the user is looking for a type or brand generally, suggest
               a link to the filter page: `[View all TypeName](/?type=TypeId)` or
               `[View BrandName](/?brand=BrandId)`.

            Available Catalog Categories (Types):
            {{types}}

            Available Catalog Brands:
            {{brands}}
            """;

    // Tool definitions để gửi lên API (không dùng AIFunctionFactory vì bỏ UseFunctionInvocation)
    private readonly IList<AITool> _tools;

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

        _tools =
        [
            AIFunctionFactory.Create(GetUserInfo),
            AIFunctionFactory.Create(SearchCatalog),
            AIFunctionFactory.Create(AddToCart),
            AIFunctionFactory.Create(GetCartContents),
        ];

        _chatOptions = new() { Temperature = 0.0f, Tools = _tools };

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
            _logger.LogInformation(
                "No existing chat session for {SessionId}, starting fresh.",
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
            // Chỉ giữ System + User/Assistant text thuần túy để gửi lên API
            var currentMessages = Messages
                .Where(m =>
                    m.Role == ChatRole.System
                    || (
                        (m.Role == ChatRole.User || m.Role == ChatRole.Assistant)
                        && !string.IsNullOrWhiteSpace(m.Text)
                    )
                )
                .Select(m => new ChatMessage(m.Role, m.Text!))
                .ToList();

            string? finalText = await RunAgentLoopAsync(currentMessages, cancellationToken);

            if (!string.IsNullOrWhiteSpace(finalText))
                Messages.Add(new ChatMessage(ChatRole.Assistant, finalText));

            onMessageAdded();
            _ = SaveSessionHistoryAsync();
        }
        catch (OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Chat response generation was cancelled.");
            _ = SaveSessionHistoryAsync();
            throw;
        }
        catch (Exception e)
        {
            if (e.ToString().Contains("429") || e.Message.Contains("Too Many Requests"))
                _logger.LogWarning("API Rate Limit exceeded (429 Too Many Requests).");
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

    private async Task<string?> RunAgentLoopAsync(
        List<ChatMessage> currentMessages,
        CancellationToken cancellationToken
    )
    {
        const int maxIterations = 5;

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            ChatResponse response;
            try
            {
                response = await _chatClient.GetResponseAsync(
                    currentMessages,
                    _chatOptions,
                    cancellationToken
                );
            }
            catch (Exception ex)
                when (ex.Message.Contains("tool_use_failed") || ex.Message.Contains("400"))
            {
                // Groq từ chối tool call format.
                // Fallback: gọi SearchCatalog thủ công rồi inject kết quả vào context,
                // sau đó gọi lại model không có tools để tạo text response tự nhiên.
                _logger.LogWarning(
                    "tool_use_failed at iteration {i}, using manual search fallback.",
                    iteration
                );

                // Lấy query từ user message cuối
                var lastUserText =
                    currentMessages.LastOrDefault(m => m.Role == ChatRole.User)?.Text
                    ?? string.Empty;

                // Tự gọi SearchCatalog với query là text của user
                var searchResult = await SearchCatalog(lastUserText);

                // Inject kết quả search vào context như một tool result giả
                var fallbackMessages = currentMessages.ToList();
                fallbackMessages.Add(
                    new ChatMessage(
                        ChatRole.System,
                        $"[Search result for '{lastUserText}']: {searchResult}\n"
                            + "Based on the above search results, answer the user's question in natural language. "
                            + "Format products as: * **[Name](/item/ID)** by Brand for $Price - [Add to Cart](submit:add-to-cart:ID)"
                    )
                );

                var noToolOptions = new ChatOptions { Temperature = _chatOptions.Temperature };
                response = await _chatClient.GetResponseAsync(
                    fallbackMessages,
                    noToolOptions,
                    cancellationToken
                );

                return response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text;
            }

            var toolCalls = response
                .Messages.SelectMany(m => m.Contents)
                .OfType<FunctionCallContent>()
                .ToList();

            if (!toolCalls.Any())
            {
                var assistantText =
                    response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text
                    ?? string.Empty;

                // Fallback: Llama đôi khi leak tool call dưới dạng JSON text
                // thay vì structured FunctionCallContent.
                // Ví dụ: ```json\n{"tool": "SearchCatalog", "query": "..."}\n```
                var inlineCall = TryParseInlineToolCall(assistantText);
                if (inlineCall is not null)
                {
                    _logger.LogInformation(
                        "Detected inline JSON tool call: {Tool}",
                        inlineCall.Name
                    );

                    // Thực thi tool thủ công
                    var toolResult = await ExecuteToolAsync(inlineCall, cancellationToken);

                    // Inject kết quả vào context rồi gọi lại model để tạo text response
                    var followUpMessages = currentMessages.ToList();
                    followUpMessages.Add(new ChatMessage(ChatRole.Assistant, assistantText));
                    followUpMessages.Add(
                        new ChatMessage(
                            ChatRole.System,
                            $"Tool result for {inlineCall.Name}: {toolResult}\n"
                                + "Now respond to the user in natural language using the above data. "
                                + "Format products as: * **[Name](/item/ID)** by Brand for $Price - [Add to Cart](submit:add-to-cart:ID)"
                        )
                    );

                    var noToolOptions = new ChatOptions { Temperature = _chatOptions.Temperature };
                    var followUp = await _chatClient.GetResponseAsync(
                        followUpMessages,
                        noToolOptions,
                        cancellationToken
                    );
                    return followUp.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text;
                }

                return assistantText;
            }

            // Thêm assistant message có tool calls vào history
            foreach (var msg in response.Messages)
                currentMessages.Add(msg);

            // Thực thi tool và thêm kết quả
            foreach (var toolCall in toolCalls)
            {
                var toolResult = await ExecuteToolAsync(toolCall, cancellationToken);
                currentMessages.Add(
                    new ChatMessage(
                        ChatRole.Tool,
                        [new FunctionResultContent(toolCall.CallId, toolResult)]
                    )
                );

                _logger.LogInformation(
                    "Tool {Name} executed, result length: {Len}",
                    toolCall.Name,
                    toolResult?.Length
                );
            }
        }

        _logger.LogWarning("Agent loop reached max iterations without final response.");
        return null;
    }

    // Thực thi tool call thủ công — không phụ thuộc FunctionInvokingChatClient
    private async Task<string> ExecuteToolAsync(
        FunctionCallContent toolCall,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Parse arguments từ IDictionary
            string GetArg(string key) =>
                toolCall.Arguments?.TryGetValue(key, out var val) == true
                    ? val?.ToString() ?? string.Empty
                    : string.Empty;

            return toolCall.Name switch
            {
                nameof(GetUserInfo) => GetUserInfo(),
                nameof(SearchCatalog) => await SearchCatalog(GetArg("productDescription")),
                nameof(AddToCart) => await AddToCart(
                    int.TryParse(GetArg("itemId"), out var id) ? id : 0,
                    int.TryParse(GetArg("quantity"), out var qty) ? qty : 1
                ),
                nameof(GetCartContents) => await GetCartContents(),
                _ => $"Unknown tool: {toolCall.Name}",
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Tool execution failed: {Tool}", toolCall.Name);
            return $"Tool execution failed: {e.Message}";
        }
    }

    // Parse tool call từ text khi model leak JSON thay vì dùng structured tool call
    // Hỗ trợ các format phổ biến của Llama:
    // {"tool": "SearchCatalog", "query": "..."}
    // {"tool": "SearchCatalog", "productDescription": "..."}
    // {"tool": "AddToCart", "itemId": 5, "quantity": 1}
    private FunctionCallContent? TryParseInlineToolCall(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            // Tìm JSON block trong text (có thể bọc trong ```json ... ```)
            var jsonStart = text.IndexOf('{');
            var jsonEnd = text.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd < 0 || jsonEnd <= jsonStart)
                return null;

            var jsonStr = text[jsonStart..(jsonEnd + 1)];
            var node = JsonNode.Parse(jsonStr);
            if (node is not JsonObject obj)
                return null;

            // Lấy tên tool
            var toolName = obj["tool"]?.ToString() ?? obj["function"]?.ToString();
            if (string.IsNullOrWhiteSpace(toolName))
                return null;

            // Map tên tool về đúng method name (Llama hay viết khác case)
            toolName = toolName.Trim() switch
            {
                "SearchCatalog" or "searchCatalog" or "search_catalog" => nameof(SearchCatalog),
                "AddToCart" or "addToCart" or "add_to_cart" => nameof(AddToCart),
                "GetCartContents" or "getCartContents" or "get_cart_contents" => nameof(
                    GetCartContents
                ),
                "GetUserInfo" or "getUserInfo" or "get_user_info" => nameof(GetUserInfo),
                _ => toolName,
            };

            // Build arguments dictionary
            var args = new Dictionary<string, object?>();
            foreach (var kv in obj)
            {
                if (kv.Key == "tool" || kv.Key == "function")
                    continue;
                // Map các tên argument khác nhau
                var argKey = kv.Key switch
                {
                    "query" or "description" or "search" => "productDescription",
                    "id" or "item_id" or "product_id" => "itemId",
                    "qty" or "count" or "amount" => "quantity",
                    _ => kv.Key,
                };
                args[argKey] = kv.Value?.ToString();
            }

            return new FunctionCallContent(
                callId: Guid.NewGuid().ToString("N")[..8],
                name: toolName,
                arguments: args
            );
        }
        catch
        {
            return null;
        }
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
