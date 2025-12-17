using PaymentsPublisher.Services;

namespace PaymentsPublisher
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PaymentEventsPublisher Worker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbService = scope.ServiceProvider.GetRequiredService<IDbService>();
                    var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

                    var messages = await dbService.GetUnprocessedMessagesAsync(50);

                    if (messages.IsSuccess && messages.Value.Count == 0)
                    {
                        await Task.Delay(5000, stoppingToken);
                        continue;
                    }
                    else if (messages.IsFailure)
                    {
                        _logger.LogError("Failed to retrieve unprocessed messages: {Error}", messages.Error);
                        await Task.Delay(5000, stoppingToken);
                        continue;
                    }

                    foreach (var message in messages.Value)
                    {
                        var jsonMessage = System.Text.Json.JsonSerializer.Serialize(message);
                        var publishResult = await publisher.PublishAsync(jsonMessage, message.Type);
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
