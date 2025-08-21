using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Domain.Entities;

public class Floor
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string? FloorNumber { get; set; }
    
    [StringLength(200)]
    public string? Description { get; set; }
    
    [StringLength(255)]
    public string? FloorPlanImagePath { get; set; }
    
    public int BuildingId { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;
    
    // Navigation properties
    public Building Building { get; set; } = null!;
    public List<Asset> Assets { get; set; } = new();
}
