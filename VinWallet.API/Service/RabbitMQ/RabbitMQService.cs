using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace VinWallet.API.Service.RabbitMQ
{
    public class RabbitMQService : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly string _exchange;

        public RabbitMQService(IConnectionFactory connectionFactory, string exchange)
        {

            _connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _exchange = exchange;

            _channel.ExchangeDeclareAsync(exchange: _exchange, type: ExchangeType.Topic, durable: true);
        }

        public void Dispose()
        {
            _channel?.CloseAsync();
            _connection?.CloseAsync();
        }

        public async void Publish(string routingKey, object message)
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(message));
            var props = new BasicProperties();
            props.ContentType = "application/json";
            props.DeliveryMode = DeliveryModes.Persistent;
            await _channel.BasicPublishAsync(_exchange, routingKey, false, props, body);
            Console.WriteLine($"[RabbitMQ] Sent: {routingKey} -> {message}");
        }

        public async Task ConsumeAsync(string queueName, string routingKey, Func<string, Task> onMessageReceived)
        {
            await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            await _channel.QueueBindAsync(queue: queueName, exchange: _exchange, routingKey: routingKey);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = System.Text.Encoding.UTF8.GetString(body);
                Console.WriteLine($"[RabbitMQ] Received: {message}");

                if (onMessageReceived != null)
                {
                    await onMessageReceived(message);
                }
            };
            await _channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);
        }

    }
}
