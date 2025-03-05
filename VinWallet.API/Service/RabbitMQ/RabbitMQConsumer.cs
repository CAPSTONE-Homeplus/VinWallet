using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using VinWallet.API.Extensions;
using VinWallet.API.Service.Interfaces;
using VinWallet.API.Service.RabbitMQ;
using VinWallet.API.Service.RabbitMQ.Message;
using VinWallet.Repository.Enums;

namespace HomeClean.API.Service.Implements.RabbitMQ
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly string _queueName;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public RabbitMQConsumer(IOptions<RabbitMQOptions> options, IConnectionFactory connectionFactory, IServiceScopeFactory serviceScopeFactory)
        {
            var config = options.Value;
            _queueName = config.QueueName;

            _connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _channel.ExchangeDeclareAsync(config.Exchange, ExchangeType.Topic, durable: true);
            _channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.QueueBindAsync(_queueName, config.Exchange, "vin_wallet.#");
            _serviceScopeFactory = serviceScopeFactory;
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
    }
}
