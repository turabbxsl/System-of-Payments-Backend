using NpgsqlTypes;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentsApi.Models;

public partial class Outboxmessage
{
    public Guid Id { get; set; }

    public Guid Transactionid { get; set; }

    public string Payload { get; set; } = null!;

    [Column("status")]
    public TransactionStatus Status { get; set; }

    public string Type { get; set; } = null!;

    public DateTime Createdat { get; set; }
}
