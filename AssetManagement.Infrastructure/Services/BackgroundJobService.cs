using AssetManagement.Domain.Entities;
using AssetManagement.Infrastructure.Data;
using AssetManagement.Infrastructure.Services;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Infrastructure.Services;

public interface IBackgroundJobService
{
    string EnqueueExcelImport(string filePath, string fileName, string importedBy);
    void EnqueueAssetHistoryTracking(int assetId, string action, string description, string userId);
}

public class BackgroundJobService : IBackgroundJobService
{
    private readonly ILogger<BackgroundJobService> _logger;

    public BackgroundJobService(ILogger<BackgroundJobService> logger)
    {
        _logger = logger;
    }

    public string EnqueueExcelImport(string filePath, string fileName, string importedBy)
    {
        var jobId = BackgroundJob.Enqueue<ExcelImportJob>(job => 
            job.ProcessExcelImportAsync(filePath, fileName, importedBy));

        _logger.LogInformation("Excel import job queued with ID: {JobId} for file: {FileName}", jobId, fileName);
        
        return jobId;
    }

    public void EnqueueAssetHistoryTracking(int assetId, string action, string description, string userId)
    {
        BackgroundJob.Enqueue<AssetHistoryJob>(job => 
            job.TrackAssetHistoryAsync(assetId, action, description, userId));
    }
}

public class ExcelImportJob
{
    private readonly AssetManagementDbContext _context;
    private readonly IExcelImportService _excelImportService;
    private readonly ILogger<ExcelImportJob> _logger;

    public ExcelImportJob(
        AssetManagementDbContext context,
        IExcelImportService excelImportService,
        ILogger<ExcelImportJob> logger)
    {
        _context = context;
        _excelImportService = excelImportService;
        _logger = logger;
    }

    public async Task ProcessExcelImportAsync(string filePath, string fileName, string importedBy)
    {
        try
        {
            _logger.LogInformation("Starting background Excel import job. File: {FileName}, File path: {FilePath}", 
                fileName, filePath);
            
            var result = await _excelImportService.ImportAssetsAsync(filePath, fileName, importedBy);

            if (result.Success)
            {
                _logger.LogInformation("Background Excel import completed successfully. File: {FileName}, Imported: {Imported}, Errors: {Errors}", 
                    fileName, result.Imported, result.Errors);
            }
            else
            {
                _logger.LogError("Background Excel import failed. File: {FileName}, Errors: {Errors}", 
                    fileName, string.Join("; ", result.ErrorMessages));
            }
            
            // Clean up temporary file
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation("Temporary file cleaned up: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up temporary file: {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background Excel import job failed for file: {FileName}", fileName);
            throw;
        }
    }
}

public class AssetHistoryJob
{
    private readonly AssetManagementDbContext _context;
    private readonly ILogger<AssetHistoryJob> _logger;

    public AssetHistoryJob(AssetManagementDbContext context, ILogger<AssetHistoryJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task TrackAssetHistoryAsync(int assetId, string action, string description, string userId)
    {
        try
        {
            var assetHistory = new AssetHistory
            {
                AssetId = assetId,
                Action = action,
                Description = description,
                UserId = userId,
                Timestamp = DateTime.UtcNow
            };

            await _context.AssetHistory.AddAsync(assetHistory);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Asset history tracked. AssetId: {AssetId}, Action: {Action}", assetId, action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track asset history. AssetId: {AssetId}, Action: {Action}", assetId, action);
            throw;
        }
    }
}
