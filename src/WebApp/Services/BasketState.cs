using System.Security.Claims;
using eShop.WebAppComponents.Catalog;
using eShop.WebAppComponents.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace eShop.WebApp.Services;

public class BasketState(
    BasketService basketService,
    CatalogService catalogService,
    OrderingService orderingService,
    DiscountService discountService,
    AuthenticationStateProvider authenticationStateProvider,
    BasketUpdateNotifier basketUpdateNotifier
) : IBasketState
{
    private Task<IReadOnlyCollection<BasketItem>>? _cachedBasket;
    private HashSet<BasketStateChangedSubscription> _changeSubscriptions = new();

    public async Task DeleteBasketAsync()
    {
        await basketService.DeleteBasketAsync();
        _cachedBasket = null;
        
        var buyerId = await authenticationStateProvider.GetBuyerIdAsync() ?? "guest";
        basketUpdateNotifier.NotifyUpdate(buyerId);

        await NotifyChangeSubscribersAsync();
    }

    public async Task<IReadOnlyCollection<BasketItem>> GetBasketItemsAsync() =>
        (await GetUserAsync()).Identity?.IsAuthenticated == true
            ? await FetchBasketItemsAsync()
            : [];

    public IDisposable NotifyOnChange(EventCallback callback)
    {
        var subscription = new BasketStateChangedSubscription(this, callback);
        _changeSubscriptions.Add(subscription);
        return subscription;
    }

    public void ClearCache()
    {
        _cachedBasket = null;
    }

    public async Task AddAsync(CatalogItem item, int quantity = 1)
    {
        if (quantity <= 0) quantity = 1;
        var items = (await FetchBasketItemsAsync())
            .Select(i => new BasketQuantity(i.ProductId, i.Quantity))
            .ToList();
        bool found = false;
        for (var i = 0; i < items.Count; i++)
        {
            var existing = items[i];
            if (existing.ProductId == item.Id)
            {
                items[i] = existing with { Quantity = existing.Quantity + quantity };
                found = true;
                break;
            }
        }

        if (!found)
        {
            items.Add(new BasketQuantity(item.Id, quantity));
        }

        _cachedBasket = null;
        await basketService.UpdateBasketAsync(items);
        
        var buyerId = await authenticationStateProvider.GetBuyerIdAsync() ?? "guest";
        basketUpdateNotifier.NotifyUpdate(buyerId);
        
        await NotifyChangeSubscribersAsync();
    }

    public async Task SetQuantityAsync(int productId, int quantity)
    {
        var existingItems = (await FetchBasketItemsAsync()).ToList();
        if (existingItems.FirstOrDefault(row => row.ProductId == productId) is { } row)
        {
            if (quantity > 0)
            {
                row.Quantity = quantity;
            }
            else
            {
                existingItems.Remove(row);
            }

            _cachedBasket = null;
            await basketService.UpdateBasketAsync(
                existingItems.Select(i => new BasketQuantity(i.ProductId, i.Quantity)).ToList()
            );

            var buyerId = await authenticationStateProvider.GetBuyerIdAsync() ?? "guest";
            basketUpdateNotifier.NotifyUpdate(buyerId);

            await NotifyChangeSubscribersAsync();
        }
    }

    public async Task CheckoutAsync(BasketCheckoutInfo checkoutInfo)
    {
        if (checkoutInfo.RequestId == default)
        {
            checkoutInfo.RequestId = Guid.NewGuid();
        }

        var buyerId =
            await authenticationStateProvider.GetBuyerIdAsync()
            ?? throw new InvalidOperationException("User does not have a buyer ID");
        var userName =
            await authenticationStateProvider.GetUserNameAsync()
            ?? throw new InvalidOperationException("User does not have a user name");

        // Get details for the items in the basket
        var orderItems = await FetchBasketItemsAsync();

        // Call into Ordering.API to create the order using those details
        var request = new CreateOrderRequest(
            UserId: buyerId,
            UserName: userName,
            City: checkoutInfo.City!,
            Street: checkoutInfo.Street!,
            State: checkoutInfo.State!,
            Country: checkoutInfo.Country!,
            ZipCode: checkoutInfo.ZipCode!,
            CardNumber: "1111222233334444",
            CardHolderName: "TESTUSER",
            CardExpiration: DateTime.UtcNow.AddYears(1),
            CardSecurityNumber: "111",
            CardTypeId: checkoutInfo.CardTypeId,
            Buyer: buyerId,
            Items: [.. orderItems]
        );
        await orderingService.CreateOrder(request, checkoutInfo.RequestId);
        await DeleteBasketAsync();
    }

    private Task NotifyChangeSubscribersAsync() =>
        Task.WhenAll(_changeSubscriptions.Select(s => s.NotifyAsync()));

    private async Task<ClaimsPrincipal> GetUserAsync() =>
        (await authenticationStateProvider.GetAuthenticationStateAsync()).User;

    private Task<IReadOnlyCollection<BasketItem>> FetchBasketItemsAsync()
    {
        return _cachedBasket ??= FetchCoreAsync();

        async Task<IReadOnlyCollection<BasketItem>> FetchCoreAsync()
        {
            var quantities = await basketService.GetBasketAsync();
            if (quantities.Count == 0)
            {
                return [];
            }

            var buyerId = await authenticationStateProvider.GetBuyerIdAsync();

            // Get details for the items in the basket
            var basketItems = new List<BasketItem>();
            var productIds = quantities.Select(row => row.ProductId);
            var catalogItems = (await catalogService.GetCatalogItems(productIds)).ToDictionary(
                k => k.Id,
                v => v
            );

            decimal tempTotal = 0; // Total price ban đầu chưa giảm giá

            foreach (var item in quantities)
            {
                tempTotal += catalogItems[item.ProductId].Price * item.Quantity;
            }
            decimal DiscountRate = 0;
            if (!string.IsNullOrEmpty(buyerId))
            {
                var discountInfo = await discountService.GetDiscountAsync(buyerId, tempTotal);
                if (discountInfo != null)
                {
                    DiscountRate = discountInfo.DiscountRate;
                }
            }

            foreach (var item in quantities)
            {
                var catalogItem = catalogItems[item.ProductId];
                var orderItem = new BasketItem
                {
                    Id = Guid.NewGuid().ToString(), // TODO: this value is meaningless, use ProductId instead.
                    ProductId = catalogItem.Id,
                    ProductName = catalogItem.Name,
                    UnitPrice = catalogItem.Price * (1 - DiscountRate),
                    Quantity = item.Quantity,
                };
                basketItems.Add(orderItem);
            }

            return basketItems;
        }
    }

    private class BasketStateChangedSubscription(BasketState Owner, EventCallback Callback)
        : IDisposable
    {
        public Task NotifyAsync() => Callback.InvokeAsync();

        public void Dispose() => Owner._changeSubscriptions.Remove(this);
    }
}

public record CreateOrderRequest(
    string UserId,
    string UserName,
    string City,
    string Street,
    string State,
    string Country,
    string ZipCode,
    string CardNumber,
    string CardHolderName,
    DateTime CardExpiration,
    string CardSecurityNumber,
    int CardTypeId,
    string Buyer,
    List<BasketItem> Items
);
