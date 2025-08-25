using AssetManagement.Domain.Entities;

namespace AssetManagement.Web.Models;

public class MyWorkViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public string[] Roles { get; set; } = Array.Empty<string>();
    public string[] Permissions { get; set; } = Array.Empty<string>();
    public List<AssetRequest> PendingRequests { get; set; } = new();
    public List<AssetRequest> MyRequests { get; set; } = new();
    public List<AssetRequest> PendingApprovals { get; set; } = new();
    public List<AuditLog> RecentActivity { get; set; } = new();
    public List<Workflow> Workflows { get; set; } = new();
}
