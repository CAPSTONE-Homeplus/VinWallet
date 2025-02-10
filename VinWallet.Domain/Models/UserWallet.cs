using System;
using System.Collections.Generic;

namespace VinWallet.Domain.Models;

public partial class UserWallet
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public Guid? WalletId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Status { get; set; }

    public virtual User? User { get; set; }

    public virtual Wallet? Wallet { get; set; }
}
