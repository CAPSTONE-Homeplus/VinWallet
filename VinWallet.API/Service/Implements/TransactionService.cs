using AutoMapper;
using Hangfire;
using Microsoft.EntityFrameworkCore;
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
        private readonly ISignalRHubService _signalRHubService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IWalletService _walletService;

        public TransactionService(IUnitOfWork<VinWalletContext> unitOfWork, ILogger<TransactionService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, ISignalRHubService signalRHubService, IBackgroundJobClient backgroundJobClient, IWalletService walletService) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _signalRHubService = signalRHubService;
            _backgroundJobClient = backgroundJobClient;
            _walletService = walletService;
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
                transaction.Status = TransactionEnum.TransactionStatus.Pending.ToString();

                var category = await _unitOfWork.GetRepository<Category>().SingleOrDefaultAsync(predicate: x => x.Name.Equals(TransactionCategoryEnum.TransactionCategory.Spending.ToString()));
                transaction.CategoryId = category.Id;

                var userWallet = await _unitOfWork.GetRepository<UserWallet>()
                    .SingleOrDefaultAsync(predicate: x => x.UserId.Equals(createTransactionRequest.UserId) && x.WalletId.Equals(createTransactionRequest.WalletId),
                                  include: x => x.Include(x => x.Wallet));

                if (userWallet == null) throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotFound);

                var wallet = userWallet.Wallet;
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
                    _backgroundJobClient.Enqueue(() => _signalRHubService.SendNotificationToUser(leaderId, message));
                }
            }
            return _mapper.Map<TransactionResponse>(transaction);
        }



        public async Task<bool> ProcessPayment(CreateTransactionRequest createTransactionRequest)
        {
            if (createTransactionRequest.OrderId != null)
            {
                var response = await CreateTransaction(createTransactionRequest);
                var success = await _walletService.UpdateWalletBalance(createTransactionRequest.WalletId, createTransactionRequest.Amount, TransactionCategoryEnum.TransactionCategory.Spending);
                var status = success == true ? TransactionEnum.TransactionStatus.Success : TransactionEnum.TransactionStatus.Failed;
                _backgroundJobClient.Enqueue(() => UpdateTransactionStatus(response.Id, status));
                return success;
            }
            else
            {
                var response = await CreateTransaction(createTransactionRequest);
                if (response == null) return false;
                //VNPAy
                return true;
            }
        }



        public async Task<bool> UpdateTransactionStatus(Guid id, TransactionEnum.TransactionStatus transactionStatus)
        {
            if (id == Guid.Empty) throw new BadHttpRequestException(MessageConstant.TransactionMessage.EmptyTransactionId);
            var transaction =await _unitOfWork.GetRepository<Transaction>().SingleOrDefaultAsync(predicate: x => x.Id.Equals(id));
            if (transaction == null) throw new BadHttpRequestException(MessageConstant.TransactionMessage.TransactionNotFound);
            transaction.Status = transactionStatus.ToString();
            transaction.UpdatedAt = DateTime.UtcNow.AddHours(7);
            _unitOfWork.GetRepository<Transaction>().UpdateAsync(transaction);
            if (await _unitOfWork.CommitAsync() <= 0) return false;
            return true;
        }
    }
}
