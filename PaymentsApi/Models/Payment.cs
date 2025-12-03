using NpgsqlTypes;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentsApi.Models;

public partial class Payment
{
    public Guid Id { get; set; }

    public Guid Userid { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public int Providerid { get; set; }

    [Column("status")]
    public TransactionStatus Status { get; set; }

    public DateTime Createdat { get; set; }

    public DateTime? Updatedat { get; set; }
}
