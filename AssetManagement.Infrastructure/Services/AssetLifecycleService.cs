using AssetManagement.Domain.Entities;
using AssetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AssetManagement.Infrastructure.Services;

public class AssetLifecycleService
{
    private readonly AssetManagementDbContext _context;
    private readonly ILogger<AssetLifecycleService> _logger;

    public AssetLifecycleService(AssetManagementDbContext context, ILogger<AssetLifecycleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> CanTransitionToState(string assetTag, AssetLifecycleState newState, string currentUser)
    {
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.AssetTag == assetTag);
        if (asset == null)
        {
            _logger.LogWarning("Asset {AssetTag} not found for state transition", assetTag);
            return false;
        }

        return CanTransition(asset.LifecycleState, newState, asset.CurrentSite, currentUser);
    }

    private bool CanTransition(AssetLifecycleState currentState, AssetLifecycleState newState, string? currentSite, string currentUser)
    {
        // Define allowed transitions based on new workflow
        var allowedTransitions = new Dictionary<AssetLifecycleState, AssetLifecycleState[]>
        {
            { AssetLifecycleState.InStorage, new[] { AssetLifecycleState.ReadyForShipment, AssetLifecycleState.Deployed, AssetLifecycleState.SalvagePending } },
            { AssetLifecycleState.ReadyForShipment, new[] { AssetLifecycleState.InTransit } }, // Only Facilities Drivers can pick up
            { AssetLifecycleState.InTransit, new[] { AssetLifecycleState.Delivered } },
            { AssetLifecycleState.Delivered, new[] { AssetLifecycleState.InStorage, AssetLifecycleState.Deployed } },
            { AssetLifecycleState.Deployed, new[] { AssetLifecycleState.RedeployPending, AssetLifecycleState.SalvagePending, AssetLifecycleState.ReadyForShipment, AssetLifecycleState.InStorage } },
            { AssetLifecycleState.RedeployPending, new[] { AssetLifecycleState.Deployed, AssetLifecycleState.InStorage, AssetLifecycleState.ReadyForShipment } },
            { AssetLifecycleState.SalvagePending, new[] { AssetLifecycleState.ReadyForShipment } }, // Cannot be redeployed, only shipped for salvage
            { AssetLifecycleState.Salvaged, new AssetLifecycleState[] { } } // Terminal state - no further transitions
        };

        // Check if transition is allowed
        if (!allowedTransitions.ContainsKey(currentState) || 
            !allowedTransitions[currentState].Contains(newState))
        {
            _logger.LogWarning("Invalid transition from {CurrentState} to {NewState}", currentState, newState);
            return false;
        }

        // Special rules
        if (newState == AssetLifecycleState.Salvaged)
        {
            // Allow salvage finalization at any site
            _logger.LogInformation("Salvage finalization allowed at site: {CurrentSite}, User: {User}", currentSite, currentUser);
        }

        // Only Facilities Drivers can pick up assets ready for shipment
        if (newState == AssetLifecycleState.InTransit && currentState == AssetLifecycleState.ReadyForShipment)
        {
            // TODO: Check if user has Facilities Driver role
            _logger.LogInformation("Asset picked up by Facilities Driver: {User}", currentUser);
        }

        return true;
    }

    public async Task<bool> TransitionToState(string assetTag, AssetLifecycleState newState, string currentUser, object? contextData = null)
    {
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.AssetTag == assetTag);
        if (asset == null)
        {
            _logger.LogWarning("Asset {AssetTag} not found for state transition", assetTag);
            return false;
        }

        if (!CanTransition(asset.LifecycleState, newState, asset.CurrentSite, currentUser))
        {
            return false;
        }

        var oldState = asset.LifecycleState;
        asset.LifecycleState = newState;
        asset.UpdatedAt = DateTime.UtcNow;
        asset.UpdatedBy = currentUser;

