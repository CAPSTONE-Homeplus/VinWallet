﻿using AutoMapper;
using Azure.Core;
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
        private readonly IVNPayService _vNPayService;

        public TransactionService(IUnitOfWork<VinWalletContext> unitOfWork, ILogger<TransactionService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, ISignalRHubService signalRHubService, IBackgroundJobClient backgroundJobClient, IWalletService walletService, IVNPayService vNPayService) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _signalRHubService = signalRHubService;
            _backgroundJobClient = backgroundJobClient;
            _walletService = walletService;
            _vNPayService = vNPayService;
        }


        public async Task<TransactionResponse> ProcessPayment(CreateTransactionRequest createTransactionRequest)
        {
            // TransactionResponse response = null;
            Transaction transaction = null;

            try
            {
                transaction = await PreHandle(createTransactionRequest);
                if (transaction == null)
                    throw new BadHttpRequestException(MessageConstant.TransactionMessage.CreateTransactionFailed);

                if (createTransactionRequest.OrderId != null)
                {
                    return await ProcessWalletPayment(createTransactionRequest, transaction);
                }
                else
                {
                    // var transaction = _mapper.Map<Transaction>(createTransactionRequest);
                    return await ProcessVNPayPayment(createTransactionRequest, transaction);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Payment processing failed: {ex.Message}");
                if (transaction != null)
                {
                    await UpdateTransactionStatus(transaction, TransactionEnum.TransactionStatus.Failed);
                }
                throw;
            }
        }

        public async Task<TransactionResponse> CreateTransaction(CreateTransactionRequest createTransactionRequest)
        {
            //if (!createTransactionRequest.UserId.ToString().Equals(GetUserIdFromJwt())) throw new BadHttpRequestException(MessageConstant.UserMessage.NotAllowAction);
            //var transaction = _mapper.Map<Transaction>(createTransactionRequest);
            //if (createTransactionRequest.OrderId != null)
            //{
            //    var token = GetJwtToken();
            //    var apiResponse = await CallApiUtils.CallApiEndpoint(HomeCleanApiEndPointConstant.Order.OrderEndpoint.Replace("{id}", createTransactionRequest.OrderId.ToString()), token);
            //    var order = await CallApiUtils.GenerateObjectFromResponse<OrderResponse>(apiResponse);
            //    if (order.Id == null || order.Id == Guid.Empty) throw new BadHttpRequestException(MessageConstant.Order.OrderNotFound);
            //    transaction.Id = Guid.NewGuid();

            //    var paymentMethod = await _unitOfWork.GetRepository<PaymentMethod>().SingleOrDefaultAsync(predicate: x => x.Name.Equals(PaymenMethodEnum.PaymentMethodEnum.Wallet.ToString()));
            //    transaction.PaymentMethodId = paymentMethod.Id;
            //    transaction.Amount = order.TotalAmount.ToString();
            //    transaction.TransactionDate = DateTime.UtcNow.AddHours(7);
            //    transaction.CreatedAt = DateTime.UtcNow.AddHours(7);
            //    transaction.UpdatedAt = DateTime.UtcNow.AddHours(7);
            //    transaction.Code = order.Code;
            //    transaction.Status = TransactionEnum.TransactionStatus.Pending.ToString();

            //    var category = await _unitOfWork.GetRepository<Category>().SingleOrDefaultAsync(predicate: x => x.Name.Equals(TransactionCategoryEnum.TransactionCategory.Spending.ToString()));
            //    transaction.CategoryId = category.Id;

            //    var userWallet = await _unitOfWork.GetRepository<UserWallet>()
            //        .SingleOrDefaultAsync(predicate: x => x.UserId.Equals(createTransactionRequest.UserId) && x.WalletId.Equals(createTransactionRequest.WalletId),
            //                      include: x => x.Include(x => x.Wallet));

            //    if (userWallet == null) throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotFound);

            //    var wallet = userWallet.Wallet;
            //    if (await _unitOfWork.CommitAsync() <= 0)
            //    {
            //        transaction.Status = TransactionEnum.TransactionStatus.Failed.ToString();
            //        throw new DbUpdateException(MessageConstant.DataBase.DatabaseError);
            //    };
            //    transaction.Status = TransactionEnum.TransactionStatus.Success.ToString();
            //    await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);
            //    if (await _unitOfWork.CommitAsync() <= 0) throw new DbUpdateException(MessageConstant.DataBase.DatabaseError);
            //    if (wallet.Type.Equals(WalletEnum.WalletType.Shared.ToString()))
            //    {
            //        var leaderId = wallet.OwnerId.ToString();
            //        var message = $"User {createTransactionRequest.UserId} has made a transaction of {order.TotalAmount} from Shared Wallet.";
            //        _backgroundJobClient.Enqueue(() => _signalRHubService.SendNotificationToUser(leaderId, message));
            //    }
            //}
            //else
            //{
            //    transaction.Id = Guid.NewGuid();
            //    transaction.TransactionDate = DateTime.UtcNow.AddHours(7);
            //    transaction.CreatedAt = DateTime.UtcNow.AddHours(7);
            //    transaction.UpdatedAt = DateTime.UtcNow.AddHours(7);
            //    transaction.Status = TransactionEnum.TransactionStatus.Pending.ToString();
            //    transaction.Code = DateTime.Now.Ticks.ToString();
            //    var paymentMethod = await _unitOfWork.GetRepository<PaymentMethod>().SingleOrDefaultAsync(predicate: x => x.Name.Equals(PaymenMethodEnum.PaymentMethodEnum.Wallet.ToString()));
            //    transaction.PaymentMethodId = paymentMethod.Id;
            //    var category = await _unitOfWork.GetRepository<Category>().SingleOrDefaultAsync(predicate: x => x.Name.Equals(TransactionCategoryEnum.TransactionCategory.Deposit.ToString()));
            //    transaction.CategoryId = category.Id;
            //    transaction.Amount = createTransactionRequest.Amount;
            //    transaction.Type = TransactionEnum.TransactionType.Deposit.ToString();

            //    await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);
            //    if (await _unitOfWork.CommitAsync() <= 0) throw new DbUpdateException(MessageConstant.DataBase.DatabaseError);
            //}
            //return _mapper.Map<TransactionResponse>(transaction);

            //if (!createTransactionRequest.UserId.ToString().Equals(GetUserIdFromJwt()))
            //    throw new BadHttpRequestException(MessageConstant.UserMessage.NotAllowAction);
            //var userWallet = await ValidateAndGetUserWallet(createTransactionRequest.UserId, createTransactionRequest.WalletId);

            //var transaction = await InitializeBaseTransaction(createTransactionRequest);
            //try
            //{
            //    if (createTransactionRequest.OrderId != null)
            //    {
            //        await HandleOrderTransaction(transaction, createTransactionRequest, userWallet.Wallet);
            //    }
            //    else
            //    {    
            //        await HandleDepositTransaction(transaction, createTransactionRequest);
            //    }
            //    await SaveTransaction(transaction);

            //    await HandleSharedWalletNotification(transaction, userWallet.Wallet);

            //    return _mapper.Map<TransactionResponse>(transaction);
            //}
            //catch (Exception ex)
            //{
            //    transaction.Status = TransactionEnum.TransactionStatus.Failed.ToString();
            //    await _unitOfWork.CommitAsync();
            //    throw;
            //}

            try 
            {
                var transaction = await PreHandle(createTransactionRequest);
                return _mapper.Map<TransactionResponse>(transaction);
            }
            catch (Exception ex)
            {
                
            }
            return null;

        }

        private async Task<Transaction> PreHandle(CreateTransactionRequest createTransactionRequest)
        {
            if (!createTransactionRequest.UserId.ToString().Equals(GetUserIdFromJwt()))
                throw new BadHttpRequestException(MessageConstant.UserMessage.NotAllowAction);
            var userWallet = await ValidateAndGetUserWallet(createTransactionRequest.UserId, createTransactionRequest.WalletId);

            var transaction = await InitializeBaseTransaction(createTransactionRequest);
            try
            {
                if (createTransactionRequest.OrderId != null)
                {
                    await HandleOrderTransaction(transaction, createTransactionRequest);
                }
                else
                {
                    await HandleDepositTransaction(transaction, createTransactionRequest);
                }
                await SaveTransaction(transaction);

                await HandleSharedWalletNotification(transaction, userWallet.Wallet);

                return transaction;
            }
            catch (Exception ex)
            {
                transaction.Status = TransactionEnum.TransactionStatus.Failed.ToString();
                await _unitOfWork.CommitAsync();
                throw;
            }
        }

        private async Task SaveTransaction(Transaction transaction)
        {
            var x = await _unitOfWork.GetRepository<Transaction>().InsertAsync(transaction);
            if (await _unitOfWork.CommitAsync() <= 0)
                throw new DbUpdateException(MessageConstant.DataBase.DatabaseError);
        }
        private async Task HandleSharedWalletNotification(Transaction transaction, Wallet wallet)
        {
            if (wallet.Type.Equals(WalletEnum.WalletType.Shared.ToString()))
            {
                var leaderId = wallet.OwnerId.ToString();
                var message = $"User {transaction.UserId} has made a transaction of {transaction.Amount} from Shared Wallet.";
                _backgroundJobClient.Enqueue(() => _signalRHubService.SendNotificationToUser(leaderId, message));
            }
        }

        private async Task<OrderResponse> GetOrderDetails(Guid orderId)
        {
            var token = GetJwtToken();
            var apiResponse = await CallApiUtils.CallApiEndpoint(
                HomeCleanApiEndPointConstant.Order.OrderEndpoint.Replace("{id}", orderId.ToString()),
                token
            );
            var order = await CallApiUtils.GenerateObjectFromResponse<OrderResponse>(apiResponse);

            if (order.Id == null || order.Id == Guid.Empty)
                throw new BadHttpRequestException(MessageConstant.Order.OrderNotFound);

            return order;
        }

        private async Task<UserWallet> ValidateAndGetUserWallet(Guid? userId, Guid walletId)
        {
            var userWallet = await _unitOfWork.GetRepository<UserWallet>()
                .SingleOrDefaultAsync(
                    predicate: x => x.UserId.Equals(userId) && x.WalletId.Equals(walletId),
                    include: x => x.Include(x => x.Wallet)
                );

            if (userWallet == null)
                throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotFound);

            return userWallet;
        }

        private async Task HandleDepositTransaction(Transaction transaction, CreateTransactionRequest request)
        {
            transaction.Code = DateTime.Now.Ticks.ToString();
            var category = await _unitOfWork.GetRepository<Category>()
                .SingleOrDefaultAsync(predicate: x => x.Name.Equals(TransactionCategoryEnum.TransactionCategory.Deposit.ToString()));
            transaction.CategoryId = category.Id;
            transaction.Amount = request.Amount;
            transaction.Type = TransactionEnum.TransactionType.Deposit.ToString();
        }
        private async Task HandleOrderTransaction(Transaction transaction, CreateTransactionRequest request)
        {
            
            var order = await GetOrderDetails(request.OrderId.Value);
            transaction.Amount = order.TotalAmount.ToString();
            transaction.Code = order.Code;
            var category = await _unitOfWork.GetRepository<Category>()
                .SingleOrDefaultAsync(predicate: x => x.Name.Equals(TransactionCategoryEnum.TransactionCategory.Spending.ToString()));
            transaction.CategoryId = category.Id;
            transaction.Type = TransactionEnum.TransactionType.Spending.ToString();
        }

        private async Task<Transaction> InitializeBaseTransaction(CreateTransactionRequest request)
        {
            var transaction = _mapper.Map<Transaction>(request);
            transaction.Id = Guid.NewGuid();
            transaction.TransactionDate = DateTime.UtcNow.AddHours(7);
            transaction.CreatedAt = DateTime.UtcNow.AddHours(7);
            transaction.UpdatedAt = DateTime.UtcNow.AddHours(7);
            transaction.Status = TransactionEnum.TransactionStatus.Pending.ToString();

            var paymentMethod = await _unitOfWork.GetRepository<PaymentMethod>()
                .SingleOrDefaultAsync(predicate: x => x.Name.Equals(PaymenMethodEnum.PaymentMethodEnum.Wallet.ToString()));
            transaction.PaymentMethodId = paymentMethod.Id;

            return transaction;
        }

        private async Task<TransactionResponse> ProcessWalletPayment(CreateTransactionRequest request, Transaction transaction)
        {
            var success = await _walletService.UpdateWalletBalance(
                request.WalletId,
                transaction.Amount,
                TransactionCategoryEnum.TransactionCategory.Spending
            );

            var status = success ?
                TransactionEnum.TransactionStatus.Success :
                TransactionEnum.TransactionStatus.Failed;

            await UpdateTransactionStatus(transaction, status);

            transaction.Status = status.ToString();
            return _mapper.Map<TransactionResponse>(transaction);
        }


        private async Task<TransactionResponse> ProcessVNPayPayment(CreateTransactionRequest request, Transaction transaction)
        {
            try
            {               
                var paymentUrl = _vNPayService.GeneratePaymentUrl(
                    request.Amount,
                    transaction.Id.ToString()
                );

                await UpdateTransactionPaymentUrl(transaction, paymentUrl);
                transaction.PaymentUrl = paymentUrl;
                return _mapper.Map<TransactionResponse>(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError($"VNPay payment URL generation failed: {ex.Message}");
                await UpdateTransactionStatus(transaction, TransactionEnum.TransactionStatus.Failed);
                throw;
            }
        }

        private async Task UpdateTransactionPaymentUrl(Transaction transaction, string paymentUrl)
        {
            //var transaction = await _unitOfWork.GetRepository<Transaction>()
            //    .SingleOrDefaultAsync(predicate: x => x.Id.Equals(transactionId));

            if (transaction != null)
            {

                transaction.PaymentUrl = paymentUrl;
                transaction.UpdatedAt = DateTime.UtcNow.AddHours(7);
                _unitOfWork.GetRepository<Transaction>().UpdateAsync(transaction);
                await _unitOfWork.CommitAsync();
            }
        }



        public async Task<bool> UpdateTransactionStatus(Transaction transaction, TransactionEnum.TransactionStatus transactionStatus)
        {
            transaction.Status = transactionStatus.ToString();
            transaction.UpdatedAt = DateTime.UtcNow.AddHours(7);
            _unitOfWork.GetRepository<Transaction>().UpdateAsync(transaction);
            if (await _unitOfWork.CommitAsync() <= 0) return false;
            return true;
        }
    }
}
