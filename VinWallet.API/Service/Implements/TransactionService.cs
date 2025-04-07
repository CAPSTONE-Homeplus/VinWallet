using AutoMapper;
using Azure.Core;
using Hangfire;
using HomeClean.API.Service.Implements.RabbitMQ;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using VinWallet.API.Service.Interfaces;
using VinWallet.API.Service.RabbitMQ;
using VinWallet.Domain.Models;
using VinWallet.Domain.Paginate;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Enums;
using VinWallet.Repository.Generic.Interfaces;
using VinWallet.Repository.Payload.Request.TransactionRequest;
using VinWallet.Repository.Payload.Response.AnalystResponse;
using VinWallet.Repository.Payload.Response.OrderResponse;
using VinWallet.Repository.Payload.Response.TransactionResponse;
using VinWallet.Repository.Utils;
using static VinWallet.Repository.Constants.HomeCleanApiEndPointConstant;

namespace VinWallet.API.Service.Implements
{
    public class TransactionService : BaseService<TransactionService>, ITransactionService
    {
        private readonly ISignalRHubService _signalRHubService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IWalletService _walletService;
        private readonly IVNPayService _vNPayService;
        private readonly RabbitMQPublisher _rabbitMQPublisher;
        public TransactionService(IUnitOfWork<VinWalletContext> unitOfWork, ILogger<TransactionService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, ISignalRHubService signalRHubService, IBackgroundJobClient backgroundJobClient, RabbitMQPublisher rabbitMQPublisher, IWalletService walletService, IVNPayService vNPayService) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _signalRHubService = signalRHubService;
            _backgroundJobClient = backgroundJobClient;
            _walletService = walletService;
            _vNPayService = vNPayService;
            _rabbitMQPublisher = rabbitMQPublisher;
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
            var transaction = await PreHandle(createTransactionRequest);
            if (transaction == null)
                throw new BadHttpRequestException(MessageConstant.TransactionMessage.CreateTransactionFailed);
            return _mapper.Map<TransactionResponse>(transaction);

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


                if (createTransactionRequest.ServiceType == ServiceType.HomeClean)
                {
                    await _rabbitMQPublisher.Publish("payment_success", "homeclean", transaction.OrderId);
                }
                else if (createTransactionRequest.ServiceType == ServiceType.Laundry)
                {
                    await _rabbitMQPublisher.Publish("payment_success", "vinlaundy", transaction.OrderId);
                }

