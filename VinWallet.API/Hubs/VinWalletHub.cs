using Microsoft.AspNetCore.SignalR;

public class VinWalletHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var houseId = Context.GetHttpContext().Request.Query["houseId"].ToString();
        if (!string.IsNullOrEmpty(houseId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, houseId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var houseId = Context.GetHttpContext().Request.Query["houseId"].ToString();
        if (!string.IsNullOrEmpty(houseId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, houseId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
