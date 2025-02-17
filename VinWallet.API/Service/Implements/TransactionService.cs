using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VinWallet.API.Hubs;
using VinWallet.API.Service.Interfaces;
using VinWallet.Domain.Models;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Enums;
using VinWallet.Repository.Generic.Interfaces;
using VinWallet.Repository.Payload.Request.TransactionRequest;
using VinWallet.Repository.Payload.Response.OrderResponse;
using VinWallet.Repository.Payload.Response.TransactionResponse;
using VinWallet.Repository.Utils;

namespace VinWallet.API.Service.Implements
{
    public class TransactionService : BaseService<TransactionService>, ITransactionService
    {
        private readonly IHubContext<VinWalletHub> _hubContext;
        public TransactionService(IUnitOfWork<VinWalletContext> unitOfWork, ILogger<TransactionService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, IHubContext<VinWalletHub> hubContext) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _hubContext = hubContext;
        }

        public async Task<TransactionResponse> CreateTransaction(CreateTransactionRequest createTransactionRequest)
        {
            if (!createTransactionRequest.UserId.ToString().Equals(GetUserIdFromJwt())) throw new BadHttpRequestException(MessageConstant.UserMessage.NotAllowAction);
            var transaction = _mapper.Map<Transaction>(createTransactionRequest);
            if (createTransactionRequest.OrderId != null)
            {
                var token = GetJwtToken();
                var apiResponse = await CallApiUtils.CallApiEndpoint(HomeCleanApiEndPointConstant.Order.OrderEndpoint.Replace("{id}", createTransactionRequest.OrderId.ToString()), token);
                var order = await CallApiUtils.GenerateObjectFromResponse<OrderResponse>(apiResponse);
                if (order.Id == null || order.Id == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Order.OrderNotFound);
                transaction.Id = Guid.NewGuid();

                var paymentMethod = await _unitOfWork.GetRepository<PaymentMethod>().SingleOrDefaultAsync(predicate: x => x.Name.Equals(PaymenMethodEnum.PaymentMethodEnum.Wallet.ToString()));
                transaction.PaymentMethodId = paymentMethod.Id;
                transaction.Amount = order.TotalAmount.ToString();
                transaction.TransactionDate = DateTime.UtcNow.AddHours(7);
                transaction.CreatedAt = DateTime.UtcNow.AddHours(7);
                transaction.UpdatedAt = DateTime.UtcNow.AddHours(7);
                transaction.Code = order.Code;

                var category = await _unitOfWork.GetRepository<Category>().SingleOrDefaultAsync(predicate: x => x.Name.Equals(TransactionCategoryEnum.TransactionCategory.Spending.ToString()));
                transaction.CategoryId = category.Id;

                var userWallet = await _unitOfWork.GetRepository<UserWallet>()
                    .SingleOrDefaultAsync(predicate: x => x.UserId.Equals(createTransactionRequest.UserId) && x.WalletId.Equals(createTransactionRequest.WalletId),
                                  include: x => x.Include(x => x.Wallet));

                if (userWallet == null) throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotFound);

                var wallet = userWallet.Wallet;
                wallet.Balance = wallet.Balance - order.TotalAmount;
                _unitOfWork.GetRepository<Wallet>().UpdateAsync(wallet);
                if (await _unitOfWork.CommitAsync() <= 0)
                {
                    transaction.Status = TransactionEnum.TransactionStatus.Failed.ToString();
                    throw new DbUpdateException(MessageConstant.DataBase.DatabaseError);
                };
                transaction.Status = TransactionEnum.TransactionStatus.Success.ToString();
                await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);
                if (await _unitOfWork.CommitAsync() <= 0) throw new DbUpdateException(MessageConstant.DataBase.DatabaseError);
                if (wallet.Type.Equals(WalletEnum.WalletType.Shared.ToString()))
                {
                    var leaderId = wallet.OwnerId.ToString();
                    var message = $"User {createTransactionRequest.UserId} has made a transaction of {order.TotalAmount} from Shared Wallet.";

                    await _hubContext.Clients.User(leaderId).SendAsync("ReceiveTransactionNotification", message);
                }

            }
            return _mapper.Map<TransactionResponse>(transaction);
        }
    }
}