                //await _rabbitMQPublisher.Publish("payment_success", "homeclean" ,transaction.OrderId);
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
        private async Task<OrderResponseForPaymentLaundry> GetOrderDetailsLaundry(Guid orderId)
        {
            var response = await CallApiUtils.CallApiEndpoint(
                VinLaundryApiEndPointConstant.Order.OrderEndpointForPayment.Replace("{id}", orderId.ToString())
            );
            var order = await CallApiUtils.GenerateObjectFromResponse<OrderResponseForPaymentLaundry>(response);
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
            try
            {
                if(request.OrderId != null && request.ServiceType != null)
                {
                    switch (request.ServiceType)
                    {
                        case ServiceType.HomeClean:
                            var order = await GetOrderDetails(request.OrderId.Value);
                            transaction.Amount = order.TotalAmount.ToString();
                            var randomHC = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper(); 
                            transaction.Code = $"HC-{DateTime.UtcNow.AddHours(7):yyyyMMddHHmmssfff}-{randomHC}";
                            var category = await _unitOfWork.GetRepository<Category>()
                                .SingleOrDefaultAsync(predicate: x => x.Name.Equals(TransactionCategoryEnum.TransactionCategory.Spending.ToString()));
                            transaction.CategoryId = category.Id;
                            transaction.Type = TransactionEnum.TransactionType.Spending.ToString();
                            break;

                        case ServiceType.Laundry:
                            //Viết ở đây
                            var orderLaundry = await GetOrderDetailsLaundry(request.OrderId.Value);
                            transaction.Amount = orderLaundry.TotalAmount.ToString();
                            var categoryLaundry = await _unitOfWork.GetRepository<Category>()
                                .SingleOrDefaultAsync(predicate: x => x.Name.Equals(TransactionCategoryEnum.TransactionCategory.Spending.ToString()));
                            var randomLD = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper(); 
                            transaction.Code = $"LD-{DateTime.UtcNow.AddHours(7):yyyyMMddHHmmssfff}-{randomLD}";
                            transaction.CategoryId = categoryLaundry.Id;
                            transaction.Type = TransactionEnum.TransactionType.Spending.ToString();
                            break;

                        default:
                            throw new BadHttpRequestException("Unsupported service type");
                    }
                }
                
            }
            catch (Exception ex)
            {
                throw new BadHttpRequestException(MessageConstant.Order.OrderNotFound);

            }
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

        public async Task<IPaginate<GetTransactionResponse>> GetTransactionByUserId(Guid userId, string? search, string? orderBy, int page, int size)
        {
            if (userId == Guid.Empty)
                throw new BadHttpRequestException(MessageConstant.UserMessage.EmptyUserId);

            if (!userId.ToString().Equals(GetUserIdFromJwt()))
                throw new BadHttpRequestException(MessageConstant.UserMessage.NotAllowAction);

            Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>> orderByFunc = x => x.OrderByDescending(y => y.CreatedAt);

            var transactions = await _unitOfWork.GetRepository<Transaction>().GetPagingListAsync(
                selector: x => new GetTransactionResponse(
                    x.Id,
                    x.WalletId,
                    x.UserId,
                    x.PaymentMethodId,
                    x.Amount,
                    x.Type,
                    x.PaymentUrl,
                    x.Note,
                    x.TransactionDate,
                    x.Status,
                    x.CreatedAt,
                    x.UpdatedAt,
                    x.Code,
                    x.CategoryId,
                    x.OrderId
                ),
                predicate: x => x.UserId == userId &&
                                 (string.IsNullOrEmpty(search) || x.Code.Contains(search) || x.Type.Contains(search)),
                orderBy: orderByFunc,
                page: page,
                size: size
            );

            return transactions;
        }

        public async Task<IPaginate<GetTransactionResponse>> GetTransactionByUserIdAndWalletId(Guid userId, Guid walletId, string? search, string? orderBy, int page, int size)
        {
            if (userId == Guid.Empty)
                throw new BadHttpRequestException(MessageConstant.UserMessage.EmptyUserId);
            if (walletId == Guid.Empty)
                throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotFound);

            if (!userId.ToString().Equals(GetUserIdFromJwt()))
                throw new BadHttpRequestException(MessageConstant.UserMessage.NotAllowAction);
            Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>> orderByFunc = x => x.OrderByDescending(y => y.CreatedAt);
            var transactions = await _unitOfWork.GetRepository<Transaction>().GetPagingListAsync(
                selector: x => new GetTransactionResponse(
                    x.Id,
                    x.WalletId,
                    x.UserId,
                    x.PaymentMethodId,
                    x.Amount,
                    x.Type,
                    x.PaymentUrl,
                    x.Note,
                    x.TransactionDate,
                    x.Status,
                    x.CreatedAt,
                    x.UpdatedAt,
                    x.Code,
                    x.CategoryId,
                    x.OrderId
                ),
                predicate: x => x.UserId == userId && x.WalletId == walletId &&
                                 (string.IsNullOrEmpty(search) || x.Code.Contains(search) || x.Type.Contains(search)),
                orderBy: orderByFunc,
                page: page,
                size: size
            );

            return transactions;
        }

        public async Task<IPaginate<GetTransactionResponse>> GetTransactionByWalletId(Guid walletId, string? search, string? orderBy, int page, int size)
        {
            if (walletId == Guid.Empty)
                throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotFound);

            var wallet = await _unitOfWork.GetRepository<Wallet>().SingleOrDefaultAsync(predicate: x => x.Id == walletId);
            if (wallet == null)
                throw new BadHttpRequestException(MessageConstant.WalletMessage.WalletNotFound);

