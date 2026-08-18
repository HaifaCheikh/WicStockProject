using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace WicStock_.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrEmpty(userRole))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userRole);
            }
            
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrEmpty(userRole))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userRole);
            }
            
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}
