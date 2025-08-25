using Microsoft.AspNetCore.Identity;

namespace AssetManagement.Domain.Entities;

public class UserGroup
{
    public string UserId { get; set; } = string.Empty;
    public int GroupId { get; set; }
    
    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public Group Group { get; set; } = null!;
}