            //if (!wallet.OwnerId.ToString().Equals(GetUserIdFromJwt()))
            //    throw new BadHttpRequestException(MessageConstant.UserMessage.NotAllowAction);

            Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>> orderByFunc = x => x.OrderByDescending(y => y.CreatedAt);
            var transactions = await _unitOfWork.GetRepository<Transaction>().GetPagingListAsync(
                               selector: x => new GetTransactionResponse(
                                                      x.Id,
                                                                         x.WalletId,
                                                                                            x.UserId,
                                                                                                               x.PaymentMethodId,
                                                                                                                                  x.Amount,
                                                                                                                                                     x.Type,
                                                                                                                                                                        x.PaymentUrl,
                                                                                                                                                                                           x.Note,
                                                                                                                                                                                                              x.TransactionDate,
                                                                                                                                                                                                                                 x.Status,
                                                                                                                                                                                                                                                    x.CreatedAt,
                                                                                                                                                                                                                                                                       x.UpdatedAt,
                                                                                                                                                                                                                                                                                          x.Code,
                                                                                                                                                                                                                                                                                                             x.CategoryId,
                                                                                                                                                                                                                                                                                                                                x.OrderId
                                                                                                                                                                                                                                                                                                                                               ),
                                              predicate: x => x.WalletId == walletId &&
                                                                              (string.IsNullOrEmpty(search) || x.Code.Contains(search) || x.Type.Contains(search)),
                                                             orderBy: orderByFunc,
                                                                            page: page,
                                                                                           size: size
                                                                                                      );

            return transactions;
        }

        public async Task<GetTransactionResponse> GetTransactionById(Guid transactionId)
        {
            if (transactionId == Guid.Empty)
                throw new BadHttpRequestException(MessageConstant.TransactionMessage.TransactionNotFound);

            var transaction = await _unitOfWork.GetRepository<Transaction>().SingleOrDefaultAsync(
                predicate: x => x.Id == transactionId,
                include: x => x.Include(y => y.Category).Include(y => y.PaymentMethod)
            );

            if (transaction == null)
                throw new BadHttpRequestException(MessageConstant.TransactionMessage.TransactionNotFound);

            // Check if the user has access to this transaction
            if (!transaction.UserId.ToString().Equals(GetUserIdFromJwt()))
                throw new BadHttpRequestException(MessageConstant.UserMessage.NotAllowAction);

            return new GetTransactionResponse(
                transaction.Id,
                transaction.WalletId,
                transaction.UserId,
                transaction.PaymentMethodId,
                transaction.Amount,
                transaction.Type,
                transaction.PaymentUrl,
                transaction.Note,
                transaction.TransactionDate,
                transaction.Status,
                transaction.CreatedAt,
                transaction.UpdatedAt,
                transaction.Code,
                transaction.CategoryId,
                transaction.OrderId
            );
        }

        public async Task<IPaginate<GetTransactionResponse>> GetAllTransaction(string? search, string? orderBy, int page, int size)
        {


            Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>> orderByFunc;

            if (!string.IsNullOrEmpty(orderBy))
            {
                switch (orderBy.ToLower())
                {
                    case "date_asc":
                        orderByFunc = x => x.OrderBy(y => y.TransactionDate);
                        break;
                    case "date_desc":
                        orderByFunc = x => x.OrderByDescending(y => y.TransactionDate);
                        break;
                    case "amount_asc":
                        orderByFunc = x => x.OrderBy(y => Convert.ToDecimal(y.Amount));
                        break;
                    case "amount_desc":
                        orderByFunc = x => x.OrderByDescending(y => Convert.ToDecimal(y.Amount));
                        break;
                    default:
                        orderByFunc = x => x.OrderByDescending(y => y.CreatedAt);
                        break;
                }
            }
            else
            {
                orderByFunc = x => x.OrderByDescending(y => y.CreatedAt);
            }

            // Query all transactions with search filter
            var transactions = await _unitOfWork.GetRepository<Transaction>().GetPagingListAsync(
                selector: x => new GetTransactionResponse(
                    x.Id,
                    x.WalletId,
                    x.UserId,
                    x.PaymentMethodId,
                    x.Amount,
                    x.Type,
                    x.PaymentUrl,
                    x.Note,
                    x.TransactionDate,
                    x.Status,
                    x.CreatedAt,
                    x.UpdatedAt,
                    x.Code,
                    x.CategoryId,
                    x.OrderId
                ),
                predicate: x => string.IsNullOrEmpty(search) ||
                             x.Code.Contains(search) ||
                             x.Type.Contains(search) ||
                             x.Status.Contains(search) ||
                             x.Note.Contains(search),
                orderBy: orderByFunc,
                page: page,
                size: size
            );

            return transactions;
        }

        public async Task<List<TransactionChartData>> GetTransactionsByTimePeriod(Guid userId, DateTime startDate, DateTime endDate, string groupBy = "day")
        {
            if (!userId.ToString().Equals(GetUserIdFromJwt()))
                throw new BadHttpRequestException(MessageConstant.UserMessage.NotAllowAction);

            var transactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
                predicate: x => x.UserId.Equals(userId) &&
                                x.TransactionDate >= startDate &&
                                x.TransactionDate <= endDate,
                orderBy: x => x.OrderBy(y => y.TransactionDate)
            );

