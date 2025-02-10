using System;
using System.Collections.Generic;

namespace VinWallet.Domain.Models;

public partial class TransactionCategory
{
    public Guid Id { get; set; }

    public Guid? CategoryId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Status { get; set; }

    public virtual Category? Category { get; set; }
}
