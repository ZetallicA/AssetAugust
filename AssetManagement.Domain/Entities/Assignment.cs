using Microsoft.AspNetCore.Identity;

namespace AssetManagement.Domain.Entities;

public class Assignment
{
    public long Id { get; set; }
    public string SubjectType { get; set; } = string.Empty;
    public string SubjectId { get; set; } = string.Empty;
    public int PermissionId { get; set; }
    public string ScopeType { get; set; } = string.Empty;
    public string? ScopeId { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    
    // Navigation properties
    public Permission Permission { get; set; } = null!;
}
