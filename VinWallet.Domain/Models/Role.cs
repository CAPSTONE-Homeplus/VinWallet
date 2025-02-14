using System;
using System.Collections.Generic;

namespace VinWallet.Domain.Models;

public partial class Role
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
