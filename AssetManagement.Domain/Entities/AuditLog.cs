using Microsoft.AspNetCore.Identity;

namespace AssetManagement.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public DateTime WhenUtc { get; set; }
    public string? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Target { get; set; }
    public string? DetailsJson { get; set; }
    
    // Navigation properties
    public ApplicationUser? ActorUser { get; set; }
}
