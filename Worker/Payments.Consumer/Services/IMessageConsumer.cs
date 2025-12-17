using Consumer.Config;
using Consumer.Models;
using Microsoft.Extensions.Options;
using Payments.Common.Config;
using Payments.Common.Constants;
using Payments.Common.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Text.Json;

namespace Consumer.Services
{
    public interface IMessageConsumer
    {
        Task StartConsumingAsync(CancellationToken cancellationToken);
    }

    public class MessageConsumer : IMessageConsumer, IDisposable
    {
        private readonly ILogger<MessageConsumer> _logger;
        private readonly ConsumerRabbitMqConfig _config;
        private readonly RabbitMqConnectionConfig _baseConfig;

        private IConnection? _connection;
        private IChannel? _channel;
        private AsyncEventingBasicConsumer? _consumer;


        private readonly object _lock = new();

        public MessageConsumer(ILogger<MessageConsumer> logger, IOptions<ConsumerRabbitMqConfig> config, IOptions<RabbitMqConnectionConfig> baseConfig)
        {
            _logger = logger;
            _config = config.Value;
            _baseConfig = baseConfig.Value;
        }

        public async Task StartConsumingAsync(CancellationToken cancellationToken)
        {
            await InitializeRabbitMqListener();

            if (_channel == null)
                return;

            await SetupTopologyAsync();

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: _config.PrefetchCount, global: false);

            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.ReceivedAsync += async (sender, ea) =>
            {
                var consumer = (AsyncEventingBasicConsumer)sender;
                var channel = consumer.Channel;

                var retryCount = GetRetryCount(ea.BasicProperties);
                var result = await ProcessMessageAsync(ea);

                if (!result.IsSuccess)
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                else
                {
                    if (retryCount >= _config.MaxRetries)
                        await PublishToDlqAsync(channel, ea, retryCount);
                    else
                        await PublishToRetryAsync(channel, ea, retryCount);
                }
            };

            await _channel.BasicConsumeAsync(queue: TopologyContants.PaymentEventsQueue,
                                         autoAck: false,
                                         //consumerTag: _config.ConsumerTag,
                                         consumer: _consumer);
        }

