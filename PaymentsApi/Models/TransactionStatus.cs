using NpgsqlTypes;

namespace PaymentsApi.Models
{
    public enum TransactionStatus
    {
        [PgName("Pending")]
        Pending,
        [PgName("Processing")]
        Processing,
        [PgName("Success")]
        Success,
        [PgName("Failed")]
        Failed
    }

}
