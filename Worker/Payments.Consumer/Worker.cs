using Consumer.Services;

namespace Consumer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageConsumer _consumer;


        public Worker(ILogger<Worker> logger, IMessageConsumer consumer)
        {
            _logger = logger;
            _consumer = consumer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PaymentEventsConsumer Worker is starting.");

            await _consumer.StartConsumingAsync(stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

    }
}
