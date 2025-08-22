using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Domain.Entities;

public class Building
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? Address { get; set; }
    
    [StringLength(100)]
    public string? City { get; set; }
    
    [StringLength(50)]
    public string? State { get; set; }
    
    [StringLength(20)]
    public string? ZipCode { get; set; }
    
    [StringLength(100)]
    public string? Country { get; set; } = "USA";
    
    [StringLength(50)]
    public string? BuildingCode { get; set; }
    
    // Temporarily commented out until database migration is complete
    // [StringLength(20)]
    // public string? Phone { get; set; }
    
    // [StringLength(20)]
    // public string? Fax { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;
    
    // Navigation properties
    public List<Floor> Floors { get; set; } = new();
    public List<Asset> Assets { get; set; } = new();
}
