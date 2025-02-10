using System;
using System.Collections.Generic;

namespace VinWallet.Domain.Models;

public partial class RolePermission
{
    public Guid Id { get; set; }

    public Guid? RoleId { get; set; }

    public Guid? PermissionId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Status { get; set; }

    public virtual Permission? Permission { get; set; }

    public virtual Role? Role { get; set; }
}
