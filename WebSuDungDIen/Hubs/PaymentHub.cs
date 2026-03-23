using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebSuDungDIen.Hubs
{
    [AllowAnonymous]
    public class PaymentHub : Hub
    {
    }
}