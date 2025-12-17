using Microsoft.Extensions.Options;
using Payments.Common.Config;
using Payments.Common.Constants;
using Payments.Common.Models;
using PaymentsPublisher.Config;
using PaymentsPublisher.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Text;
using System.Text.Json;

namespace PaymentsPublisher.Services
{
    public interface IMessagePublisher
    {
        Task<Result<bool>> PublishAsync(string messageBody, string messageType);
    }

    public class MessagePublisher : IMessagePublisher, IDisposable
    {
        private readonly RabbitMqConnectionConfig _baseConfigOptions;
        private readonly PublisherRabbitMqConfig _publisherConfigOptions;
        private readonly ILogger<MessagePublisher> _logger;
        private readonly IDbService _dbService;

        private readonly object _lock = new();

        private IConnection? _connection;
        private IChannel? _channel;

        public MessagePublisher(ILogger<MessagePublisher> logger, IDbService dbService, IOptions<RabbitMqConnectionConfig> baseConfigOptions, IOptions<PublisherRabbitMqConfig> publisherConfigOptions)
        {
            _logger = logger;
            _dbService = dbService;
            _baseConfigOptions = baseConfigOptions.Value;
            _publisherConfigOptions = publisherConfigOptions.Value;
        }


        public async Task<Result<bool>> PublishAsync(string messageBody, string messageType)
        {
            if (_channel == null)
            {
                var initResult = await InitializeAsync();
                if (initResult.IsFailure)
                {
                    return initResult;
                }
            }

            try
            {
                var messageModel = JsonSerializer.Deserialize<OutboxMessage>(messageBody);
                var paymentId = messageModel?.TransactionId ?? Guid.Empty;
                var body = Encoding.UTF8.GetBytes(messageBody);
                var props = new BasicProperties();
                props.Persistent = true;

                await _channel.BasicPublishAsync(
                    exchange: TopologyContants.PaymentEventsExchange,
                    routingKey: messageType,
                    basicProperties: props,
                    mandatory: true,
                    body: body
                );

                var updateDbResult = await _dbService.UpdateMessageAsync(paymentId);

                if (updateDbResult.IsFailure)
                {
                    var dbErrorMessage = updateDbResult.Error.Message;

                    _logger.LogError(
                        "RabbitMQ published but DB update failed for ID {Id}. Error: {DbError}",
                        paymentId,
                        dbErrorMessage
                    );

                    return updateDbResult;
                }

                _logger.LogInformation("Message published successfully. Type: {Type}", messageType);
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message of type: {Type}", messageType);
                return Result.Failure<bool>(Error.PublisherFailure with
                {
                    Message = $"RabbitMQ publish failed: {ex.Message}"
                });
            }
        }


        private async Task<Result<bool>> InitializeAsync()
        {
            lock (_lock)
            {
                if (_channel != null)
                    return Result.Success(true);
            }

            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = _baseConfigOptions.HostName,
                    Port = _baseConfigOptions.Port,
                    UserName = _publisherConfigOptions.UserName,
                    Password = _publisherConfigOptions.Password,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection?.Dispose();
                _connection = await factory.CreateConnectionAsync();

                _channel?.Dispose();
                _channel = await _connection.CreateChannelAsync();

                await SetupRabbitMqAsync();

                _logger.LogInformation("RabbitMQ connection and channel initialized successfully.");
                return Result.Success(true);
            }
            catch (Exception ex) when (ex is BrokerUnreachableException || ex is IOException)
            {
                _logger.LogCritical(ex, "Failed to connect to RabbitMQ broker: {Host}", _baseConfigOptions.HostName);
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

        private async Task SetupRabbitMqAsync()
        {
            try
            {
                // Main - Declare and Bind Exchanges and Queues
                await _channel.ExchangeDeclareAsync(
                    exchange: TopologyContants.PaymentEventsExchange,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false
                    );

                await _channel.QueueDeclareAsync(
                    queue: TopologyContants.PaymentEventsQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                    );

                await _channel.QueueBindAsync(
                    queue: TopologyContants.PaymentEventsQueue,
                    exchange: TopologyContants.PaymentEventsExchange,
                    routingKey: TopologyContants.RoutingKeyWildcard
                    );
         
                // Retry - Declare and Bind Exchanges and Queues
                await _channel.ExchangeDeclareAsync(
                    exchange: TopologyContants.PaymentRetryExchange,
                    type: ExchangeType.Topic,
                    durable: true
                    );

                var retryQueueArgs = new Dictionary<string, object>() {
            { "x-message-ttl", _publisherConfigOptions.RetryDelayMs },
            { "x-dead-letter-exchange", TopologyContants.PaymentEventsExchange },
            { "x-dead-letter-routing-key",TopologyContants.RoutingKeyWildcard}
            };

                await _channel.QueueDeclareAsync(
                    queue: TopologyContants.PaymentRetryQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: retryQueueArgs
                    );
                await _channel.QueueBindAsync(
                    queue: TopologyContants.PaymentRetryQueue,
                    exchange: TopologyContants.PaymentRetryExchange,
                    routingKey: TopologyContants.RoutingKeyWildcard
                    );

                // DLQ = Declare and Bind Exchanges and Queues
                await _channel.ExchangeDeclareAsync(
                    exchange: TopologyContants.PaymentDLQExchange,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false
                    );
                await _channel.QueueDeclareAsync(
                    queue: TopologyContants.PaymentDLQQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                    );
                await _channel.QueueBindAsync(
                    queue: TopologyContants.PaymentDLQQueue,
                    exchange: TopologyContants.PaymentDLQExchange,
                    routingKey: TopologyContants.RoutingKeyWildcard
                    );
            }
            catch (Exception ex)
            {

            }
        }

        public void Dispose()
        {
            _channel?.CloseAsync();
            _connection?.CloseAsync();
        }
    }
}