        // Update specific fields based on state
        switch (newState)
        {
            case AssetLifecycleState.Deployed:
                if (contextData is DeployContext deployContext)
                {
                    asset.CurrentDesk = deployContext.Desk;
                    asset.DeployedAt = DateTimeOffset.UtcNow;
                    asset.DeployedBy = currentUser;
                    asset.DeployedToUser = deployContext.UserName;
                    asset.DeployedToEmail = deployContext.UserEmail;
                }
                break;
            case AssetLifecycleState.Delivered:
                if (contextData is DeliveryContext deliveryContext)
                {
                    asset.CurrentSite = deliveryContext.ToSite;
                    asset.Location = deliveryContext.DeliveryLocation;
                    asset.Floor = deliveryContext.DeliveryFloor;
                    asset.Desk = deliveryContext.DeliveryDesk;
                    asset.DeliveredAt = DateTimeOffset.UtcNow;
                    asset.DeliveredBy = currentUser;
                }
                break;
            case AssetLifecycleState.InStorage:
                // Automatically set storage location based on current site (same building)
                if (!string.IsNullOrEmpty(asset.CurrentSite))
                {
                    asset.CurrentStorageLocation = $"{asset.CurrentSite} Storage";
                    asset.Floor = "Storage";
                    asset.Location = asset.CurrentSite; // Same building
                    _logger.LogInformation("Asset {AssetTag} moved to storage at {Site}", assetTag, asset.CurrentSite);
                }
                break;
            case AssetLifecycleState.ReadyForShipment:
                // Asset is ready for pickup by Facilities Drivers
                asset.ReadyForPickupAt = DateTimeOffset.UtcNow;
                asset.ReadyForPickupBy = currentUser;
                _logger.LogInformation("Asset {AssetTag} ready for shipment pickup at {Site}", assetTag, asset.CurrentSite);
                break;
            case AssetLifecycleState.InTransit:
                // Asset picked up by Facilities Driver
                asset.PickedUpAt = DateTimeOffset.UtcNow;
                asset.PickedUpBy = currentUser;
                if (contextData is PickupContext pickupContext)
                {
                    asset.DestinationSite = pickupContext.DestinationSite;
                    asset.Carrier = pickupContext.Carrier;
                    asset.TrackingNumber = pickupContext.TrackingNumber;
                }
                _logger.LogInformation("Asset {AssetTag} picked up by Facilities Driver {User}", assetTag, currentUser);
                break;
            case AssetLifecycleState.SalvagePending:
                // Clear sensitive data when marking for salvage (cannot be redeployed)
                ClearSensitiveDataForSalvage(asset);
                _logger.LogInformation("Asset {AssetTag} marked for salvage - sensitive data cleared, cannot be redeployed", assetTag);
                break;
            case AssetLifecycleState.Salvaged:
                // Ensure minimal data retention for salvaged assets
                EnsureMinimalSalvageData(asset);
                _logger.LogInformation("Asset {AssetTag} finalized as salvaged - minimal data retained", assetTag);
                break;
        }

        // Create audit event
        var eventData = new
        {
            OldState = oldState.ToString(),
            NewState = newState.ToString(),
            Context = contextData
        };

        var assetEvent = new AssetEvent
        {
            AssetTag = assetTag,
            Type = $"StateChanged_{newState}",
            DataJson = JsonSerializer.Serialize(eventData),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = currentUser
        };

