using AssetManagement.Domain.Entities;
using AssetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AssetManagement.Infrastructure.Services;

public class SalvageService
{
    private readonly AssetManagementDbContext _context;
    private readonly AssetLifecycleService _lifecycleService;
    private readonly ILogger<SalvageService> _logger;

    public SalvageService(AssetManagementDbContext context, AssetLifecycleService lifecycleService, ILogger<SalvageService> logger)
    {
        _context = context;
        _lifecycleService = lifecycleService;
        _logger = logger;
    }

    public async Task<SalvageBatch> CreateSalvageBatch(string pickupVendor, string currentUser)
    {
        // Generate batch code
        var batchCode = GenerateBatchCode();

        var batch = new SalvageBatch
        {
            BatchCode = batchCode,
            PickupVendor = pickupVendor,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = currentUser
        };

        _context.SalvageBatches.Add(batch);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Salvage batch {BatchCode} created by {User}", batchCode, currentUser);

        return batch;
    }

    public async Task<bool> AddAssetToBatch(string assetTag, Guid batchId, string currentUser)
    {
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.AssetTag == assetTag);
        if (asset == null)
        {
            _logger.LogWarning("Asset {AssetTag} not found for salvage batch.", assetTag);
            return false;
        }

        // Verify asset is in correct state
        if (asset.LifecycleState != AssetLifecycleState.Delivered && asset.LifecycleState != AssetLifecycleState.SalvagePending)
        {
            _logger.LogWarning("Asset {AssetTag} cannot be added to salvage batch. Site: {Site}, State: {State}", 
                assetTag, asset.CurrentSite, asset.LifecycleState);
            return false;
        }

        return await _lifecycleService.AddToSalvageBatch(assetTag, batchId, currentUser);
    }

    public async Task<bool> FinalizeSalvageBatch(Guid batchId, string manifestNumber, DateTimeOffset pickedUpAt, string currentUser)
    {
        return await _lifecycleService.FinalizeSalvageBatch(batchId, manifestNumber, pickedUpAt, currentUser);
    }

    public async Task<SalvageBatch?> GetSalvageBatch(Guid batchId)
    {
        return await _context.SalvageBatches
            .Include(b => b.Assets)
            .FirstOrDefaultAsync(b => b.Id == batchId);
    }

    public async Task<List<SalvageBatch>> GetSalvageBatches(DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null)
    {
        var query = _context.SalvageBatches.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(b => b.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(b => b.CreatedAt <= toDate.Value);
        }

        return await query
            .Include(b => b.Assets)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Asset>> GetAssetsEligibleForSalvage(string? site = null)
    {
        var query = _context.Assets
            .Where(a => (a.LifecycleState == AssetLifecycleState.Delivered || a.LifecycleState == AssetLifecycleState.SalvagePending) &&
                       a.SalvageBatchId == null);
        
        if (!string.IsNullOrEmpty(site))
        {
            query = query.Where(a => a.CurrentSite == site);
        }
        
        return await query
            .OrderBy(a => a.AssetTag)
            .ToListAsync();
    }

    public async Task<SalvageManifest> GenerateManifest(Guid batchId)
    {
        var batch = await _context.SalvageBatches
            .Include(b => b.Assets)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch == null)
        {
            throw new ArgumentException($"Salvage batch {batchId} not found");
        }

        var manifest = new SalvageManifest
        {
            BatchCode = batch.BatchCode,
            PickupVendor = batch.PickupVendor,
            CreatedAt = batch.CreatedAt,
            CreatedBy = batch.CreatedBy,
            Items = batch.Assets.Select(a => new SalvageManifestItem
            {
                AssetTag = a.AssetTag,
                SerialNumber = a.SerialNumber,
                Manufacturer = a.Manufacturer,
                Model = a.Model,
                Notes = a.Notes,
                Weight = EstimateWeight(a.Category, a.Model), // Optional weight estimation
                BinNumber = a.CurrentStorageLocation
            }).ToList()
        };

        return manifest;
    }

    public async Task<SalvageReport> GenerateSalvageReport(DateTimeOffset fromDate, DateTimeOffset toDate)
    {
        var batches = await _context.SalvageBatches
            .Include(b => b.Assets)
            .Where(b => b.CreatedAt >= fromDate && b.CreatedAt <= toDate)
            .ToListAsync();

        var report = new SalvageReport
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalBatches = batches.Count,
            TotalAssets = batches.Sum(b => b.Assets.Count),
            Batches = batches.Select(b => new SalvageBatchSummary
            {
                BatchCode = b.BatchCode,
                CreatedAt = b.CreatedAt,
                AssetCount = b.Assets.Count,
                PickupVendor = b.PickupVendor,
                PickedUpAt = b.PickedUpAt,
                ManifestNumber = b.PickupManifestNumber
            }).ToList()
        };

        return report;
    }

    private string GenerateBatchCode()
    {
        var now = DateTimeOffset.UtcNow;
        var dateString = now.ToString("yyyy-MM-dd");
        var timeString = now.ToString("HHmmss");
        return $"SAL-{dateString}-{timeString}";
    }

    private decimal? EstimateWeight(string? category, string? model)
    {
        // Simple weight estimation based on category
        return category?.ToLower() switch
        {
            "laptop" => 2.5m,
            "desktop" => 8.0m,
            "monitor" => 5.0m,
            "printer" => 15.0m,
            "server" => 25.0m,
            "network equipment" => 3.0m,
            _ => null
        };
    }
}

public class SalvageManifest
{
    public string BatchCode { get; set; } = string.Empty;
    public string PickupVendor { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public List<SalvageManifestItem> Items { get; set; } = new();
    public int TotalCount => Items.Count;
    public decimal? TotalWeight => Items.Sum(i => i.Weight);
}

public class SalvageManifestItem
{
    public string AssetTag { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? Notes { get; set; }
    public decimal? Weight { get; set; }
    public string? BinNumber { get; set; }
}

public class SalvageReport
{
    public DateTimeOffset FromDate { get; set; }
    public DateTimeOffset ToDate { get; set; }
    public int TotalBatches { get; set; }
    public int TotalAssets { get; set; }
    public List<SalvageBatchSummary> Batches { get; set; } = new();
}

public class SalvageBatchSummary
{
    public string BatchCode { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public int AssetCount { get; set; }
    public string PickupVendor { get; set; } = string.Empty;
    public DateTimeOffset? PickedUpAt { get; set; }
    public string? ManifestNumber { get; set; }
}
