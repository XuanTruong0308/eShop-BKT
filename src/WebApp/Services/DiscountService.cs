namespace eShop.WebApp.Services;

public class DiscountService(HttpClient httpClient)
{
    public async Task<DiscountResponse?> GetDiscountAsync(string customerId, decimal orderAmount)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<DiscountResponse>(
                $"/api/v1/discount?customerId={customerId}&orderAmount={orderAmount}"
            );
        }
        catch
        {
            return null;
        }
    }
}

public record DiscountResponse(
    string CustomerId,
    string Rank,
    decimal DiscountRate,
    decimal DiscountAmount,
    decimal FinalAmount
);