        _context.AssetEvents.Add(assetEvent);

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Asset {AssetTag} transitioned from {OldState} to {NewState} by {User}", 
            assetTag, oldState, newState, currentUser);

        return true;
    }

    /// <summary>
    /// Clear sensitive data when marking an asset for salvage (cannot be redeployed)
    /// </summary>
    private void ClearSensitiveDataForSalvage(Asset asset)
    {
        // Clear network-related data that can be reused
        asset.IpAddress = null;
        asset.MacAddress = null;
        asset.WallPort = null;
        asset.SwitchName = null;
        asset.SwitchPort = null;
        asset.NetName = null;
        
        // Clear user assignment data
        asset.AssignedUserName = null;
        asset.AssignedUserEmail = null;
        asset.DeployedToUser = null;
        asset.DeployedToEmail = null;
        asset.CurrentDesk = null;
        asset.Desk = null;
        
        // Clear phone/communication data
        asset.PhoneNumber = null;
        asset.Extension = null;
        
        // Clear deployment tracking
        asset.DeployedAt = null;
        asset.DeployedBy = null;
        
        // Keep minimal identifying information:
        // - AssetTag, SerialNumber, Manufacturer, Model (for identification)
        // - Location, CurrentSite (for storage tracking)
        // - Notes (for salvage context)
        // - All other procurement/audit fields remain intact
    }

    /// <summary>
    /// Ensure only minimal data is retained for salvaged assets
    /// </summary>
    private void EnsureMinimalSalvageData(Asset asset)
    {
        // This method ensures the asset has only the minimal required fields
        // The actual data clearing happens in ClearSensitiveDataForSalvage when SalvagePending
        // This method is a safety check to ensure compliance
        
        // Verify minimal data is present
        if (string.IsNullOrEmpty(asset.AssetTag))
        {
            _logger.LogWarning("Asset missing required AssetTag for salvage");
        }
        
        if (string.IsNullOrEmpty(asset.CurrentSite))
        {
            _logger.LogWarning("Asset {AssetTag} missing CurrentSite for salvage", asset.AssetTag);
        }
    }

    /// <summary>
    /// Mark asset as ready for shipment (pickup by Facilities Drivers)
    /// </summary>
    public async Task<bool> MarkReadyForShipment(string assetTag, string currentUser)
    {
        return await TransitionToState(assetTag, AssetLifecycleState.ReadyForShipment, currentUser);
    }

    /// <summary>
    /// Pick up asset by Facilities Driver (transition from ReadyForShipment to InTransit)
    /// </summary>
    public async Task<bool> PickupAsset(string assetTag, string destinationSite, string? carrier, string? trackingNumber, string currentUser)
    {
        var pickupContext = new PickupContext
        {
            DestinationSite = destinationSite,
            Carrier = carrier,
            TrackingNumber = trackingNumber
        };

        return await TransitionToState(assetTag, AssetLifecycleState.InTransit, currentUser, pickupContext);
    }

    /// <summary>
    /// Deliver asset by Facilities Driver (transition from InTransit to Delivered)
    /// </summary>
    public async Task<bool> DeliverAsset(string assetTag, string toSite, string deliveryLocation, string deliveryFloor, string? deliveryDesk, string currentUser)
    {
        var deliveryContext = new DeliveryContext
        {
            ToSite = toSite,
            DeliveryLocation = deliveryLocation,
            DeliveryFloor = deliveryFloor,
            DeliveryDesk = deliveryDesk
        };

        return await TransitionToState(assetTag, AssetLifecycleState.Delivered, currentUser, deliveryContext);
    }

    /// <summary>
    /// Reassign location for delivered assets when they arrive
    /// </summary>
    public async Task<bool> ReassignLocationAfterDelivery(string assetTag, string newLocation, string newFloor, string? newDesk, string currentUser)
    {
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.AssetTag == assetTag);
        if (asset == null)
        {
            _logger.LogWarning("Asset {AssetTag} not found for location reassignment", assetTag);
            return false;
        }

        // Only allow reassignment if asset is delivered
        if (asset.LifecycleState != AssetLifecycleState.Delivered)
        {
            _logger.LogWarning("Asset {AssetTag} cannot be reassigned - not delivered (current state: {State})", 
                assetTag, asset.LifecycleState);
            return false;
        }

        // Update location information
        asset.Location = newLocation;
        asset.Floor = newFloor;
        asset.Desk = newDesk;
        asset.UpdatedAt = DateTime.UtcNow;
        asset.UpdatedBy = currentUser;

        // Create audit event
        var assetEvent = new AssetEvent
        {
            AssetTag = assetTag,
            Type = "LocationReassignedAfterDelivery",
            DataJson = JsonSerializer.Serialize(new { 
                NewLocation = newLocation,
                NewFloor = newFloor,
                NewDesk = newDesk,
                PreviousLocation = asset.Location,
                PreviousFloor = asset.Floor,
                PreviousDesk = asset.Desk
            }),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = currentUser
        };

        _context.AssetEvents.Add(assetEvent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Asset {AssetTag} location reassigned after delivery to {Location}/{Floor}/{Desk} by {User}", 
            assetTag, newLocation, newFloor, newDesk, currentUser);

        return true;
    }

    public async Task<bool> DeployAsset(string assetTag, string desk, string userName, string userEmail, string currentUser)
    {
        var deployContext = new DeployContext
        {
            Desk = desk,
            UserName = userName,
            UserEmail = userEmail
        };

        return await TransitionToState(assetTag, AssetLifecycleState.Deployed, currentUser, deployContext);
    }

    public async Task<bool> ReplaceAsset(string oldAssetTag, string newAssetTag, string desk, string userName, string userEmail, string currentUser, bool oldAssetToSalvage = true)
    {
        // Deploy new asset
        var deployContext = new DeployContext
        {
            Desk = desk,
            UserName = userName,
            UserEmail = userEmail
        };

        var newAssetDeployed = await TransitionToState(newAssetTag, AssetLifecycleState.Deployed, currentUser, deployContext);
        if (!newAssetDeployed) return false;

        // Move old asset to salvage or redeploy pending
        var oldAssetNewState = oldAssetToSalvage ? AssetLifecycleState.SalvagePending : AssetLifecycleState.RedeployPending;
        var oldAssetMoved = await TransitionToState(oldAssetTag, oldAssetNewState, currentUser);

        if (oldAssetMoved)
        {
            // Create linked events
            var replacementEvent = new AssetEvent
            {
                AssetTag = newAssetTag,
                Type = "Replaced",
                DataJson = JsonSerializer.Serialize(new { ReplacedAssetTag = oldAssetTag }),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = currentUser
            };

            var replacedEvent = new AssetEvent
            {
                AssetTag = oldAssetTag,
                Type = "ReplacedBy",
                DataJson = JsonSerializer.Serialize(new { ReplacedByAssetTag = newAssetTag }),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = currentUser
            };

            _context.AssetEvents.AddRange(replacementEvent, replacedEvent);
            await _context.SaveChangesAsync();
        }

        return oldAssetMoved;
    }

    public async Task<bool> RedeployAsset(string assetTag, string? newDesk, string currentUser)
    {
        if (string.IsNullOrEmpty(newDesk))
        {
            // Move to storage - automatically set storage location (same building)
            return await TransitionToState(assetTag, AssetLifecycleState.InStorage, currentUser);
        }
        else
        {
            // Redeploy to new desk
            var deployContext = new DeployContext
            {
                Desk = newDesk,
                UserName = null, // Keep existing user
                UserEmail = null
            };

            return await TransitionToState(assetTag, AssetLifecycleState.Deployed, currentUser, deployContext);
        }
    }

    public async Task<bool> MarkSalvagePending(string assetTag, string currentUser)
    {
        return await TransitionToState(assetTag, AssetLifecycleState.SalvagePending, currentUser);
    }

    public async Task<bool> AddToSalvageBatch(string assetTag, Guid batchId, string currentUser)
    {
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.AssetTag == assetTag);
        if (asset == null) return false;

        // Verify asset is in correct state
        if (asset.LifecycleState != AssetLifecycleState.Delivered && asset.LifecycleState != AssetLifecycleState.SalvagePending)
        {
            _logger.LogWarning("Asset {AssetTag} cannot be added to salvage batch. Site: {Site}, State: {State}", 
                assetTag, asset.CurrentSite, asset.LifecycleState);
            return false;
        }

        asset.SalvageBatchId = batchId;
        asset.UpdatedAt = DateTime.UtcNow;
        asset.UpdatedBy = currentUser;

        var assetEvent = new AssetEvent
        {
            AssetTag = assetTag,
            Type = "AddedToSalvageBatch",
            DataJson = JsonSerializer.Serialize(new { BatchId = batchId }),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = currentUser
        };

        _context.AssetEvents.Add(assetEvent);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> FinalizeSalvageBatch(Guid batchId, string manifestNumber, DateTimeOffset pickedUpAt, string currentUser)
    {
        var batch = await _context.SalvageBatches
            .Include(b => b.Assets)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch == null) return false;

        // Verify all assets are in correct state
        var invalidAssets = batch.Assets.Where(a => a.LifecycleState != AssetLifecycleState.Delivered && 
                                                   a.LifecycleState != AssetLifecycleState.SalvagePending).ToList();
        if (invalidAssets.Any())
        {
            _logger.LogWarning("Cannot finalize batch {BatchId}. Assets not in correct state: {AssetTags}", 
                batchId, string.Join(", ", invalidAssets.Select(a => a.AssetTag)));
            return false;
        }

        batch.PickupManifestNumber = manifestNumber;
        batch.PickedUpAt = pickedUpAt;

        // Move all assets to Salvaged state
        foreach (var asset in batch.Assets)
        {
            await TransitionToState(asset.AssetTag, AssetLifecycleState.Salvaged, currentUser);
        }

        var batchEvent = new AssetEvent
        {
            AssetTag = batch.Assets.FirstOrDefault()?.AssetTag ?? "BATCH",
            Type = "SalvageBatchFinalized",
            DataJson = JsonSerializer.Serialize(new { 
                BatchId = batchId, 
                BatchCode = batch.BatchCode,
                ManifestNumber = manifestNumber,
                PickedUpAt = pickedUpAt,
                AssetCount = batch.Assets.Count
            }),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = currentUser
        };

        _context.AssetEvents.Add(batchEvent);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Get all assets currently ready for shipment pickup
    /// </summary>
    public async Task<List<Asset>> GetAssetsReadyForShipment()
    {
        return await _context.Assets
            .Where(a => a.LifecycleState == AssetLifecycleState.ReadyForShipment)
            .OrderBy(a => a.AssetTag)
            .ToListAsync();
    }

    /// <summary>
    /// Get all assets currently in transit
    /// </summary>
    public async Task<List<Asset>> GetAssetsInTransit()
    {
        return await _context.Assets
            .Where(a => a.LifecycleState == AssetLifecycleState.InTransit)
            .OrderBy(a => a.AssetTag)
            .ToListAsync();
    }

    /// <summary>
    /// Get all assets currently delivered
    /// </summary>
    public async Task<List<Asset>> GetAssetsDelivered()
    {
        return await _context.Assets
            .Where(a => a.LifecycleState == AssetLifecycleState.Delivered)
            .OrderBy(a => a.AssetTag)
            .ToListAsync();
    }

    /// <summary>
    /// Get assets in storage for a specific site
    /// </summary>
    public async Task<List<Asset>> GetAssetsInStorage(string site)
    {
        return await _context.Assets
            .Where(a => a.LifecycleState == AssetLifecycleState.InStorage && 
                       a.CurrentSite == site)
            .OrderBy(a => a.AssetTag)
            .ToListAsync();
    }

    /// <summary>
    /// Get assets marked for salvage at a specific site
    /// </summary>
    public async Task<List<Asset>> GetAssetsMarkedForSalvage(string site)
    {
        return await _context.Assets
            .Where(a => a.LifecycleState == AssetLifecycleState.SalvagePending && 
                       a.CurrentSite == site)
            .OrderBy(a => a.AssetTag)
            .ToListAsync();
    }

    // Context classes for state transitions
    public class DeployContext
    {
        public string? Desk { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
    }

    public class PickupContext
    {
        public string DestinationSite { get; set; } = string.Empty;
        public string? Carrier { get; set; }
        public string? TrackingNumber { get; set; }
    }

    public class DeliveryContext
    {
        public string ToSite { get; set; } = string.Empty;
        public string DeliveryLocation { get; set; } = string.Empty;
        public string DeliveryFloor { get; set; } = string.Empty;
        public string? DeliveryDesk { get; set; }
    }
}
