namespace Discount.API;

public class CustomerLoyalty
{
    public string Id { get; set; } = null!; // Customer Guid
    public decimal TotalSpent { get; set; }
    public string Rank { get; set; } = "NOR";
}
