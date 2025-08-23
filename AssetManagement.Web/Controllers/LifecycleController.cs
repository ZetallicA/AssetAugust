using AssetManagement.Domain.Entities;
using AssetManagement.Infrastructure.Services;
using AssetManagement.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssetManagement.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LifecycleController : ControllerBase
{
    private readonly AssetLifecycleService _lifecycleService;
    private readonly TransferService _transferService;
    private readonly SalvageService _salvageService;
    private readonly ILogger<LifecycleController> _logger;

    public LifecycleController(
        AssetLifecycleService lifecycleService,
        TransferService transferService,
        SalvageService salvageService,
        ILogger<LifecycleController> logger)
    {
        _lifecycleService = lifecycleService;
        _transferService = transferService;
        _salvageService = salvageService;
        _logger = logger;
    }

    [HttpPost("deploy")]
    public async Task<IActionResult> DeployAsset([FromBody] DeployAssetRequest request)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var success = await _lifecycleService.DeployAsset(
                request.AssetTag, 
                request.Desk, 
                request.UserName, 
                request.UserEmail, 
                currentUser);

            if (success)
            {
                return Ok(new { message = "Asset deployed successfully" });
            }

            return BadRequest(new { message = "Failed to deploy asset. Check if asset exists and is in a valid state." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying asset {AssetTag}", request.AssetTag);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("replace")]
    public async Task<IActionResult> ReplaceAsset([FromBody] ReplaceAssetRequest request)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var success = await _lifecycleService.ReplaceAsset(
                request.OldAssetTag,
                request.NewAssetTag,
                request.Desk,
                request.UserName,
                request.UserEmail,
                currentUser,
                request.OldAssetToSalvage);

            if (success)
            {
                return Ok(new { message = "Asset replacement completed successfully" });
            }

            return BadRequest(new { message = "Failed to replace asset. Check if both assets exist and are in valid states." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing asset {OldAssetTag} with {NewAssetTag}", request.OldAssetTag, request.NewAssetTag);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("redeploy")]
    public async Task<IActionResult> RedeployAsset([FromBody] RedeployAssetRequest request)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var success = await _lifecycleService.RedeployAsset(
                request.AssetTag,
                request.NewDesk,
                currentUser);

            if (success)
            {
                var action = string.IsNullOrEmpty(request.NewDesk) ? "moved to storage" : "redeployed";
                return Ok(new { message = $"Asset {action} successfully" });
            }

            return BadRequest(new { message = "Failed to redeploy asset. Check if asset exists and is in a valid state." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error redeploying asset {AssetTag}", request.AssetTag);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("mark-salvage-pending")]
    public async Task<IActionResult> MarkSalvagePending([FromBody] string assetTag)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var success = await _lifecycleService.MarkSalvagePending(assetTag, currentUser);

            if (success)
            {
                return Ok(new { message = "Asset marked for salvage successfully" });
            }

            return BadRequest(new { message = "Failed to mark asset for salvage. Check if asset exists and is in a valid state." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking asset {AssetTag} for salvage", assetTag);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("mark-ready-for-shipment")]
    public async Task<IActionResult> MarkReadyForShipment([FromBody] string assetTag)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var success = await _lifecycleService.MarkReadyForShipment(assetTag, currentUser);

            if (success)
            {
                return Ok(new { message = "Asset marked ready for shipment pickup" });
            }

            return BadRequest(new { message = "Failed to mark asset ready for shipment. Check if asset exists and is in a valid state." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking asset {AssetTag} ready for shipment", assetTag);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("pickup-asset")]
    [Authorize(Roles = "FacilitiesDriver,Admin")]
    public async Task<IActionResult> PickupAsset([FromBody] PickupAssetRequest request)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var success = await _lifecycleService.PickupAsset(
                request.AssetTag,
                request.DestinationSite,
                request.Carrier,
                request.TrackingNumber,
                currentUser);

            if (success)
            {
                return Ok(new { message = "Asset picked up successfully" });
            }

            return BadRequest(new { message = "Failed to pickup asset. Check if asset exists and is ready for shipment." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error picking up asset {AssetTag}", request.AssetTag);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("deliver-asset")]
    [Authorize(Roles = "FacilitiesDriver,Admin")]
    public async Task<IActionResult> DeliverAsset([FromBody] DeliverAssetRequest request)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var success = await _lifecycleService.DeliverAsset(
                request.AssetTag,
                request.ToSite,
                request.DeliveryLocation,
                request.DeliveryFloor,
                request.DeliveryDesk,
                currentUser);

            if (success)
            {
                return Ok(new { message = "Asset delivered successfully" });
            }

            return BadRequest(new { message = "Failed to deliver asset. Check if asset exists and is in transit." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error delivering asset {AssetTag}", request.AssetTag);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("reassign-location-after-delivery")]
    public async Task<IActionResult> ReassignLocationAfterDelivery([FromBody] ReassignLocationRequest request)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var success = await _lifecycleService.ReassignLocationAfterDelivery(
                request.AssetTag,
                request.NewLocation,
                request.NewFloor,
                request.NewDesk,
                currentUser);

            if (success)
            {
                return Ok(new { message = "Asset location reassigned successfully" });
            }

            return BadRequest(new { message = "Failed to reassign asset location. Check if asset exists and is delivered." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reassigning location for asset {AssetTag}", request.AssetTag);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateTransferRequest request)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var transfer = await _transferService.CreateTransfer(
                request.AssetTag,
                request.ToSite,
                request.ToStorageBin,
                request.Carrier,
                request.TrackingNumber,
                currentUser);

            if (transfer != null)
            {
                var transferDto = new AssetTransferDto
                {
                    Id = transfer.Id,
                    AssetTag = transfer.AssetTag,
                    FromSite = transfer.FromSite,
                    ToSite = transfer.ToSite,
                    FromStorageBin = transfer.FromStorageBin,
                    ToStorageBin = transfer.ToStorageBin,
                    Carrier = transfer.Carrier,
                    TrackingNumber = transfer.TrackingNumber,
                    CreatedAt = transfer.CreatedAt,
                    ShippedAt = transfer.ShippedAt,
                    ReceivedAt = transfer.ReceivedAt,
                    CreatedBy = transfer.CreatedBy,
                    ReceivedBy = transfer.ReceivedBy,
                    State = transfer.State
                };

                return Ok(transferDto);
            }

            return BadRequest(new { message = "Failed to create transfer. Check if asset exists and is in a valid state." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transfer for asset {AssetTag}", request.AssetTag);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("transfer/ship")]
    public async Task<IActionResult> ShipTransfer([FromBody] ShipTransferRequest request)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var success = await _transferService.ShipTransfer(request.TransferId, currentUser);

            if (success)
            {
                return Ok(new { message = "Transfer shipped successfully" });
            }

            return BadRequest(new { message = "Failed to ship transfer. Check if transfer exists and is in Draft state." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shipping transfer {TransferId}", request.TransferId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("transfer/receive")]
    public async Task<IActionResult> ReceiveTransfer([FromBody] ReceiveTransferRequest request)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var success = await _transferService.ReceiveTransfer(request.TransferId, request.ReceivedBy, currentUser);

            if (success)
            {
                return Ok(new { message = "Transfer received successfully" });
            }

            return BadRequest(new { message = "Failed to receive transfer. Check if transfer exists and is in Shipped state." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving transfer {TransferId}", request.TransferId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("transfers/{assetTag}")]
    public async Task<IActionResult> GetAssetTransfers(string assetTag)
    {
        try
        {
            var transfers = await _transferService.GetTransfersByAsset(assetTag);
            var transferDtos = transfers.Select(t => new AssetTransferDto
            {
                Id = t.Id,
                AssetTag = t.AssetTag,
                FromSite = t.FromSite,
                ToSite = t.ToSite,
                FromStorageBin = t.FromStorageBin,
                ToStorageBin = t.ToStorageBin,
                Carrier = t.Carrier,
                TrackingNumber = t.TrackingNumber,
                CreatedAt = t.CreatedAt,
                ShippedAt = t.ShippedAt,
                ReceivedAt = t.ReceivedAt,
                CreatedBy = t.CreatedBy,
                ReceivedBy = t.ReceivedBy,
                State = t.State
            }).ToList();

            return Ok(transferDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transfers for asset {AssetTag}", assetTag);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("transfers/pending")]
    public async Task<IActionResult> GetPendingTransfers([FromQuery] string? site)
    {
        try
        {
            var transfers = await _transferService.GetPendingTransfers(site);
            var transferDtos = transfers.Select(t => new AssetTransferDto
            {
                Id = t.Id,
                AssetTag = t.AssetTag,
                FromSite = t.FromSite,
                ToSite = t.ToSite,
                FromStorageBin = t.FromStorageBin,
                ToStorageBin = t.ToStorageBin,
                Carrier = t.Carrier,
                TrackingNumber = t.TrackingNumber,
                CreatedAt = t.CreatedAt,
                ShippedAt = t.ShippedAt,
                ReceivedAt = t.ReceivedAt,
                CreatedBy = t.CreatedBy,
                ReceivedBy = t.ReceivedBy,
                State = t.State
            }).ToList();

            return Ok(transferDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending transfers for site {Site}", site);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("salvage/batch")]
    [Authorize(Roles = "SiteTech,JohnStOps,Admin")]
    public async Task<IActionResult> CreateSalvageBatch([FromBody] CreateSalvageBatchRequest request)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var batch = await _salvageService.CreateSalvageBatch(request.PickupVendor, currentUser);

            var batchDto = new SalvageBatchDto
            {
                Id = batch.Id,
                BatchCode = batch.BatchCode,
                PickupVendor = batch.PickupVendor,
                CreatedAt = batch.CreatedAt,
                PickedUpAt = batch.PickedUpAt,
                CreatedBy = batch.CreatedBy,
                PickupManifestNumber = batch.PickupManifestNumber,
                AssetCount = 0
            };

            return Ok(batchDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating salvage batch");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("salvage/batch/add")]
    [Authorize(Roles = "SiteTech,JohnStOps,Admin")]
    public async Task<IActionResult> AddToSalvageBatch([FromBody] AddToSalvageBatchRequest request)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var success = await _salvageService.AddAssetToBatch(request.AssetTag, request.BatchId, currentUser);

            if (success)
            {
                return Ok(new { message = "Asset added to salvage batch successfully" });
            }

            return BadRequest(new { message = "Failed to add asset to salvage batch. Check if asset is in correct state." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding asset {AssetTag} to salvage batch {BatchId}", request.AssetTag, request.BatchId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("salvage/batch/finalize")]
    [Authorize(Roles = "SiteTech,JohnStOps,Admin")]
    public async Task<IActionResult> FinalizeSalvageBatch([FromBody] FinalizeSalvageBatchRequest request)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var success = await _salvageService.FinalizeSalvageBatch(
                request.BatchId, 
                request.ManifestNumber, 
                request.PickedUpAt, 
                currentUser);

            if (success)
            {
                return Ok(new { message = "Salvage batch finalized successfully" });
            }

            return BadRequest(new { message = "Failed to finalize salvage batch. Check if all assets are in correct state." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing salvage batch {BatchId}", request.BatchId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("salvage/batch/{batchId}")]
    public async Task<IActionResult> GetSalvageBatch(Guid batchId)
    {
        try
        {
            var batch = await _salvageService.GetSalvageBatch(batchId);
            if (batch == null)
            {
                return NotFound(new { message = "Salvage batch not found" });
            }

            var batchDto = new SalvageBatchDto
            {
                Id = batch.Id,
                BatchCode = batch.BatchCode,
                PickupVendor = batch.PickupVendor,
                CreatedAt = batch.CreatedAt,
                PickedUpAt = batch.PickedUpAt,
                CreatedBy = batch.CreatedBy,
                PickupManifestNumber = batch.PickupManifestNumber,
                AssetCount = batch.Assets.Count
            };

            return Ok(batchDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting salvage batch {BatchId}", batchId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("salvage/batch/{batchId}/manifest")]
    public async Task<IActionResult> GetSalvageManifest(Guid batchId)
    {
        try
        {
            var manifest = await _salvageService.GenerateManifest(batchId);
            var manifestDto = new SalvageManifestDto
            {
                BatchCode = manifest.BatchCode,
                PickupVendor = manifest.PickupVendor,
                CreatedAt = manifest.CreatedAt,
                CreatedBy = manifest.CreatedBy,
                Items = manifest.Items.Select(i => new SalvageManifestItemDto
                {
                    AssetTag = i.AssetTag,
                    SerialNumber = i.SerialNumber,
                    Manufacturer = i.Manufacturer,
                    Model = i.Model,
                    Notes = i.Notes,
                    Weight = i.Weight,
                    BinNumber = i.BinNumber
                }).ToList(),
                TotalCount = manifest.TotalCount,
                TotalWeight = manifest.TotalWeight
            };

            return Ok(manifestDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating manifest for batch {BatchId}", batchId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("salvage/eligible")]
    [Authorize(Roles = "SiteTech,JohnStOps,Admin")]
    public async Task<IActionResult> GetEligibleAssetsForSalvage([FromQuery] string site = "66JOHN")
    {
        try
        {
            var assets = await _salvageService.GetAssetsEligibleForSalvage(site);
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting eligible assets for salvage at site {Site}", site);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("assets/in-transit")]
    public async Task<IActionResult> GetAssetsInTransit()
    {
        try
        {
            var assets = await _lifecycleService.GetAssetsInTransit();
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assets in transit");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("assets/ready-for-shipment")]
    public async Task<IActionResult> GetAssetsReadyForShipment()
    {
        try
        {
            var assets = await _lifecycleService.GetAssetsReadyForShipment();
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assets ready for shipment");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("assets/delivered")]
    public async Task<IActionResult> GetAssetsDelivered()
    {
        try
        {
            var assets = await _lifecycleService.GetAssetsDelivered();
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting delivered assets");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("assets/in-storage")]
    public async Task<IActionResult> GetAssetsInStorage([FromQuery] string site)
    {
        try
        {
            var assets = await _lifecycleService.GetAssetsInStorage(site);
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assets in storage at site {Site}", site);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("assets/marked-for-salvage")]
    public async Task<IActionResult> GetAssetsMarkedForSalvage([FromQuery] string site)
    {
        try
        {
            var assets = await _lifecycleService.GetAssetsMarkedForSalvage(site);
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assets marked for salvage at site {Site}", site);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
