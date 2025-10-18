using Microsoft.AspNetCore.SignalR;

namespace ProjectApi.Hubs
{
    public class PaymentsHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var orderId = httpContext?.Request.Query["orderId"].ToString();
            if (!string.IsNullOrEmpty(orderId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"DH_{orderId}");
                Console.WriteLine($"👥 Client joined group DH_{orderId}");
            }

            await base.OnConnectedAsync();
        }
    }
}
