
using Microsoft.EntityFrameworkCore.Metadata;
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
        private readonly Dictionary<string, string> _queues;

        public RabbitMQConsumer(IOptions<RabbitMQOptions> options, IConnectionFactory connectionFactory)
        {
            var config = options.Value;

            _connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _channel.ExchangeDeclareAsync(config.Exchange, ExchangeType.Topic, durable: true);

            _queues = config.Queues;

            foreach (var queue in _queues.Values)
            {
                _channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
                _channel.QueueBindAsync(queue, config.Exchange, $"{queue}.#");
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var queue in _queues.Values)
            {
                StartListening(queue);
            }
            return Task.CompletedTask;
        }

        private void StartListening(string queueName)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"[RabbitMQ] Received from {queueName}: {message}");
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

                await Task.Yield();
            };

            _channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);
        }
    }
}
