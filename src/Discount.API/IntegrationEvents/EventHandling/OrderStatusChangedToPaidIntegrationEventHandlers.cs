using Discount.API.IntegrationEvents.Events;
using eShop.EventBus.Abstractions;
using StackExchange.Redis;

namespace Discount.API.IntegrationEvents.EventHandling;

public class OrderStatusChangedToPaidIntegrationEventHandler(
    LoyaltyDbContext dbContext,
    IConnectionMultiplexer redisConnection,
    ILogger<OrderStatusChangedToPaidIntegrationEventHandler> logger
) : IIntegrationEventHandler<OrderStatusChangedToPaidIntegrationEvent>
{
    public async Task Handle(OrderStatusChangedToPaidIntegrationEvent @event)
    {
        if (string.IsNullOrEmpty(@event.BuyerIdentityGuid))
        {
            logger.LogWarning(
                "Nhận được sự kiện Order Paid nhưng không tìm thấy BuyIdentityGuid (OrderId: {OrderId})",
                @event.OrderId
            );

            return;
        }

        logger.LogInformation(
            "Nhận được sự kiện Order Paid. OrderId: {OrderId}, Customer: {BuyerName}",
            @event.OrderId,
            @event.BuyerName
        );

        // 1. Cộng dồn chi tiêu và tính toán Rank mới dưới Database Postgres
        var loyalty = await dbContext.CustomerLoyalties.FindAsync(@event.BuyerIdentityGuid);
        if (loyalty == null)
        {
            loyalty = new CustomerLoyalty
            {
                Id = @event.BuyerIdentityGuid,
                TotalSpent = 0m,
                Rank = "NOR"
            };
            dbContext.CustomerLoyalties.Add(loyalty);
        }

        loyalty.TotalSpent += @event.Total;

        loyalty.Rank = loyalty.TotalSpent switch
        {
            >= 1000m => "SVIP",
            >= 500m => "VIP",
            >= 300m => "Dimond",
            >= 100m => "Platium",
            _ => "NOR"
        };

        await dbContext.SaveChangesAsync();

        // 2. Ghi đè trực tiếp thông tin Rank mới vào Redis Cache
        try
        {
            var redisDb = redisConnection.GetDatabase();
            string cacheKey = $"loyalty:{@event.BuyerIdentityGuid}";
            await redisDb.StringSetAsync(cacheKey, loyalty.Rank, TimeSpan.FromMinutes(30));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi ghi đè Redis Cache cho khách hàng {BuyerIdentityGuid}", @event.BuyerIdentityGuid);
        }

        logger.LogInformation(
            "Customer {BuyerName} spent: ${TotalSpent}. Current Rank: {Rank}",
            @event.BuyerName,
            loyalty.TotalSpent,
            loyalty.Rank
        );
    }
}
