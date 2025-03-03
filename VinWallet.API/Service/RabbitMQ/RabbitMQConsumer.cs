using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using VinWallet.API.Service.RabbitMQ;

namespace HomeClean.API.Service.Implements.RabbitMQ
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly string _queueName;

        public RabbitMQConsumer(IOptions<RabbitMQOptions> options, IConnectionFactory connectionFactory)
        {
            var config = options.Value;
            _queueName = config.QueueName;

            _connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _channel.ExchangeDeclareAsync(config.Exchange, ExchangeType.Topic, durable: true);
            _channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.QueueBindAsync(_queueName, config.Exchange, "home_plus.#");
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
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                await Task.Yield();
            };

            _channel.BasicConsumeAsync(queue: _queueName, autoAck: true, consumer: consumer);
        }
    }
}
