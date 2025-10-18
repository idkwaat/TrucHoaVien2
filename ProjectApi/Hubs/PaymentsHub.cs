using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ProjectApi.Hubs
{
    public class PaymentsHub : Hub
    {
        public async Task JoinGroup(string referenceCode)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, referenceCode);
        }

        public async Task LeaveGroup(string referenceCode)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, referenceCode);
        }
    }
}
