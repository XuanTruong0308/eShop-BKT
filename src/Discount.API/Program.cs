using Discount.API;
using Discount.API.IntegrationEvents.EventHandling;
using Discount.API.IntegrationEvents.Events;
using eShop.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<LoyaltyDbContext>("discountdb");
builder.AddRedisClient("redis");

builder
    .AddRabbitMqEventBus("EventBus")
    .AddSubscription<
        OrderStatusChangedToPaidIntegrationEvent,
        OrderStatusChangedToPaidIntegrationEventHandler
    >();

var app = builder.Build();

app.MapDefaultEndpoints();

// Ensure the database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LoyaltyDbContext>();
    await context.Database.EnsureCreatedAsync();
}

app.MapGet(
    "api/v1/discount",
    async (
        string customerId,
        decimal orderAmount,
        LoyaltyDbContext dbContext,
        StackExchange.Redis.IConnectionMultiplexer redisConnection
    ) =>
    {
        if (customerId == "debug_all")
        {
            var allInDb = await dbContext.CustomerLoyalties.ToListAsync();
            return Results.Ok(allInDb);
        }

        string rank = "NOR";
        var redisDb = redisConnection.GetDatabase();
        string cacheKey = $"loyalty:{customerId}";

        try
        {
            var cachedRank = await redisDb.StringGetAsync(cacheKey);
            if (!cachedRank.IsNullOrEmpty)
            {
                rank = cachedRank.ToString();
            }
            else
            {
                var loyaltyData = await dbContext.CustomerLoyalties.FindAsync(customerId);
                if (loyaltyData != null)
                {
                    rank = loyaltyData.Rank;
                }
                // Write back to cache
                await redisDb.StringSetAsync(cacheKey, rank, TimeSpan.FromMinutes(30));
            }
        }
        catch
        {
            var loyaltyData = await dbContext.CustomerLoyalties.FindAsync(customerId);
            if (loyaltyData != null)
            {
                rank = loyaltyData.Rank;
            }
        }

        decimal discountRate = rank switch
        {
            "SVIP" => 0.35m,
            "VIP" => 0.30m,
            "Dimond" => 0.25m,
            "Platium" => 0.20m,
            _ => 0.00m,
        };

        var discountAmount = orderAmount * discountRate;
        var finalAmount = orderAmount - discountAmount;

        return Results.Ok(
            new
            {
                customerId = customerId,
                Rank = rank,
                DiscountRate = discountRate,
                discountAmount = discountAmount,
                FinalAmount = finalAmount,
            }
        );
    }
);

app.Run();
