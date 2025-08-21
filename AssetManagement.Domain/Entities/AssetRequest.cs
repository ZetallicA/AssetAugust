using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Domain.Entities;

public class AssetRequest
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    [StringLength(50)]
    public string RequestType { get; set; } = string.Empty; // New, Repair, Replacement, Decommission
    
    public int? AssetId { get; set; }
    
    [StringLength(100)]
    public string? RequestedAssetType { get; set; }
    
    [StringLength(100)]
    public string? RequestedManufacturer { get; set; }
    
    [StringLength(100)]
    public string? RequestedModel { get; set; }
    
    [StringLength(50)]
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
    
    [StringLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, In Progress, Completed
    
    [StringLength(1000)]
    public string? ApprovalNotes { get; set; }
    
    [StringLength(1000)]
    public string? RejectionReason { get; set; }
    
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? ApprovalDate { get; set; }
    
    public DateTime? CompletionDate { get; set; }
    
    public DateTime? DueDate { get; set; }
    
    public decimal? EstimatedCost { get; set; }
    
    public decimal? ActualCost { get; set; }
    
    // Foreign keys
    public string RequesterId { get; set; } = string.Empty;
    public string? ApproverId { get; set; }
    public string? AssignedToId { get; set; }
    
    // Navigation properties
    public ApplicationUser Requester { get; set; } = null!;
    public ApplicationUser? Approver { get; set; }
    public ApplicationUser? AssignedTo { get; set; }
    public Asset? Asset { get; set; }
}
