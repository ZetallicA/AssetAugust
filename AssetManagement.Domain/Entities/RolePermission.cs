using Microsoft.AspNetCore.Identity;

namespace AssetManagement.Domain.Entities;

public class RolePermission
{
    public string RoleId { get; set; } = string.Empty;
    public int PermissionId { get; set; }
    
    // Navigation properties
    public IdentityRole Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
