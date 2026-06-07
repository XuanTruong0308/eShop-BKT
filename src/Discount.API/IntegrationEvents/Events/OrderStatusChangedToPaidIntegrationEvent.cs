using eShop.EventBus.Events;

namespace Discount.API.IntegrationEvents.Events;

public record OrderStatusChangedToPaidIntegrationEvent : IntegrationEvent
{
    public int OrderId { get; init; }
    public string? BuyerIdentityGuid { get; init; }
    public string? BuyerName { get; init; }
    public decimal Total { get; init; }
}