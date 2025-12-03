namespace PaymentsApi.Models;

public partial class Idempotencykey
{
    public Guid Id { get; set; }

    public string Key { get; set; } = null!;

    public Guid Userid { get; set; }

    public string Requesthash { get; set; } = null!;

    public string? Resultdata { get; set; }

    public DateTime Createdat { get; set; }

    public DateTime? Updatedat { get; set; }
}
