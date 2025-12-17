namespace Payments.Common.Config
{
    public class RabbitMqConnectionConfig
    {
        public string HostName { get; set; } = string.Empty;
        public int Port { get; set; } = 5672;
    }
}
