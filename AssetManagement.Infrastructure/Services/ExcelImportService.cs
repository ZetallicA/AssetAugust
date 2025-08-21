using AssetManagement.Domain.Entities;
using AssetManagement.Infrastructure.Data;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Infrastructure.Services;

public interface IExcelImportService
{
    Task<ImportResult> ImportAssetsAsync(string filePath, string fileName, string importedBy);
    Task<ImportPreviewResult> PreviewImportAsync(string filePath, string fileName);
}

public class ExcelImportService : IExcelImportService
{
    private readonly AssetManagementDbContext _context;
    private readonly ILogger<ExcelImportService> _logger;

    public ExcelImportService(AssetManagementDbContext context, ILogger<ExcelImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ImportResult> ImportAssetsAsync(string filePath, string fileName, string importedBy)
    {
        var result = new ImportResult();
        
        try
        {
            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1); // First worksheet
            
            _logger.LogInformation("Excel file opened successfully. Worksheet name: {WorksheetName}", worksheet.Name);
            
            var rows = worksheet.RowsUsed().Skip(1); // Skip header row
            var assets = new List<Asset>();
            var processedAssetTags = new HashSet<string>(); // Track asset tags in current import
            var errors = new List<string>();
            
            _logger.LogInformation("Starting Excel import for file: {FileName}. Total rows to process: {RowCount}", 
                fileName, rows.Count());
            
            foreach (var row in rows)
            {
                try
                {
                    var asset = new Asset
                    {
                        AssetTag = GetCellValue(row, 1),
                        SerialNumber = GetCellValue(row, 2),
                        ServiceTag = GetCellValue(row, 3),
                        Manufacturer = GetCellValue(row, 4),
                        Model = GetCellValue(row, 5),
                        Category = GetCellValue(row, 6),
                        NetName = GetCellValue(row, 7),
                        AssignedUserName = GetCellValue(row, 8),
                        AssignedUserEmail = GetCellValue(row, 9),
                        Manager = GetCellValue(row, 10),
                        Department = GetCellValue(row, 11),
                        Unit = GetCellValue(row, 12),
                        Location = GetCellValue(row, 13),
                        Floor = GetCellValue(row, 14),
                        Desk = GetCellValue(row, 15),
                        Status = GetCellValue(row, 16),
                        IpAddress = GetCellValue(row, 17),
                        MacAddress = GetCellValue(row, 18),
                        WallPort = GetCellValue(row, 19),
                        SwitchName = GetCellValue(row, 20),
                        SwitchPort = GetCellValue(row, 21),
                        PhoneNumber = GetCellValue(row, 22),
                        Extension = GetCellValue(row, 23),
                        Imei = GetCellValue(row, 24),
                        CardNumber = GetCellValue(row, 25),
                        OsVersion = GetCellValue(row, 26),
                        License1 = GetCellValue(row, 27),
                        License2 = GetCellValue(row, 28),
                        License3 = GetCellValue(row, 29),
                        License4 = GetCellValue(row, 30),
                        License5 = GetCellValue(row, 31),
                        PurchasePrice = GetDecimalValue(row, 32),
                        OrderNumber = GetCellValue(row, 33),
                        Vendor = GetCellValue(row, 34),
                        VendorInvoice = GetCellValue(row, 35),
                        PurchaseDate = GetDateTimeValue(row, 36),
                        WarrantyStart = GetDateTimeValue(row, 37),
                        WarrantyEndDate = GetDateTimeValue(row, 38),
                        Notes = GetCellValue(row, 39),
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = importedBy
                    };

                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(asset.AssetTag))
                    {
                        var error = new ImportError
                        {
                            RowNumber = row.RowNumber(),
                            AssetTag = asset.AssetTag ?? "",
                            SerialNumber = asset.SerialNumber ?? "",
                            ErrorMessage = "Asset Tag is required",
                            RowData = GetRowData(row)
                        };
                        result.ErrorDetails.Add(error);
                        errors.Add($"Row {row.RowNumber()}: Asset Tag is required");
                        _logger.LogWarning("Row {RowNumber}: Asset Tag is required", row.RowNumber());
                        result.Errors++;
                        continue;
                    }
                    
                    _logger.LogDebug("Processing asset: {AssetTag} from row {RowNumber}", asset.AssetTag, row.RowNumber());

                                            // Check for duplicate asset tag
                        var existingAsset = await _context.Assets
                            .FirstOrDefaultAsync(a => a.AssetTag == asset.AssetTag);
                        
                        if (existingAsset != null)
                        {
                            var error = new ImportError
                            {
                                RowNumber = row.RowNumber(),
                                AssetTag = asset.AssetTag,
                                SerialNumber = asset.SerialNumber ?? "",
                                ErrorMessage = $"Asset Tag '{asset.AssetTag}' already exists in database. Skipping duplicate.",
                                RowData = GetRowData(row)
                            };
                            result.ErrorDetails.Add(error);
                            _logger.LogWarning("Duplicate asset tag found in database: {AssetTag}. Skipping row {RowNumber}", asset.AssetTag, row.RowNumber());
                            errors.Add($"Row {row.RowNumber()}: Asset Tag '{asset.AssetTag}' already exists in database. Skipping duplicate.");
                            result.Errors++;
                            continue;
                        }

                        // Check for duplicate asset tag within current import
                        if (processedAssetTags.Contains(asset.AssetTag))
                        {
                            var error = new ImportError
                            {
                                RowNumber = row.RowNumber(),
                                AssetTag = asset.AssetTag,
                                SerialNumber = asset.SerialNumber ?? "",
                                ErrorMessage = $"Asset Tag '{asset.AssetTag}' is duplicate within this import file. Skipping duplicate.",
                                RowData = GetRowData(row)
                            };
                            result.ErrorDetails.Add(error);
                            _logger.LogWarning("Duplicate asset tag found in import file: {AssetTag}. Skipping row {RowNumber}", asset.AssetTag, row.RowNumber());
                            errors.Add($"Row {row.RowNumber()}: Asset Tag '{asset.AssetTag}' is duplicate within this import file. Skipping duplicate.");
                            result.Errors++;
                            continue;
                        }

                        // Validate location code if provided - but don't reject, just flag for review
                        if (!string.IsNullOrWhiteSpace(asset.Location))
                        {
                            // Define location code aliases
                            var locationAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "LIC", "LIC" },                    // Queens
                                { "BROOKLYN", "BROOKLYN" },          // Brooklyn
                                { "BRONX", "BRONX" },                // Bronx
                                { "STATEN ISLAND", "STATEN ISLAND" }, // Staten Island
                                { "CHURCHST", "CHURCHST" },          // 100 Church Street (Manhattan)
                                { "66JOHN", "66JOHN" }               // 66 John Street (Manhattan)
                            };

                            // Check if the location is an alias and map it to the actual building code
                            if (locationAliases.ContainsKey(asset.Location))
                            {
                                asset.Location = locationAliases[asset.Location];
                            }

                            // Check if the location is valid (either a natural name or a building code)
                            var validLocation = locationAliases.ContainsKey(asset.Location) || 
                                               await _context.Buildings.AnyAsync(b => b.BuildingCode == asset.Location);
                            
                            if (!validLocation)
                            {
                                var error = new ImportError
                                {
                                    RowNumber = row.RowNumber(),
                                    AssetTag = asset.AssetTag,
                                    SerialNumber = asset.SerialNumber ?? "",
                                    ErrorMessage = $"Location code '{asset.Location}' is not recognized. Valid locations: LIC, BROOKLYN, BRONX, STATEN ISLAND, 66JOHN",
                                    RowData = GetRowData(row)
                                };
                                result.ErrorDetails.Add(error);
                                _logger.LogWarning("Invalid location code found: {Location}. Row {RowNumber} - Flagging for review", asset.Location, row.RowNumber());
                                errors.Add($"Row {row.RowNumber()}: Location code '{asset.Location}' is not recognized. Valid locations: LIC, BROOKLYN, BRONX, STATEN ISLAND, 66JOHN");
                                result.Errors++;
                                continue; // Skip this asset - don't import it
                            }
                        }

                        // Add new asset
                        assets.Add(asset);
                        processedAssetTags.Add(asset.AssetTag); // Track this asset tag
                        result.Imported++;
                }
                catch (Exception ex)
                {
                    var error = new ImportError
                    {
                        RowNumber = row.RowNumber(),
                        AssetTag = GetCellValue(row, 1) ?? "",
                        SerialNumber = GetCellValue(row, 2) ?? "",
                        ErrorMessage = ex.Message,
                        RowData = GetRowData(row)
                    };
                    result.ErrorDetails.Add(error);
                    errors.Add($"Row {row.RowNumber()}: {ex.Message}");
                    result.Errors++;
                }
            }

            // Save changes - insert assets individually to handle duplicates gracefully
            _logger.LogInformation("Saving {AssetCount} assets to database individually", assets.Count);
            var savedCount = 0;
            
            foreach (var asset in assets)
            {
                try
                {
                    await _context.Assets.AddAsync(asset);
                    await _context.SaveChangesAsync();
                    savedCount++;
                    _logger.LogDebug("Successfully saved asset: {AssetTag}", asset.AssetTag);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to save asset {AssetTag}: {Error}", asset.AssetTag, ex.Message);
                    errors.Add($"Failed to save asset {asset.AssetTag}: {ex.Message}");
                    result.Errors++;
                }
            }
            
            _logger.LogInformation("Successfully saved {SavedCount} out of {TotalCount} assets to database", savedCount, assets.Count);
            result.Imported = savedCount;

            result.Success = true;
            result.ErrorMessages = errors;
            
            _logger.LogInformation("Excel import completed. Imported: {Imported}, Errors: {Errors}", 
                result.Imported, result.Errors);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessages.Add($"Import failed: {ex.Message}");
            _logger.LogError(ex, "Excel import failed for file: {FileName}", fileName);
        }

        return result;
    }

    public async Task<ImportPreviewResult> PreviewImportAsync(string filePath, string fileName)
    {
        var result = new ImportPreviewResult();
        
        try
        {
            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1); // First worksheet
            
            var rows = worksheet.RowsUsed().Skip(1); // Skip header row
            var previewAssets = new List<AssetPreview>();
            var errors = new List<string>(); // Use List instead of HashSet to avoid key conflicts
            var totalRows = rows.Count();
            
            _logger.LogInformation("Starting Excel preview for file: {FileName}. Total rows to process: {RowCount}", 
                fileName, totalRows);
            
            var rowsToPreview = rows.Take(10).ToList();
            _logger.LogInformation("Found {PreviewCount} rows to preview", rowsToPreview.Count);
            
            // Define location code aliases (moved outside loop to avoid recreation)
            var locationAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "LIC", "LIC" },                    // Queens - 31-00 47 Avenue
                { "BROOKLYN", "BROOKLYN" },          // Brooklyn - 9 Bond Street
                { "BRONX", "BRONX" },                // Bronx - 260 E. 161 Street
                { "STATEN ISLAND", "STATEN ISLAND" }, // Staten Island - 350 St. Marks Place
                { "66JOHN", "66JOHN" }               // Manhattan - 66 John Street
            };
            
            foreach (var row in rowsToPreview) // Preview first 10 rows
            {
                try
                {
                    _logger.LogInformation("Starting to process row {RowNumber}", row.RowNumber());
                    
                    var assetTag = GetCellValue(row, 1);
                    var serialNumber = GetCellValue(row, 2);
                    var manufacturer = GetCellValue(row, 4);
                    var model = GetCellValue(row, 5);
                    var category = GetCellValue(row, 6);
                    var assignedUser = GetCellValue(row, 8);
                    var department = GetCellValue(row, 11);
                    var location = GetCellValue(row, 13);
                    var status = GetCellValue(row, 16);

                    _logger.LogInformation("Row {RowNumber}: Extracted values - AssetTag: '{AssetTag}', Location: '{Location}'", 
                        row.RowNumber(), assetTag, location);

                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(assetTag))
                    {
                        errors.Add($"Row {row.RowNumber()}: Asset Tag is required");
                    }

                    // Validate location code if provided
                    if (!string.IsNullOrWhiteSpace(location))
                    {
                        _logger.LogInformation("Row {RowNumber}: Processing location '{Location}'", row.RowNumber(), location);
                        
                        var originalLocation = location;
                        
                        // Check if the location is an alias and map it to the actual building code
                        if (locationAliases.ContainsKey(location))
                        {
                            location = locationAliases[location];
                            _logger.LogInformation("Row {RowNumber}: Mapped location '{OriginalLocation}' to '{MappedLocation}'", 
                                row.RowNumber(), originalLocation, location);
                        }

                        _logger.LogInformation("Row {RowNumber}: Checking if location '{Location}' exists in database", row.RowNumber(), location);
                        
                        // Check if the location is valid (either a natural name or a building code)
                        var validLocation = locationAliases.ContainsKey(location) || 
                                           await _context.Buildings.AnyAsync(b => b.BuildingCode == location);
                        
                        if (!validLocation)
                        {
                            var errorMsg = $"Row {row.RowNumber()}: Location code '{originalLocation}' is not recognized. Valid locations: LIC, BROOKLYN, BRONX, STATEN ISLAND, 66JOHN";
                            errors.Add(errorMsg);
                            _logger.LogWarning("Row {RowNumber}: Invalid location '{Location}' -> '{MappedLocation}'", 
                                row.RowNumber(), originalLocation, location);
                        }
                        else
                        {
                            _logger.LogInformation("Row {RowNumber}: Valid location '{Location}' -> '{MappedLocation}'", 
                                row.RowNumber(), originalLocation, location);
                        }
                    }

                    _logger.LogInformation("Row {RowNumber}: About to create preview asset", row.RowNumber());

                    _logger.LogInformation("Row {RowNumber}: Checking if asset tag '{AssetTag}' is duplicate", row.RowNumber(), assetTag);
                    var isDuplicate = !string.IsNullOrWhiteSpace(assetTag) && await IsAssetTagDuplicate(assetTag);
                    _logger.LogInformation("Row {RowNumber}: Asset tag '{AssetTag}' is duplicate: {IsDuplicate}", row.RowNumber(), assetTag, isDuplicate);

                    var previewAsset = new AssetPreview
                    {
                        RowNumber = row.RowNumber(),
                        AssetTag = assetTag ?? string.Empty,
                        SerialNumber = serialNumber,
                        Manufacturer = manufacturer,
                        Model = model,
                        Category = category,
                        AssignedUserName = assignedUser,
                        Department = department,
                        Location = location,
                        Status = status,
                        IsDuplicate = isDuplicate
                    };

                    previewAssets.Add(previewAsset);
                    _logger.LogInformation("Row {RowNumber}: Added preview asset with AssetTag '{AssetTag}', Location '{Location}'", 
                        row.RowNumber(), previewAsset.AssetTag, previewAsset.Location);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing row {RowNumber} for preview. Exception: {ExceptionMessage}", 
                        row.RowNumber(), ex.Message);
                    errors.Add($"Row {row.RowNumber()}: {ex.Message}");
                }
            }

            result.Success = true;
            result.PreviewAssets = previewAssets;
            result.ErrorMessages = errors;
            result.TotalRows = totalRows;
            
            _logger.LogInformation("Excel preview completed. Previewed: {PreviewCount}, Total rows: {TotalRows}", 
                previewAssets.Count, result.TotalRows);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessages.Add($"Preview failed: {ex.Message}");
            _logger.LogError(ex, "Excel preview failed for file: {FileName}", fileName);
        }

        return result;
    }

    private async Task<bool> IsAssetTagDuplicate(string assetTag)
    {
        return await _context.Assets.AnyAsync(a => a.AssetTag == assetTag);
    }

    private static string? GetCellValue(IXLRow row, int column)
    {
        var cell = row.Cell(column);
        return cell.IsEmpty() ? null : cell.GetString().Trim();
    }

    private static decimal? GetDecimalValue(IXLRow row, int column)
    {
        var cell = row.Cell(column);
        if (cell.IsEmpty()) return null;
        
        if (decimal.TryParse(cell.GetString(), out var value))
            return value;
        
        return null;
    }

    private static DateTime? GetDateTimeValue(IXLRow row, int column)
    {
        var cell = row.Cell(column);
        if (cell.IsEmpty()) return null;
        
        if (DateTime.TryParse(cell.GetString(), out var value))
            return value;
        
        return null;
    }

    private static Dictionary<string, object> GetRowData(IXLRow row)
    {
        var headers = new[]
        {
            "Asset Tag", "Serial Number", "Service Tag", "Manufacturer", "Model", "Category", "Net Name",
            "Assigned User Name", "Assigned User Email", "Manager", "Department", "Unit", "Location", "Floor", "Desk",
            "Status", "IP Address", "MAC Address", "Wall Port", "Switch Name", "Switch Port", "Phone Number",
            "Extension", "IMEI", "Card Number", "OS Version", "License1", "License2", "License3", "License4", "License5",
            "Purchase Price", "Order Number", "Vendor", "Vendor Invoice", "Purchase Date", "Warranty Start", "Warranty End Date",
            "Notes", "Created At", "Created By"
        };

        var rowData = new Dictionary<string, object>();
        for (int i = 0; i < headers.Length && i < 41; i++)
        {
            var cell = row.Cell(i + 1);
            rowData[headers[i]] = cell.IsEmpty() ? "" : cell.GetString();
        }
        return rowData;
    }


}

public class ImportResult
{
    public bool Success { get; set; }
    public int Imported { get; set; }
    public int Errors { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
    public List<ImportError> ErrorDetails { get; set; } = new();
}

public class ImportError
{
    public int RowNumber { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, object> RowData { get; set; } = new();
}

public class ImportPreviewResult
{
    public bool Success { get; set; }
    public List<AssetPreview> PreviewAssets { get; set; } = new();
    public List<string> ErrorMessages { get; set; } = new();
    public int TotalRows { get; set; }
}

public class AssetPreview
{
    public int RowNumber { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? Category { get; set; }
    public string? AssignedUserName { get; set; }
    public string? Department { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
    public bool IsDuplicate { get; set; }
}
