namespace eShop.WebApp.Services;

public class OrderingService(HttpClient httpClient)
{
    private readonly string remoteServiceBaseUrl = "/api/Orders/";

    public async Task<OrderRecord[]> GetOrders()
    {
        try
        {
            var response = await httpClient.GetAsync(remoteServiceBaseUrl);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var errorMsg = $"GetOrders failed: {response.StatusCode} - {content}";
                System.IO.File.WriteAllText("webapp_error.txt", errorMsg);
                throw new HttpRequestException(errorMsg);
            }
            return (await response.Content.ReadFromJsonAsync<OrderRecord[]>())!;
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText("webapp_error.txt", ex.ToString());
            throw;
        }
    }

    public async Task CreateOrder(CreateOrderRequest request, Guid requestId)
    {
        try
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, remoteServiceBaseUrl);
            requestMessage.Headers.Add("x-requestid", requestId.ToString());
            requestMessage.Content = JsonContent.Create(request);
            var response = await httpClient.SendAsync(requestMessage);
            
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var errorMsg = $"CreateOrder failed: {response.StatusCode} - {content}";
                System.IO.File.WriteAllText("webapp_error.txt", errorMsg);
                throw new HttpRequestException(errorMsg);
            }
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText("webapp_error.txt", ex.ToString());
            throw;
        }
    }
}

public record OrderRecord(
    int OrderNumber,
    DateTime Date,
    string Status,
    decimal Total);
