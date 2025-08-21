using Microsoft.AspNetCore.Identity;

namespace AssetManagement.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    
    public string? LastName { get; set; }
    
    public string? Department { get; set; }
    
    public string? Title { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public List<Asset> AssignedAssets { get; set; } = new();
    public List<AssetRequest> SubmittedRequests { get; set; } = new();
    public List<AssetRequest> ApprovedRequests { get; set; } = new();
    public List<AssetHistory> AssetHistory { get; set; } = new();
}
