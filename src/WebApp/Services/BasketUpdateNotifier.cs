using System;
using System.Threading.Tasks;

namespace eShop.WebApp.Services;

public class BasketUpdateNotifier
{
    public event Func<string, Task>? OnBasketUpdated;

    public void NotifyUpdate(string userId)
    {
        OnBasketUpdated?.Invoke(userId);
    }
}
