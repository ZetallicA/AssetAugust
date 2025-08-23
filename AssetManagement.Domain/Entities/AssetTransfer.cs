using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Domain.Entities;

public class AssetTransfer
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string AssetTag { get; set; } = default!;
    
    [Required]
    [StringLength(50)]
    public string FromSite { get; set; } = default!;       // e.g., 66JOHN (Storage) or LIC
    
    [Required]
    [StringLength(50)]
    public string ToSite { get; set; } = default!;
    
    [StringLength(100)]
    public string? FromStorageBin { get; set; }            // optional bin code
    
    [StringLength(100)]
    public string? ToStorageBin { get; set; }
    
    [StringLength(100)]
    public string? Carrier { get; set; }
    
    [StringLength(100)]
    public string? TrackingNumber { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public DateTimeOffset? ShippedAt { get; set; }
    
    public DateTimeOffset? ReceivedAt { get; set; }
    
    [Required]
    [StringLength(100)]
    public string CreatedBy { get; set; } = default!;
    
    [StringLength(100)]
    public string? ReceivedBy { get; set; }
    
    [Required]
    [StringLength(20)]
    public string State { get; set; } = "Draft";           // Draft -> Shipped -> Received -> Closed
    
    // Navigation properties
    public Asset? Asset { get; set; }
}
