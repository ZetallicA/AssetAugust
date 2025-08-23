namespace AssetManagement.Web.Models;

public class CreateTransferRequest
{
    public string AssetTag { get; set; } = string.Empty;
    public string ToSite { get; set; } = string.Empty;
    public string? ToStorageBin { get; set; }
    public string? Carrier { get; set; }
    public string? TrackingNumber { get; set; }
}

public class ShipTransferRequest
{
    public Guid TransferId { get; set; }
}

public class ReceiveTransferRequest
{
    public Guid TransferId { get; set; }
    public string ReceivedBy { get; set; } = string.Empty;
}

public class DeployAssetRequest
{
    public string AssetTag { get; set; } = string.Empty;
    public string Desk { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
}

public class ReplaceAssetRequest
{
    public string OldAssetTag { get; set; } = string.Empty;
    public string NewAssetTag { get; set; } = string.Empty;
    public string Desk { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public bool OldAssetToSalvage { get; set; } = true;
}

public class RedeployAssetRequest
{
    public string AssetTag { get; set; } = string.Empty;
    public string? NewDesk { get; set; }
}

public class ReassignLocationRequest
{
    public string AssetTag { get; set; } = string.Empty;
    public string NewLocation { get; set; } = string.Empty;
    public string NewFloor { get; set; } = string.Empty;
    public string? NewDesk { get; set; }
}

public class PickupAssetRequest
{
    public string AssetTag { get; set; } = string.Empty;
    public string DestinationSite { get; set; } = string.Empty;
    public string? Carrier { get; set; }
    public string? TrackingNumber { get; set; }
}

public class DeliverAssetRequest
{
    public string AssetTag { get; set; } = string.Empty;
    public string ToSite { get; set; } = string.Empty;
    public string DeliveryLocation { get; set; } = string.Empty;
    public string DeliveryFloor { get; set; } = string.Empty;
    public string? DeliveryDesk { get; set; }
}

public class CreateSalvageBatchRequest
{
    public string PickupVendor { get; set; } = "RecycleCo";
}

public class AddToSalvageBatchRequest
{
    public string AssetTag { get; set; } = string.Empty;
    public Guid BatchId { get; set; }
}

public class FinalizeSalvageBatchRequest
{
    public Guid BatchId { get; set; }
    public string ManifestNumber { get; set; } = string.Empty;
    public DateTimeOffset PickedUpAt { get; set; }
}

public class AssetTransferDto
{
    public Guid Id { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string FromSite { get; set; } = string.Empty;
    public string ToSite { get; set; } = string.Empty;
    public string? FromStorageBin { get; set; }
    public string? ToStorageBin { get; set; }
    public string? Carrier { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? ReceivedBy { get; set; }
    public string State { get; set; } = string.Empty;
}

public class SalvageBatchDto
{
    public Guid Id { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string PickupVendor { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PickedUpAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? PickupManifestNumber { get; set; }
    public int AssetCount { get; set; }
}

public class AssetEventDto
{
    public Guid Id { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? DataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class SalvageManifestDto
{
    public string BatchCode { get; set; } = string.Empty;
    public string PickupVendor { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public List<SalvageManifestItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public decimal? TotalWeight { get; set; }
}

public class SalvageManifestItemDto
{
    public string AssetTag { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? Notes { get; set; }
    public decimal? Weight { get; set; }
    public string? BinNumber { get; set; }
}

public class BulkCheckoutRequest
{
    public List<string> AssetTags { get; set; } = new();
    public string Location { get; set; } = string.Empty;
    public string Floor { get; set; } = string.Empty;
    public string? Desk { get; set; }
    public string User { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
