using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using VinWallet.API.Extensions;
using VinWallet.API.Service.Interfaces;
using VinWallet.API.Service.RabbitMQ;
using VinWallet.API.Service.RabbitMQ.Message;
using VinWallet.Domain.Models;
using VinWallet.Repository.Constants;
using VinWallet.Repository.Enums;
using VinWallet.Repository.Generic.Implements;
using VinWallet.Repository.Generic.Interfaces;
using VinWallet.Repository.Payload.Response.OrderResponse;
using VinWallet.Repository.Utils;

namespace HomeClean.API.Service.Implements.RabbitMQ
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly string _queueName;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IHttpClientFactory _httpClientFactory;

        public RabbitMQConsumer(IOptions<RabbitMQOptions> options, IConnectionFactory connectionFactory, IServiceScopeFactory serviceScopeFactory, IHttpClientFactory httpClientFactory)
        {
            var config = options.Value;
            _queueName = config.QueueName;

            _connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _channel.ExchangeDeclareAsync(config.Exchange, ExchangeType.Topic, durable: true);
            _channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.QueueBindAsync(_queueName, config.Exchange, "vin_wallet.#");
            _serviceScopeFactory = serviceScopeFactory;
            _httpClientFactory = httpClientFactory;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            StartListening();
            return Task.CompletedTask;
        }

        private void StartListening()
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"[RabbitMQ] Received from {_queueName}: {message}");

                try
                {
                    var routingKeyParts = ea.RoutingKey.Split('.');
                    var eventType = routingKeyParts.Length > 1 ? routingKeyParts[1] : "";

                    if (string.IsNullOrEmpty(eventType))
                    {
                        Console.WriteLine("[RabbitMQ] Invalid message format.");
                        return;
                    }

                    switch (eventType)
                    {
                        case "payment_success":
                            break;
                        case "add_wallet_member":
                            await HandleAddWalletMemberNotification(System.Text.Json.JsonSerializer.Deserialize<InviteWalletMessage>(message));
                            break;
                        case "refund_order":
                          
                            await ProcessPointRefund(Guid.Parse(message.Trim('"')));
                            break;

                        default:
                            Console.WriteLine($"[RabbitMQ] Unhandled event type: {eventType}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RabbitMQ] Error processing message: {ex.Message}");
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            };

            _channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer);
        }

        private async Task HandleAddWalletMemberNotification(InviteWalletMessage inviteWalletMessage)
        {
            var messageObject = new MessageObject<InviteWalletMessage>
            {
                Type = MessageTypeEnum.InviteWallet.ToString(),
                Data = inviteWalletMessage
            };
            await _serviceScopeFactory.ExecuteScopedAsync<ISignalRHubService>(async service =>
            {
                await service.SendNotificationToUser(inviteWalletMessage.MemberId.ToString(), JsonConvert.SerializeObject(messageObject));
            });
        }

        private async Task<OrderResponse?> GetOrderByOrderId(Guid orderId)
        {
            try
            {
                var url = HomeCleanApiEndPointConstant.Order.OrderEndpoint.Replace("{id}", orderId.ToString()) + "/no-token";
                var apiResponse = await CallApiUtils.CallApiEndpoint(
                    HomeCleanApiEndPointConstant.Order.OrderEndpoint.Replace("{id}", orderId.ToString()) + "/no-token"
                );
                var order = await CallApiUtils.GenerateObjectFromResponse<OrderResponse>(apiResponse);

                if (order.Id == null || order.Id == Guid.Empty)
                    throw new BadHttpRequestException(MessageConstant.Order.OrderNotFound);

                return order;
            }
            catch (Exception ex)
            {
                Console.Write($"Error fetching order {orderId}: {ex.Message}");
                return null;
            }
        }

        private async Task ProcessPointRefund(Guid orderId)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var serviceProvider = scope.ServiceProvider;

                var transactionService = serviceProvider.GetRequiredService<ITransactionService>();
                var walletService = serviceProvider.GetRequiredService<IWalletService>();
                var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork<VinWalletContext>>();
                var logger = serviceProvider.GetRequiredService<ILogger<RabbitMQConsumer>>();

                // 🔹 Lấy đơn hàng từ API
                var order = await GetOrderByOrderId(orderId);
                if (order == null)
                {
                    logger.LogWarning($"Order {orderId} not found");
                    return;
                }

                // 🔹 Tìm transaction dựa trên orderId
                var transaction = await unitOfWork.GetRepository<Transaction>()
                    .SingleOrDefaultAsync(predicate: x => x.OrderId == orderId);

                if (transaction == null)
                {
                    logger.LogWarning($"No transaction found for order {orderId}");
                    return;
                }

                // 🔹 Tìm wallet từ transaction
                var wallet = await unitOfWork.GetRepository<Wallet>()
                    .SingleOrDefaultAsync(predicate: x => x.Id == transaction.WalletId);

                if (wallet == null)
                {
                    logger.LogWarning($"No wallet found for transaction {transaction.Id}");
                    return;
                }

                decimal refundAmount = order.TotalAmount.Value; // Số điểm cần hoàn

                var category = await unitOfWork.GetRepository<Category>()
                .SingleOrDefaultAsync(predicate: x => x.Name.Equals(TransactionCategoryEnum.TransactionCategory.Refund.ToString()));
                // 🔹 Cộng điểm vào ví
                wallet.Balance += refundAmount;
                wallet.UpdatedAt = DateTime.UtcNow.AddHours(7);
                unitOfWork.GetRepository<Wallet>().UpdateAsync(wallet);

                // 🔹 Tạo transaction mới ghi nhận hoàn tiền
                var refundTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    UserId = order.UserId,
                    Amount = refundAmount.ToString(),
                    Type = "Refund",
                    Note = $"Hoàn điểm từ đơn hàng {orderId}",
                    TransactionDate = DateTime.UtcNow.AddHours(7),
                    Status = "Success",
                    CreatedAt = DateTime.UtcNow.AddHours(7),
                    UpdatedAt = DateTime.UtcNow.AddHours(7),
                    Code = DateTime.UtcNow.Ticks.ToString(),
                    CategoryId = category.Id,
                    OrderId = orderId,

                };
                await unitOfWork.GetRepository<Transaction>().InsertAsync(refundTransaction);

                // 🔹 Lưu thay đổi vào DB
                await unitOfWork.CommitAsync();

                logger.LogInformation($"Successfully refunded {refundAmount} points to wallet {wallet.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing refund for order {orderId}: {ex.Message}");
                throw new Exception("Lỗi khi hoàn điểm, vui lòng thử lại.");
            }
        }



    }
}
