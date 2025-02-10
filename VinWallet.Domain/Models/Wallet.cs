using System;
using System.Collections.Generic;

namespace VinWallet.Domain.Models;

public partial class Wallet
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Balance { get; set; }

    public string? Currency { get; set; }

    public string? Type { get; set; }

    public string? ExtraField { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Status { get; set; }

    public Guid? OwnerId { get; set; }

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual ICollection<UserWallet> UserWallets { get; set; } = new List<UserWallet>();

    public virtual ICollection<WalletHistory> WalletHistories { get; set; } = new List<WalletHistory>();
}
