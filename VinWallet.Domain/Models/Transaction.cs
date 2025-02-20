using System;
using System.Collections.Generic;

namespace VinWallet.Domain.Models;

public partial class Transaction
{
    public Guid Id { get; set; }

    public Guid? WalletId { get; set; }

    public Guid? UserId { get; set; }

    public Guid? PaymentMethodId { get; set; }

    public string? Amount { get; set; }

    public string? Type { get; set; }

    public string? Note { get; set; }

    public DateTime? TransactionDate { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Code { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? OrderId { get; set; }

    public string? PaymentUrl { get; set; }

    public virtual Category? Category { get; set; }

    public virtual PaymentMethod? PaymentMethod { get; set; }

    public virtual Wallet? Wallet { get; set; }
}
