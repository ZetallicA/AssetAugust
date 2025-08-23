using AssetManagement.Domain.Entities;
using AssetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AssetManagement.Infrastructure.Services;

public class TransferService
{
    private readonly AssetManagementDbContext _context;
    private readonly AssetLifecycleService _lifecycleService;
    private readonly ILogger<TransferService> _logger;

    public TransferService(AssetManagementDbContext context, AssetLifecycleService lifecycleService, ILogger<TransferService> logger)
    {
        _context = context;
        _lifecycleService = lifecycleService;
        _logger = logger;
    }

    public async Task<AssetTransfer?> CreateTransfer(string assetTag, string toSite, string? toStorageBin, string? carrier, string? trackingNumber, string currentUser)
    {
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.AssetTag == assetTag);
        if (asset == null)
        {
            _logger.LogWarning("Asset {AssetTag} not found for transfer creation", assetTag);
            return null;
        }

        // Check if asset can be transferred
        var allowedStates = new[] { AssetLifecycleState.InStorage, AssetLifecycleState.Delivered, AssetLifecycleState.RedeployPending, AssetLifecycleState.SalvagePending };
        if (!allowedStates.Contains(asset.LifecycleState))
        {
            _logger.LogWarning("Asset {AssetTag} cannot be transferred from state {State}", assetTag, asset.LifecycleState);
            return null;
        }

        var transfer = new AssetTransfer
        {
            AssetTag = assetTag,
            FromSite = asset.CurrentSite ?? "Unknown",
            ToSite = toSite,
            FromStorageBin = asset.CurrentStorageLocation,
            ToStorageBin = toStorageBin,
            Carrier = carrier,
            TrackingNumber = trackingNumber,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = currentUser,
            State = "Draft"
        };

        _context.AssetTransfers.Add(transfer);

        // Create audit event
        var assetEvent = new AssetEvent
        {
            AssetTag = assetTag,
            Type = "TransferCreated",
            DataJson = JsonSerializer.Serialize(new { 
                TransferId = transfer.Id,
                FromSite = transfer.FromSite,
                ToSite = transfer.ToSite,
                Carrier = carrier,
                TrackingNumber = trackingNumber
            }),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = currentUser
        };

        _context.AssetEvents.Add(assetEvent);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Transfer created for asset {AssetTag} from {FromSite} to {ToSite}", assetTag, transfer.FromSite, toSite);

        return transfer;
    }

    public async Task<bool> ShipTransfer(Guid transferId, string currentUser)
    {
        var transfer = await _context.AssetTransfers.FirstOrDefaultAsync(t => t.Id == transferId);
        if (transfer == null) return false;

        if (transfer.State != "Draft")
        {
            _logger.LogWarning("Transfer {TransferId} cannot be shipped from state {State}", transferId, transfer.State);
            return false;
        }

        transfer.State = "Shipped";
        transfer.ShippedAt = DateTimeOffset.UtcNow;

        // Move asset to ReadyForShipment state
        var success = await _lifecycleService.MarkReadyForShipment(transfer.AssetTag, currentUser);

        if (success)
        {
            // Create audit event
            var assetEvent = new AssetEvent
            {
                AssetTag = transfer.AssetTag,
                Type = "TransferShipped",
                DataJson = JsonSerializer.Serialize(new { 
                    TransferId = transfer.Id,
                    TrackingNumber = transfer.TrackingNumber,
                    ShippedAt = transfer.ShippedAt
                }),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = currentUser
            };

            _context.AssetEvents.Add(assetEvent);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Transfer {TransferId} shipped for asset {AssetTag}", transferId, transfer.AssetTag);
            return true;
        }

        return false;
    }

    public async Task<bool> ReceiveTransfer(Guid transferId, string receivedBy, string currentUser)
    {
        var transfer = await _context.AssetTransfers.FirstOrDefaultAsync(t => t.Id == transferId);
        if (transfer == null) return false;

        if (transfer.State != "Shipped")
        {
            _logger.LogWarning("Transfer {TransferId} cannot be received from state {State}", transferId, transfer.State);
            return false;
        }

        transfer.State = "Received";
        transfer.ReceivedAt = DateTimeOffset.UtcNow;
        transfer.ReceivedBy = receivedBy;

        // Move asset to Delivered state
        var deliveryContext = new AssetLifecycleService.DeliveryContext
        {
            ToSite = transfer.ToSite,
            DeliveryLocation = transfer.ToStorageBin ?? "Main Delivery Area",
            DeliveryFloor = "Ground",
            DeliveryDesk = null
        };

        var stateChanged = await _lifecycleService.TransitionToState(transfer.AssetTag, AssetLifecycleState.Delivered, currentUser, deliveryContext);

        if (stateChanged)
        {
            // Create audit event
            var assetEvent = new AssetEvent
            {
                AssetTag = transfer.AssetTag,
                Type = "TransferReceived",
                DataJson = JsonSerializer.Serialize(new { 
                    TransferId = transfer.Id,
                    ReceivedBy = receivedBy,
                    ReceivedAt = transfer.ReceivedAt
                }),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = currentUser
            };

            _context.AssetEvents.Add(assetEvent);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Transfer {TransferId} received for asset {AssetTag} by {ReceivedBy}", transferId, transfer.AssetTag, receivedBy);
            return true;
        }

        return false;
    }

    public async Task<List<AssetTransfer>> GetTransfersByAsset(string assetTag)
    {
        return await _context.AssetTransfers
            .Where(t => t.AssetTag == assetTag)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<AssetTransfer>> GetTransfersByTrackingNumber(string trackingNumber)
    {
        return await _context.AssetTransfers
            .Where(t => t.TrackingNumber == trackingNumber)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<AssetTransfer>> GetPendingTransfers(string? site = null)
    {
        var query = _context.AssetTransfers.Where(t => t.State == "Draft" || t.State == "Shipped");
        
        if (!string.IsNullOrEmpty(site))
        {
            query = query.Where(t => t.FromSite == site || t.ToSite == site);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<AssetTransfer?> GetTransferById(Guid transferId)
    {
        return await _context.AssetTransfers
            .Include(t => t.Asset)
            .FirstOrDefaultAsync(t => t.Id == transferId);
    }
}
