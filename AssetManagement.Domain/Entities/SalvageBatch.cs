using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Domain.Entities;

public class SalvageBatch
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string BatchCode { get; set; } = default!;  // e.g., SAL-2025-08-001
    
    [Required]
    [StringLength(100)]
    public string PickupVendor { get; set; } = "RecycleCo";
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public DateTimeOffset? PickedUpAt { get; set; }
    
    [Required]
    [StringLength(100)]
    public string CreatedBy { get; set; } = default!;
    
    [StringLength(100)]
    public string? PickupManifestNumber { get; set; }
    
    // Navigation properties
    public List<Asset> Assets { get; set; } = new();
}
