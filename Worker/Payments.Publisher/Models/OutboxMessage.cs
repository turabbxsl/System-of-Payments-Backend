namespace PaymentsPublisher.Models
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public Guid TransactionId { get; set; }
        public DateTime CreateDate { get; set; }
        public decimal Amount { get; set; }
        public string UserId { get; set; }
        public int ProviderId { get; set; }
        public string Currency { get; set; }
    }
}
