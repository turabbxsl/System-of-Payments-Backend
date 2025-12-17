using Payments.Common.Config;

namespace PaymentsPublisher.Config
{
    public class PublisherRabbitMqConfig:RabbitMqConnectionConfig
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public int MaxRetries { get; set; } = 5;
        public int RetryDelayMs { get; set; } = 5000;
    }
}
