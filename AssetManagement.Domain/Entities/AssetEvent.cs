using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Domain.Entities;

public class AssetEvent
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string AssetTag { get; set; } = default!;
    
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = default!;        // Created, Import, TransferCreated, Shipped, Received, Deployed, Replaced, Redeploy, SalvagePending, Salvaged
    
    [StringLength(4000)]
    public string? DataJson { get; set; }               // serialize deltas/context
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    [Required]
    [StringLength(100)]
    public string CreatedBy { get; set; } = default!;
    
    // Navigation properties
    public Asset? Asset { get; set; }
}
