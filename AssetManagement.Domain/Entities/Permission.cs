using Microsoft.AspNetCore.Identity;

namespace AssetManagement.Domain.Entities;

public class Permission
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    
    // Navigation properties
    public List<RolePermission> RolePermissions { get; set; } = new();
    public List<Assignment> Assignments { get; set; } = new();
}
