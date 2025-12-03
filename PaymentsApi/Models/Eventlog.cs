using System;
using System.Collections.Generic;

namespace PaymentsApi.Models;

public partial class Eventlog
{
    public Guid Id { get; set; }

    public Guid Transactionid { get; set; }

    public string Eventtype { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public DateTime Createdat { get; set; }
}
