using RabbitMQ.Client;

namespace VinWallet.API.Service.RabbitMQ
{
    public class RabbitMQPublisher : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly string _exchange;

        public RabbitMQPublisher(IConnectionFactory connectionFactory, string exchange)
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

    }
}
