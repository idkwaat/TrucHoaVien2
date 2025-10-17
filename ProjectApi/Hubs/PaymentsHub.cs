using Microsoft.AspNetCore.SignalR;

namespace ProjectApi.Hubs
{
    public class PaymentsHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var http = Context.GetHttpContext();
            var orderId = http?.Request.Query["orderId"].ToString();

            if (!string.IsNullOrEmpty(orderId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");
            }

            await base.OnConnectedAsync();
        }
    }
}