            var result = new List<TransactionChartData>();

            // Group data by day, week, or month
            switch (groupBy.ToLower())
            {
                case "day":
                    var dailyGroups = transactions
                        .GroupBy(t => t.TransactionDate.Value.Date);

                    foreach (var group in dailyGroups)
                    {
                        result.Add(new TransactionChartData
                        {
                            TimeLabel = group.Key.ToString("yyyy-MM-dd"),
                            Count = group.Count(),
                            TotalAmount = group.Sum(t => decimal.Parse(t.Amount))
                        });
                    }
                    break;

                case "week":
                    var weeklyGroups = transactions
                        .GroupBy(t => new {
                            Year = t.TransactionDate.Value.Year,
                            Week = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                                t.TransactionDate.Value, CalendarWeekRule.FirstDay, DayOfWeek.Monday)
                        });

                    foreach (var group in weeklyGroups)
                    {
                        result.Add(new TransactionChartData
                        {
                            TimeLabel = $"{group.Key.Year}-W{group.Key.Week}",
                            Count = group.Count(),
                            TotalAmount = group.Sum(t => decimal.Parse(t.Amount))
                        });
                    }
                    break;

                case "month":
                    var monthlyGroups = transactions
                        .GroupBy(t => new { t.TransactionDate.Value.Year, t.TransactionDate.Value.Month });

