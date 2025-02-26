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
        private readonly Dictionary<string, string> _queues;

        public RabbitMQPublisher(IOptions<RabbitMQOptions> options, IConnectionFactory connectionFactory)
        {

            var config = options.Value;

            _connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _exchange = config.Exchange;
            _queues = config.Queues;

            _channel.ExchangeDeclareAsync(_exchange, ExchangeType.Topic, durable: true);
        }

        public async void Publish(string queueName, string eventType, object message, bool isBroadcast = false)
        {
            string routingKey;

            if (isBroadcast)
            {
                routingKey = $"broadcast.{eventType}";
            }
            else
            {
                if (!_queues.ContainsKey(queueName))
                {
                    Console.WriteLine($"[RabbitMQ] Queue {queueName} not found in configuration.");
                    return;
                }

                queueName = _queues[queueName];
                routingKey = $"{queueName}.{eventType}";
            }

            string jsonMessage = JsonSerializer.Serialize(message);
            byte[] body = Encoding.UTF8.GetBytes(jsonMessage);

            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await _channel.BasicPublishAsync(_exchange, routingKey, false, props, body);
            Console.WriteLine($"[RabbitMQ] Sent to routingKey: {routingKey}");
        }



        public void Dispose()
        {
            _channel?.CloseAsync();
            _connection?.CloseAsync();
        }

    }
}
