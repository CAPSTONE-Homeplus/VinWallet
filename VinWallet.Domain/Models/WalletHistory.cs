using System;
using System.Collections.Generic;

namespace VinWallet.Domain.Models;

public partial class WalletHistory
{
    public Guid Id { get; set; }

    public string? BeforeBalance { get; set; }

    public string? AfterBalance { get; set; }

    public string? ExtraField { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Status { get; set; }

    public Guid? WalletId { get; set; }

    public virtual Wallet? Wallet { get; set; }
}