                    foreach (var group in monthlyGroups)
                    {
                        result.Add(new TransactionChartData
                        {
                            TimeLabel = $"{group.Key.Year}-{group.Key.Month:D2}",
                            Count = group.Count(),
                            TotalAmount = group.Sum(t => decimal.Parse(t.Amount))
                        });
                    }
                    break;
            }

            return result;
        }

        // 2. Lấy dữ liệu phân bổ theo loại giao dịch
        public async Task<List<TransactionTypeDistribution>> GetTransactionTypeDistribution(Guid userId)
        {
            if (!userId.ToString().Equals(GetUserIdFromJwt()))
                throw new BadHttpRequestException(MessageConstant.UserMessage.NotAllowAction);

            var transactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
                predicate: x => x.UserId.Equals(userId)
            );

            var result = transactions
                .GroupBy(t => t.Type)
                .Select(g => new TransactionTypeDistribution
                {
                    Type = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(t => decimal.Parse(t.Amount))
                })
                .ToList();

            return result;
        }

        // 3. Lấy dữ liệu trạng thái giao dịch
        public async Task<List<TransactionStatusDistribution>> GetTransactionStatusDistribution(Guid userId)
        {
            if (!userId.ToString().Equals(GetUserIdFromJwt()))
                throw new BadHttpRequestException(MessageConstant.UserMessage.NotAllowAction);

            var transactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
                predicate: x => x.UserId.Equals(userId)
            );

            var result = transactions
                .GroupBy(t => t.Status)
                .Select(g => new TransactionStatusDistribution
                {
                    Status = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / transactions.Count * 100
                })
                .ToList();

            return result;
        }

        // 4. So sánh giao dịch giữa các ví
        public async Task<List<WalletTransactionComparison>> CompareWalletTransactions(Guid userId)
        {
            if (!userId.ToString().Equals(GetUserIdFromJwt()))
                throw new BadHttpRequestException(MessageConstant.UserMessage.NotAllowAction);

            // Lấy tất cả ví của người dùng
            var userWallets = await _unitOfWork.GetRepository<UserWallet>().GetListAsync(
                predicate: x => x.UserId.Equals(userId),
                include: x => x.Include(uw => uw.Wallet)
            );

            var result = new List<WalletTransactionComparison>();

            foreach (var userWallet in userWallets)
            {
                var transactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
                    predicate: x => x.WalletId.Equals(userWallet.WalletId)
                );

                var walletData = new WalletTransactionComparison
                {
                    WalletId = userWallet.WalletId.Value,
                    WalletName = userWallet.Wallet.Name,
                    TransactionCount = transactions.Count,
                    TotalDeposit = transactions
                        .Where(t => t.Type == TransactionEnum.TransactionType.Deposit.ToString())
                        .Sum(t => decimal.Parse(t.Amount)),
                    TotalSpending = transactions
                        .Where(t => t.Type == TransactionEnum.TransactionType.Spending.ToString())
                        .Sum(t => decimal.Parse(t.Amount))
                };

                result.Add(walletData);
            }

            return result;
        }

        // 5. Xu hướng chi tiêu/nạp tiền theo thời gian
        public async Task<List<SpendingDepositTrend>> GetSpendingDepositTrend(Guid userId, DateTime startDate, DateTime endDate, string interval = "month")
        {
            if (!userId.ToString().Equals(GetUserIdFromJwt()))
                throw new BadHttpRequestException(MessageConstant.UserMessage.NotAllowAction);

            var transactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
                predicate: x => x.UserId.Equals(userId) &&
                                x.TransactionDate >= startDate &&
                                x.TransactionDate <= endDate,
                orderBy: x => x.OrderBy(y => y.TransactionDate)
            );

            var result = new List<SpendingDepositTrend>();

            // Define the grouping process based on interval
            switch (interval.ToLower())
            {
                case "week":
                    var weeklyGroups = transactions
                        .GroupBy(t => new {
                            Year = t.TransactionDate.Value.Year,
                            Week = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                                t.TransactionDate.Value, CalendarWeekRule.FirstDay, DayOfWeek.Monday)
                        });

                    foreach (var group in weeklyGroups)
                    {
                        var timeLabel = $"{group.Key.Year}-W{group.Key.Week}";
                        var trendData = new SpendingDepositTrend
                        {
                            TimeLabel = timeLabel,
                            DepositAmount = group
                                .Where(t => t.Type == TransactionEnum.TransactionType.Deposit.ToString())
                                .Sum(t => decimal.Parse(t.Amount)),
                            SpendingAmount = group
                                .Where(t => t.Type == TransactionEnum.TransactionType.Spending.ToString())
                                .Sum(t => decimal.Parse(t.Amount))
                        };
                        result.Add(trendData);
                    }
                    break;

                case "day":
                    var dailyGroups = transactions
                        .GroupBy(t => t.TransactionDate.Value.Date);

                    foreach (var group in dailyGroups)
                    {
                        var timeLabel = group.Key.ToString("yyyy-MM-dd");
                        var trendData = new SpendingDepositTrend
                        {
                            TimeLabel = timeLabel,
                            DepositAmount = group
                                .Where(t => t.Type == TransactionEnum.TransactionType.Deposit.ToString())
                                .Sum(t => decimal.Parse(t.Amount)),
                            SpendingAmount = group
                                .Where(t => t.Type == TransactionEnum.TransactionType.Spending.ToString())
                                .Sum(t => decimal.Parse(t.Amount))
                        };
                        result.Add(trendData);
                    }
                    break;

                default: // month
                    var monthlyGroups = transactions
                        .GroupBy(t => new { t.TransactionDate.Value.Year, t.TransactionDate.Value.Month });

                    foreach (var group in monthlyGroups)
                    {
                        var timeLabel = $"{group.Key.Year}-{group.Key.Month:D2}";
                        var trendData = new SpendingDepositTrend
                        {
                            TimeLabel = timeLabel,
                            DepositAmount = group
                                .Where(t => t.Type == TransactionEnum.TransactionType.Deposit.ToString())
                                .Sum(t => decimal.Parse(t.Amount)),
                            SpendingAmount = group
                                .Where(t => t.Type == TransactionEnum.TransactionType.Spending.ToString())
                                .Sum(t => decimal.Parse(t.Amount))
                        };
                        result.Add(trendData);
                    }
                    break;
            }

            return result;
        }

      

        // 1. Lấy tổng quan về giao dịch trong hệ thống
        public async Task<AdminDashboardOverview> GetAdminDashboardOverview(DateTime fromDate, DateTime toDate, string type = null)
        {

            // Lấy danh sách tất cả giao dịch dựa trên khoảng thời gian và type
            var allTransactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
                predicate: x => x.TransactionDate >= fromDate && x.TransactionDate <= toDate &&
                               (string.IsNullOrEmpty(type) || x.Type == type)
            );

            // Lấy danh sách ví
            var wallets = await _unitOfWork.GetRepository<Wallet>().GetListAsync();

            // Lấy danh sách người dùng
            var users = await _unitOfWork.GetRepository<User>().GetListAsync();

            // Tính toán số liệu tổng quan
            var overview = new AdminDashboardOverview
            {
                FromDate = fromDate,
                ToDate = toDate,
                TransactionType = type,
                TotalTransactions = allTransactions.Count,
                TotalAmount = allTransactions.Sum(t => decimal.Parse(t.Amount)),
                TotalUsers = users.Count,
                TotalWallets = wallets.Count,
                TotalDeposit = allTransactions
                    .Where(t => t.Type == TransactionEnum.TransactionType.Deposit.ToString() &&
                           t.Status == TransactionEnum.TransactionStatus.Success.ToString())
                    .Sum(t => decimal.Parse(t.Amount)),
                TotalSpending = allTransactions
                    .Where(t => t.Type == TransactionEnum.TransactionType.Spending.ToString() &&
                           t.Status == TransactionEnum.TransactionStatus.Success.ToString())
                    .Sum(t => decimal.Parse(t.Amount)),
                SuccessfulTransactions = allTransactions
                    .Count(t => t.Status == TransactionEnum.TransactionStatus.Success.ToString()),
                FailedTransactions = allTransactions
                    .Count(t => t.Status == TransactionEnum.TransactionStatus.Failed.ToString()),
                PendingTransactions = allTransactions
                    .Count(t => t.Status == TransactionEnum.TransactionStatus.Pending.ToString())
            };

            return overview;
        }

        // 2. Lấy thống kê giao dịch theo khoảng thời gian
        public async Task<AdminTransactionStats> GetAdminTransactionStats(DateTime fromDate, DateTime toDate, string type = null)
        {
           

            var transactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
                predicate: x => x.TransactionDate >= fromDate && x.TransactionDate <= toDate &&
                               (string.IsNullOrEmpty(type) || x.Type == type)
            );

            var stats = new AdminTransactionStats
            {
                StartDate = fromDate,
                EndDate = toDate,
                TransactionType = type,
                TotalTransactions = transactions.Count,
                TotalAmount = transactions.Sum(t => decimal.Parse(t.Amount)),
                DepositCount = transactions.Count(t => t.Type == TransactionEnum.TransactionType.Deposit.ToString()),
                SpendingCount = transactions.Count(t => t.Type == TransactionEnum.TransactionType.Spending.ToString()),
                DepositAmount = transactions
                    .Where(t => t.Type == TransactionEnum.TransactionType.Deposit.ToString())
                    .Sum(t => decimal.Parse(t.Amount)),
                SpendingAmount = transactions
                    .Where(t => t.Type == TransactionEnum.TransactionType.Spending.ToString())
                    .Sum(t => decimal.Parse(t.Amount)),
                StatusDistribution = transactions
                    .GroupBy(t => t.Status)
                    .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
                    .ToList()
            };

            return stats;
        }

        // 3. Lấy thống kê giao dịch theo ngày
        public async Task<List<DailyTransactionStats>> GetDailyTransactionStatsForAdmin(DateTime fromDate, DateTime toDate, string type = null)
        {

            var transactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
                predicate: x => x.TransactionDate >= fromDate && x.TransactionDate <= toDate &&
                               (string.IsNullOrEmpty(type) || x.Type == type),
                orderBy: x => x.OrderBy(y => y.TransactionDate)
            );

            var result = new List<DailyTransactionStats>();

            // Tạo danh sách cho tất cả các ngày, kể cả ngày không có giao dịch
            for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
            {
                var dailyTransactions = transactions
                    .Where(t => t.TransactionDate.Value.Date == date)
                    .ToList();

                result.Add(new DailyTransactionStats
                {
                    Date = date,
                    TransactionType = type,
                    TransactionCount = dailyTransactions.Count,
                    DepositAmount = dailyTransactions
                        .Where(t => t.Type == TransactionEnum.TransactionType.Deposit.ToString())
                        .Sum(t => decimal.Parse(t.Amount)),
                    SpendingAmount = dailyTransactions
                        .Where(t => t.Type == TransactionEnum.TransactionType.Spending.ToString())
                        .Sum(t => decimal.Parse(t.Amount)),
                    SuccessCount = dailyTransactions.Count(t => t.Status == TransactionEnum.TransactionStatus.Success.ToString()),
                    FailedCount = dailyTransactions.Count(t => t.Status == TransactionEnum.TransactionStatus.Failed.ToString()),
                    PendingCount = dailyTransactions.Count(t => t.Status == TransactionEnum.TransactionStatus.Pending.ToString())
                });
            }

            return result;
        }

        // 4. Lấy thống kê top người dùng có nhiều giao dịch nhất
        public async Task<List<TopUserByTransaction>> GetTopUsersByTransactions(DateTime fromDate, DateTime toDate, string type = null, int limit = 10)
        {
            // Lấy danh sách transactions theo điều kiện
            var transactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
                predicate: x => x.TransactionDate >= fromDate &&
                                x.TransactionDate <= toDate &&
                                (string.IsNullOrEmpty(type) || x.Type == type)
            );

            // Lấy danh sách user và build dictionary cho lookup nhanh
            var users = await _unitOfWork.GetRepository<User>().GetListAsync();
            var userDict = users.ToDictionary(u => u.Id, u => u.Username);

            // Nhóm và xử lý
            var result = transactions
                .GroupBy(t => t.UserId)
                .Select(g => new TopUserByTransaction
                {
                    UserId = g.Key.Value,
                    Username = userDict.TryGetValue(g.Key.Value, out var username) ? username : "Unknown",
                    TransactionType = type,
                    TransactionCount = g.Count(),
                    TotalAmount = g.Sum(t => decimal.TryParse(t.Amount, out var a) ? a : 0),
                    DepositAmount = g
                        .Where(t => t.Type == TransactionEnum.TransactionType.Deposit.ToString())
                        .Sum(t => decimal.TryParse(t.Amount, out var a) ? a : 0),
                    SpendingAmount = g
                        .Where(t => t.Type == TransactionEnum.TransactionType.Spending.ToString())
                        .Sum(t => decimal.TryParse(t.Amount, out var a) ? a : 0),
                    LastTransactionDate = g.Max(t => t.TransactionDate)
                })
                .OrderByDescending(x => x.TransactionCount)
                .Take(limit)
                .ToList();

            return result;
        }


        // 5. Lấy thống kê giao dịch theo loại ví
        public async Task<List<WalletTypeStats>> GetWalletTypeStats(DateTime fromDate, DateTime toDate, string type = null)
        {
          

            var wallets = await _unitOfWork.GetRepository<Wallet>().GetListAsync();
            var transactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
                predicate: x => x.TransactionDate >= fromDate && x.TransactionDate <= toDate &&
                               (string.IsNullOrEmpty(type) || x.Type == type)
            );

            var result = wallets
                .GroupBy(w => w.Type)
                .Select(g => new WalletTypeStats
                {
                    WalletType = g.Key,
                    TransactionType = type,
                    WalletCount = g.Count(),
                    TransactionCount = transactions.Count(t => g.Any(w => w.Id == t.WalletId)),
                    TotalAmount = transactions
                        .Where(t => g.Any(w => w.Id == t.WalletId))
                        .Sum(t => decimal.Parse(t.Amount)),
                    DepositAmount = transactions
                        .Where(t => g.Any(w => w.Id == t.WalletId) &&
                               t.Type == TransactionEnum.TransactionType.Deposit.ToString())
                        .Sum(t => decimal.Parse(t.Amount)),
                    SpendingAmount = transactions
                        .Where(t => g.Any(w => w.Id == t.WalletId) &&
                               t.Type == TransactionEnum.TransactionType.Spending.ToString())
                        .Sum(t => decimal.Parse(t.Amount))
                })
                .ToList();

            return result;
        }

        // 6. Lấy thống kê xu hướng giao dịch theo tháng
        public async Task<List<MonthlyTransactionTrend>> GetMonthlyTransactionTrend(DateTime fromDate, DateTime toDate, string type = null)
        {
           

            var startDate = new DateTime(fromDate.Year, fromDate.Month, 1);
            var endDate = new DateTime(toDate.Year, toDate.Month, DateTime.DaysInMonth(toDate.Year, toDate.Month));

            var transactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
                predicate: x => x.TransactionDate >= startDate && x.TransactionDate <= endDate &&
                               (string.IsNullOrEmpty(type) || x.Type == type),
                orderBy: x => x.OrderBy(y => y.TransactionDate)
            );

            var result = new List<MonthlyTransactionTrend>();

            // Tạo danh sách cho tất cả các tháng, kể cả tháng không có giao dịch
            for (var date = startDate; date <= endDate; date = date.AddMonths(1))
            {
                var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                var monthlyTransactions = transactions
                    .Where(t => t.TransactionDate >= firstDayOfMonth && t.TransactionDate <= lastDayOfMonth)
                    .ToList();

                result.Add(new MonthlyTransactionTrend
                {
                    YearMonth = $"{date.Year}-{date.Month:D2}",
                    TransactionType = type,
                    TransactionCount = monthlyTransactions.Count,
                    NewUserCount = await GetNewUserCountInMonth(date.Year, date.Month),
                    DepositAmount = monthlyTransactions
                        .Where(t => t.Type == TransactionEnum.TransactionType.Deposit.ToString())
                        .Sum(t => decimal.Parse(t.Amount)),
                    SpendingAmount = monthlyTransactions
                        .Where(t => t.Type == TransactionEnum.TransactionType.Spending.ToString())
                        .Sum(t => decimal.Parse(t.Amount)),
                    SuccessRate = monthlyTransactions.Count > 0
                        ? (double)monthlyTransactions.Count(t => t.Status == TransactionEnum.TransactionStatus.Success.ToString()) / monthlyTransactions.Count * 100
                        : 0
                });
            }

            return result;
        }

        // Hàm hỗ trợ để lấy số người dùng mới trong tháng
        private async Task<int> GetNewUserCountInMonth(int year, int month)
        {
            var firstDayOfMonth = new DateTime(year, month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var userCount = await _unitOfWork.GetRepository<User>().CountAsync(
                u => u.CreatedAt >= firstDayOfMonth && u.CreatedAt <= lastDayOfMonth
            );

            return userCount;
        }

        // 7. Lấy thống kê phương thức thanh toán
        public async Task<List<PaymentMethodStats>> GetPaymentMethodStats(DateTime fromDate, DateTime toDate, string type = null)
        {

            var paymentMethods = await _unitOfWork.GetRepository<PaymentMethod>().GetListAsync();
            var transactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
                predicate: x => x.TransactionDate >= fromDate && x.TransactionDate <= toDate &&
                               (string.IsNullOrEmpty(type) || x.Type == type)
            );

            var result = paymentMethods
                .Select(pm => new PaymentMethodStats
                {
                    PaymentMethodId = pm.Id,
                    PaymentMethodName = pm.Name,
                    TransactionType = type,
                    TransactionCount = transactions.Count(t => t.PaymentMethodId == pm.Id),
                    TotalAmount = transactions
                        .Where(t => t.PaymentMethodId == pm.Id)
                        .Sum(t => decimal.Parse(t.Amount)),
                    SuccessCount = transactions.Count(t => t.PaymentMethodId == pm.Id &&
                                                      t.Status == TransactionEnum.TransactionStatus.Success.ToString()),
                    FailedCount = transactions.Count(t => t.PaymentMethodId == pm.Id &&
                                                     t.Status == TransactionEnum.TransactionStatus.Failed.ToString()),
                    SuccessRate = transactions.Count(t => t.PaymentMethodId == pm.Id) > 0
                        ? (double)transactions.Count(t => t.PaymentMethodId == pm.Id &&
                                                     t.Status == TransactionEnum.TransactionStatus.Success.ToString()) /
                          transactions.Count(t => t.PaymentMethodId == pm.Id) * 100
                        : 0
                })
                .OrderByDescending(x => x.TransactionCount)
                .ToList();

            return result;
        }

        // 8. Lấy thống kê danh mục giao dịch
        public async Task<List<CategoryStats>> GetTransactionCategoryStats(DateTime fromDate, DateTime toDate, string type = null)
        {
            

            var categories = await _unitOfWork.GetRepository<Category>().GetListAsync();
            var transactions = await _unitOfWork.GetRepository<Transaction>().GetListAsync(
                predicate: x => x.TransactionDate >= fromDate && x.TransactionDate <= toDate &&
                               (string.IsNullOrEmpty(type) || x.Type == type)
            );

            var result = categories
                .Select(c => new CategoryStats
                {
                    CategoryId = c.Id,
                    CategoryName = c.Name,
                    TransactionType = type,
                    TransactionCount = transactions.Count(t => t.CategoryId == c.Id),
                    TotalAmount = transactions
                        .Where(t => t.CategoryId == c.Id)
                        .Sum(t => decimal.Parse(t.Amount)),
                    AverageAmount = transactions.Count(t => t.CategoryId == c.Id) > 0
                        ? transactions
                            .Where(t => t.CategoryId == c.Id)
                            .Average(t => decimal.Parse(t.Amount))
                        : 0
                })
                .OrderByDescending(x => x.TransactionCount)
                .ToList();

            return result;
        }

        
    }
}
