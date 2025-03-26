using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace VinWallet.API.Service.RabbitMQ
{
    public class RabbitMQPublisher : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly string _exchange;
        private readonly string _queueName;

        public RabbitMQPublisher(IOptions<RabbitMQOptions> options, IConnectionFactory connectionFactory)
        {
            var config = options.Value;

            _connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _exchange = config.Exchange;
            _queueName = config.QueueName;

            _channel.ExchangeDeclareAsync(_exchange, ExchangeType.Topic, durable: true);
        }

        public async Task Publish(string eventType,string targetQueue, object message)
        {
            string jsonMessage = JsonSerializer.Serialize(message);
            byte[] body = Encoding.UTF8.GetBytes(jsonMessage);

            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            List<string> routingKeys = targetQueue switch
            {
                "homeclean" => new List<string> { $"home_clean.{eventType}" },
                "vinwallet" => new List<string> { $"vin_wallet.{eventType}" },
                "vinlaundy" => new List<string> { $"vin_laundy.{eventType}" },
                "all" => new List<string> { $"home_clean.{eventType}", $"vin_wallet.{eventType}", $"vin_laundy.{eventType}" },
                _ => throw new ArgumentException("Invalid target queue")
            };

            foreach (var routingKey in routingKeys)
            {
                await _channel.BasicPublishAsync(exchange: _exchange, routingKey: routingKey, false, basicProperties: props, body: body);
                Console.WriteLine($"[RabbitMQ] Sent to {routingKey}: {message}");
            }
        }

        public void Dispose()
        {
            _channel?.CloseAsync();
            _connection?.CloseAsync();
        }
    }
}
