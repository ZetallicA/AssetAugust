using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Domain.Entities;

public enum AssetLifecycleState 
{ 
    InStorage = 0,           // Asset is in storage at a site (same building, floor = "Storage")
    ReadyForShipment = 1,    // Asset is ready for pickup by Facilities Drivers
    InTransit = 2,           // Asset is being transported by Facilities Drivers
    Delivered = 3,           // Asset has been delivered to destination
    Deployed = 4,            // Asset is deployed to a user/desk
    RedeployPending = 5,     // Asset is scheduled for redeployment
    SalvagePending = 6,      // Asset is marked for salvage (cannot be redeployed)
    Salvaged = 7             // Asset has been processed through salvage (locked/read-only)
}

public class Asset
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string AssetTag { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? SerialNumber { get; set; }
    
    [StringLength(100)]
    public string? ServiceTag { get; set; }
    
    [StringLength(100)]
    public string? Manufacturer { get; set; }
    
    [StringLength(100)]
    public string? Model { get; set; }
    
    [StringLength(50)]
    public string? Category { get; set; }
    
    [StringLength(100)]
    public string? NetName { get; set; }
    
    [StringLength(100)]
    public string? AssignedUserName { get; set; }
    
    [StringLength(255)]
    [EmailAddress]
    public string? AssignedUserEmail { get; set; }
    
    [StringLength(100)]
    public string? Manager { get; set; }
    
    [StringLength(100)]
    public string? Department { get; set; }
    
    [StringLength(100)]
    public string? Unit { get; set; }
    
    [StringLength(200)]
    public string? Location { get; set; }
    
    [StringLength(50)]
    public string? Floor { get; set; }
    
    [StringLength(50)]
    public string? Desk { get; set; }
    
    [StringLength(50)]
    public string? Status { get; set; }
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    [StringLength(17)]
    public string? MacAddress { get; set; }
    
    [StringLength(50)]
    public string? WallPort { get; set; }
    
    [StringLength(100)]
    public string? SwitchName { get; set; }
    
    [StringLength(50)]
    public string? SwitchPort { get; set; }
    
    [StringLength(50)]
    public string? PhoneNumber { get; set; }
    
    [StringLength(20)]
    public string? Extension { get; set; }
    
    [StringLength(15)]
    public string? Imei { get; set; }
    
    [StringLength(50)]
    public string? CardNumber { get; set; }
    
    [StringLength(100)]
    public string? OsVersion { get; set; }
    
    [StringLength(255)]
    public string? License1 { get; set; }
    
    [StringLength(255)]
    public string? License2 { get; set; }
    
    [StringLength(255)]
    public string? License3 { get; set; }
    
    [StringLength(255)]
    public string? License4 { get; set; }
    
    [StringLength(255)]
    public string? License5 { get; set; }
    
    public decimal? PurchasePrice { get; set; }
    
    [StringLength(100)]
    public string? OrderNumber { get; set; }
    
    [StringLength(100)]
    public string? Vendor { get; set; }
    
    [StringLength(255)]
    public string? VendorInvoice { get; set; }
    
    public DateTime? PurchaseDate { get; set; }
    
    public DateTime? WarrantyStart { get; set; }
    
    public DateTime? WarrantyEndDate { get; set; }
    
    [StringLength(1000)]
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;
    
    public DateTime? UpdatedAt { get; set; }
    
    [StringLength(100)]
    public string? UpdatedBy { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Lifecycle properties
    public AssetLifecycleState LifecycleState { get; set; } = AssetLifecycleState.InStorage;
    public bool IsEditable => LifecycleState != AssetLifecycleState.Salvaged;
    
    [StringLength(100)]
    public string? CurrentStorageLocation { get; set; }   // e.g., LIC Storage, John St Storage
    
    [StringLength(50)]
    public string? CurrentSite { get; set; }              // e.g., LIC, 66JOHN, etc.
    
    [StringLength(50)]
    public string? CurrentDesk { get; set; }              // alias to Desk for deployed assets
    
    public DateTimeOffset? DeployedAt { get; set; }
    
    [StringLength(100)]
    public string? DeployedBy { get; set; }
    
    [StringLength(100)]
    public string? DeployedToUser { get; set; }
    
    [StringLength(255)]
    [EmailAddress]
    public string? DeployedToEmail { get; set; }
    
    // Shipment tracking fields
    public DateTimeOffset? ReadyForPickupAt { get; set; }
    
    [StringLength(100)]
    public string? ReadyForPickupBy { get; set; }
    
    public DateTimeOffset? PickedUpAt { get; set; }
    
    [StringLength(100)]
    public string? PickedUpBy { get; set; }
    
    [StringLength(50)]
    public string? DestinationSite { get; set; }
    
    [StringLength(100)]
    public string? Carrier { get; set; }
    
    [StringLength(100)]
    public string? TrackingNumber { get; set; }
    
    public DateTimeOffset? DeliveredAt { get; set; }
    
    [StringLength(100)]
    public string? DeliveredBy { get; set; }
    
    // Salvage batch reference
    public Guid? SalvageBatchId { get; set; }
    public SalvageBatch? SalvageBatch { get; set; }
    
    // Navigation properties
    public int? BuildingId { get; set; }
    public Building? Building { get; set; }
    
    public int? FloorId { get; set; }
    public Floor? FloorEntity { get; set; }
    
    public string? AssignedUserId { get; set; }
    public ApplicationUser? AssignedUser { get; set; }
    
    public List<AssetRequest> AssetRequests { get; set; } = new();
    public List<AssetHistory> AssetHistory { get; set; } = new();
    public List<AssetTransfer> AssetTransfers { get; set; } = new();
    public List<AssetEvent> AssetEvents { get; set; } = new();
}