        private async Task<Result<bool>> ProcessMessageAsync(BasicDeliverEventArgs ea)
        {
            try
            {
                var body = ea.Body.ToArray();
                var messageString = Encoding.UTF8.GetString(body);
                var model = JsonSerializer.Deserialize<PaymentEventModel>(messageString);

                if (model?.TransactionId == Guid.Empty)
                {
                    _logger.LogError("Empty payment ID received");
                    return Result.Failure<bool>(Error.EmptyPaymentId);
                }

                _logger.LogInformation("Message processed successfully. TransactionId: {TransactionId}", model.TransactionId);
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message}", ex.Message);
                return Result.Failure<bool>(Error.Failure with { Message = ex.Message });
            }
        }

        public async Task PublishToRetryAsync(IChannel channel, BasicDeliverEventArgs ea, int retryCount)
        {
            var props = new BasicProperties();
            props.Persistent = true;
            props.Headers = new Dictionary<string, object>{
                    { "x-retry-count", retryCount + 1 },
                    { "x-first-death-reason", "processing-failed" },
                    { "x-original-timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
            };

            await channel.BasicPublishAsync(
               exchange: TopologyContants.PaymentRetryExchange,
               routingKey: TopologyContants.RoutingKeyWildcard,
               mandatory: true,
               basicProperties: props,
               body: ea.Body);
            await channel.BasicAckAsync(ea.DeliveryTag, false);
        }

        public async Task PublishToDlqAsync(IChannel channel, BasicDeliverEventArgs ea, int retryCount)
        {
            var dlqProperties = new BasicProperties
            {
                Persistent = true,
                Headers = new Dictionary<string, object>
                {
                    { "x-retry-count", retryCount },
                    { "x-death-reason", "max-retries-exceeded" },
                    { "x-death-timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
                }
            };

            await channel.BasicPublishAsync(
                exchange: TopologyContants.PaymentDLQExchange,
                routingKey: TopologyContants.RoutingKeyWildcard,
                basicProperties:
                dlqProperties, body: ea.Body,
                mandatory: true);

            await channel.BasicAckAsync(ea.DeliveryTag, false);
        }

        public void Dispose()
        {
            _channel?.CloseAsync();
            _connection?.CloseAsync();
        }

        private int GetRetryCount(IReadOnlyBasicProperties properties)
        {
            if (properties?.Headers == null) return 0;

            if (properties.Headers.TryGetValue("x-retry-count", out var value))
            {
                if (value is int intValue)
                    return intValue;

                if (value is byte[] bytes && int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed))
                    return parsed;
            }

            return 0;
        }

        private async Task<Result<bool>> InitializeRabbitMqListener()
        {

            lock (_lock)
            {
                if (_connection != null && _channel != null)
                {
                    return Result.Success(true);
                }
            }

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _baseConfig.HostName,
                    Port = _baseConfig.Port,
                    UserName = _config.UserName,
                    Password = _config.Password,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection?.Dispose();
                _connection = await factory.CreateConnectionAsync();
                _connection.ConnectionShutdownAsync += OnConnectionShutdown;

                _channel?.Dispose();
                _channel = await _connection.CreateChannelAsync();


                return Result.Success(true);
            }
            catch (Exception ex) when (ex is BrokerUnreachableException || ex is IOException)
            {
                return Result.Failure<bool>(Error.BrokerConnectionFailure with
                {
                    Message = $"RabbitMQ broker unreachable: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                return Result.Failure<bool>(Error.Failure with
                {
                    Message = $"Unexpected error during RabbitMQ initialization: {ex.Message}"
                });
            }
        }

        private async Task SetupTopologyAsync()
        {
            if (_channel == null) return;

            // 1. Main Exchange və Queue
            await _channel.ExchangeDeclareAsync(
                exchange: TopologyContants.PaymentEventsExchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
                );
            await _channel.QueueDeclareAsync(
                queue: TopologyContants.PaymentEventsQueue,
                                durable: true,
                                autoDelete: false,
                                exclusive: false,
                                arguments: null
                );
            await _channel.QueueBindAsync(
                exchange: TopologyContants.PaymentEventsExchange,
                queue: TopologyContants.PaymentEventsQueue,
                routingKey: TopologyContants.RoutingKeyWildcard
                );

            // 2. Retry Exchange və Queue
            await _channel.ExchangeDeclareAsync(
                exchange: TopologyContants.PaymentRetryExchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
                );
            var retryQueueArgs = new Dictionary<string, object>() {
            { "x-message-ttl", _config.RetryDelayMs },
            { "x-dead-letter-exchange", TopologyContants.PaymentEventsExchange },
            { "x-dead-letter-routing-key",TopologyContants.RoutingKeyWildcard}
            };
            await _channel.QueueDeclareAsync(
                queue: TopologyContants.PaymentRetryQueue,
                                durable: true,
                                autoDelete: false,
                                exclusive: false,
                                arguments: retryQueueArgs
                );

            await _channel.QueueBindAsync(
    exchange: TopologyContants.PaymentRetryExchange,
    queue: TopologyContants.PaymentRetryQueue,
    routingKey: TopologyContants.RoutingKeyWildcard
);

            // 3. DLQ Exchange və Queue
            await _channel.ExchangeDeclareAsync(
                exchange: TopologyContants.PaymentDLQExchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
                );
            await _channel.QueueDeclareAsync(
                queue: TopologyContants.PaymentDLQQueue,
                                durable: true,
                                autoDelete: false,
                                exclusive: false,
                                arguments: null
                );
            await _channel.QueueBindAsync(
                exchange: TopologyContants.PaymentDLQExchange,
                queue: TopologyContants.PaymentDLQQueue,
                routingKey: TopologyContants.RoutingKeyWildcard
                );
        }

        private async Task OnConnectionShutdown(object? sender, ShutdownEventArgs e)
        {
            _logger.LogWarning("RabbitMQ connection shut down. Reason: {Reason}, Initiator: {Initiator}",
                               e.ReplyText, e.Initiator);
        }

    }
}
