namespace Payments.Common.Constants
{
    public class TopologyContants
    {

        // main exchange and queue
        public const string PaymentEventsExchange = "payment.events.exchange";
        public const string PaymentEventsQueue = PaymentEventsExchange + ".queue";

        // retry exchange and queue
        public const string PaymentRetryExchange = "payment.events.retry.exchange";
        public const string PaymentRetryQueue = "payment.events.retry.queue";

        // dead-letter exchange and queue
        public const string PaymentDLQExchange = "payment.events.dlq.exchange";
        public const string PaymentDLQQueue = "payment.events.dlq.queue";


        public const string RoutingKeyWildcard = "#";
    }
}
