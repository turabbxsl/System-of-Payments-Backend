namespace PaymentsApi.Models
{
    public record PaymentRequest(
        /*string IdempotencyKey,
        Guid UserId,*/
        decimal Amount,
        string Currency,
        int ProviderId
    );
}
