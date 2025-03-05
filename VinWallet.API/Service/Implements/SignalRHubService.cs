using AutoMapper;
using VinWallet.API.Service.Interfaces;
using VinWallet.Domain.Models;
using VinWallet.Repository.Generic.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace VinWallet.API.Service.Implements
{
    public class SignalRHubService : BaseService<SignalRHubService>, ISignalRHubService
    {
        private readonly IHubContext<VinWalletHub> _hubContext;


        public SignalRHubService(IUnitOfWork<VinWalletContext> unitOfWork, ILogger<SignalRHubService> logger,
                                 IMapper mapper, IHttpContextAccessor httpContextAccessor,
                                 IHubContext<VinWalletHub> hubContext)
            : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _hubContext = hubContext;
        }


        public async Task SendNotificationToAll(object message)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNotificationToAll", message);
        }

        public async Task SendNotificationToGroup(string groupName, object message)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotificationToGroup", message);
        }

        public async Task SendNotificationToUser(string userId, object message)
        {
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotificationToUser", message);
        }


    }
}
