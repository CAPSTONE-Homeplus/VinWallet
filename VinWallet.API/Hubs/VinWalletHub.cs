using Microsoft.AspNetCore.SignalR;

public class VinWalletHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext().Request.Query["userId"].ToString();
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var userId = Context.GetHttpContext().Request.Query["userId"].ToString();
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
