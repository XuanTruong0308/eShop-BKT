using eShop.Basket.API.Repositories;
using eShop.Basket.API.Model;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApplicationServices();

builder.Services.AddGrpc();

builder.WebHost.ConfigureKestrel(options =>
{
    int httpPort = 5221;
    var httpPortStr = builder.Configuration["PORT_http"] ?? builder.Configuration["HTTP_PORTS"];
    if (!string.IsNullOrEmpty(httpPortStr) && int.TryParse(httpPortStr, out var hp))
    {
        httpPort = hp;
    }
    else
    {
        var urls = builder.Configuration["ASPNETCORE_URLS"]?.Split(';', StringSplitOptions.RemoveEmptyEntries);
        if (urls != null && urls.Length >= 1)
        {
            var httpPortPart = urls[0].Split(':').LastOrDefault();
            if (int.TryParse(httpPortPart, out var hp2))
            {
                httpPort = hp2;
            }
        }
    }

    int grpcPort = 5021;
    var grpcPortStr = builder.Configuration["PORT_grpc"];
    if (!string.IsNullOrEmpty(grpcPortStr) && int.TryParse(grpcPortStr, out var gp))
    {
        grpcPort = gp;
    }

    options.ListenAnyIP(httpPort, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1);
    options.ListenAnyIP(grpcPort, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<BasketService>();

// REST APIs for Basket
app.MapGet("/api/basket", async (IBasketRepository repository, HttpContext httpContext) =>
{
    var userId = httpContext.User.FindFirst("sub")?.Value ?? "alice";
    var basket = await repository.GetBasketAsync(userId);
    return Results.Ok(basket ?? new CustomerBasket(userId));
});

app.MapPost("/api/basket", async (CustomerBasket basket, IBasketRepository repository, HttpContext httpContext) =>
{
    var userId = httpContext.User.FindFirst("sub")?.Value ?? "alice";
    basket.BuyerId = userId;
    var updatedBasket = await repository.UpdateBasketAsync(basket);
    return Results.Ok(updatedBasket);
});

app.MapDelete("/api/basket", async (IBasketRepository repository, HttpContext httpContext) =>
{
    var userId = httpContext.User.FindFirst("sub")?.Value ?? "alice";
    await repository.DeleteBasketAsync(userId);
    return Results.Ok();
});

app.Run();
