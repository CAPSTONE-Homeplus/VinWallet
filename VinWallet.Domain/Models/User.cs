using System;
using System.Collections.Generic;

namespace VinWallet.Domain.Models;

public partial class User
{
    public Guid Id { get; set; }

    public string? FullName { get; set; }

    public string? Status { get; set; }

    public string? ExtraField { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? RoomId { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<UserWallet> UserWallets { get; set; } = new List<UserWallet>();
}
