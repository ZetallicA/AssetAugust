using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Domain.Entities;

public class AssetHistory
{
    public int Id { get; set; }
    
    public int AssetId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty; // Created, Updated, Assigned, Unassigned, Moved, etc.
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [StringLength(1000)]
    public string? OldValues { get; set; }
    
    [StringLength(1000)]
    public string? NewValues { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public string? UserId { get; set; }
    
    [StringLength(100)]
    public string? UserName { get; set; }
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    // Navigation properties
    public Asset Asset { get; set; } = null!;
    public ApplicationUser? User { get; set; }
}
