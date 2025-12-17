using Payments.Common.Config;

namespace Consumer.Config
{
    public class ConsumerRabbitMqConfig:RabbitMqConnectionConfig
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConsumerTag { get; set; } = "Payments_Processor_1";

        public ushort PrefetchCount { get; set; }
        public int RetryDelayMs { get; set; }
        public int MaxRetries { get; set; }

    }
}
