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

    // Header aliases for flexible column mapping
    private static readonly Dictionary<string, string> HeaderAliases = 
        new(StringComparer.OrdinalIgnoreCase)
    {
        { "Supervisor", "Manager" },
        { "Mgr", "Manager" },
        { "Warranty Expiration", "Warranty End Date" },
        { "Warranty Exp.", "Warranty End Date" },
        { "Warranty End", "Warranty End Date" }
    };

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
            
            // Get column mapping from header row
            var headerRow = worksheet.Row(1);
            var columnMapping = GetColumnMapping(headerRow);
            
            _logger.LogInformation("Column mapping detected: {ColumnMapping}", 
                string.Join(", ", columnMapping.Select(kvp => $"{kvp.Key}:{kvp.Value}")));
            
            foreach (var row in rows)
            {
                try
                {
                    var asset = new Asset
                    {
                        AssetTag = GetCellValueByColumn(row, columnMapping, "Asset Tag"),
                        SerialNumber = GetCellValueByColumn(row, columnMapping, "Serial Number"),
                        ServiceTag = GetCellValueByColumn(row, columnMapping, "Service Tag"),
                        Manufacturer = GetCellValueByColumn(row, columnMapping, "Manufacturer"),
                        Model = GetCellValueByColumn(row, columnMapping, "Model"),
                        Category = GetCellValueByColumn(row, columnMapping, "Category"),
                        NetName = GetCellValueByColumn(row, columnMapping, "Net Name"),
                        AssignedUserName = GetCellValueByColumn(row, columnMapping, "Assigned User Name"),
                        AssignedUserEmail = GetCellValueByColumn(row, columnMapping, "Assigned User Email"),
                        Manager = GetCellValueByColumn(row, columnMapping, "Manager"),
                        Department = GetCellValueByColumn(row, columnMapping, "Department"),
                        Unit = GetCellValueByColumn(row, columnMapping, "Unit"),
                        Location = GetCellValueByColumn(row, columnMapping, "Location"),
                        Floor = GetCellValueByColumn(row, columnMapping, "Floor"),
                        Desk = GetCellValueByColumn(row, columnMapping, "Desk"),
                        Status = GetCellValueByColumn(row, columnMapping, "Status"),
                        IpAddress = GetCellValueByColumn(row, columnMapping, "IP Address"),
                        MacAddress = GetCellValueByColumn(row, columnMapping, "MAC Address"),
                        WallPort = GetCellValueByColumn(row, columnMapping, "Wall Port"),
                        SwitchName = GetCellValueByColumn(row, columnMapping, "Switch Name"),
                        SwitchPort = GetCellValueByColumn(row, columnMapping, "Switch Port"),
                        PhoneNumber = GetCellValueByColumn(row, columnMapping, "Phone Number"),
                        Extension = GetCellValueByColumn(row, columnMapping, "Extension"),
                        Imei = GetCellValueByColumn(row, columnMapping, "IMEI"),
                        CardNumber = GetCellValueByColumn(row, columnMapping, "Card Number"),
                        OsVersion = GetCellValueByColumn(row, columnMapping, "OS Version"),
                        License1 = GetCellValueByColumn(row, columnMapping, "License1"),
                        License2 = GetCellValueByColumn(row, columnMapping, "License2"),
                        License3 = GetCellValueByColumn(row, columnMapping, "License3"),
                        License4 = GetCellValueByColumn(row, columnMapping, "License4"),
                        License5 = GetCellValueByColumn(row, columnMapping, "License5"),
                        PurchasePrice = GetDecimalValueByColumn(row, columnMapping, "Purchase Price"),
                        OrderNumber = GetCellValueByColumn(row, columnMapping, "Order Number"),
                        Vendor = GetCellValueByColumn(row, columnMapping, "Vendor"),
                        VendorInvoice = GetCellValueByColumn(row, columnMapping, "Vendor Invoice"),
                        PurchaseDate = GetDateTimeValueByColumn(row, columnMapping, "Purchase Date"),
                        WarrantyStart = GetDateTimeValueByColumn(row, columnMapping, "Warranty Start"),
                        WarrantyEndDate = GetDateTimeValueByColumn(row, columnMapping, "Warranty End Date"),
                        Notes = GetCellValueByColumn(row, columnMapping, "Notes"),
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = importedBy,
                        // Set lifecycle state based on import data
                        LifecycleState = DetermineLifecycleStateFromImport(
                            GetCellValueByColumn(row, columnMapping, "Status"),
                            GetCellValueByColumn(row, columnMapping, "Category"),
                            GetCellValueByColumn(row, columnMapping, "Location"),
                            GetCellValueByColumn(row, columnMapping, "Desk")),
                        CurrentSite = GetCellValueByColumn(row, columnMapping, "Location"),
                        CurrentStorageLocation = GetCellValueByColumn(row, columnMapping, "Location"),
                        CurrentDesk = GetCellValueByColumn(row, columnMapping, "Desk")
                    };

                    // Debug logging for Extension and OS Version values
                    _logger.LogDebug("Row {RowNumber}: Extension='{Extension}', OS Version='{OsVersion}'", 
                        row.RowNumber(), asset.Extension, asset.OsVersion);

                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(asset.AssetTag))
                    {
                        var error = new ImportError
                        {
                            RowNumber = row.RowNumber(),
                            AssetTag = asset.AssetTag ?? "",
                            SerialNumber = asset.SerialNumber ?? "",
                            ErrorMessage = "Asset Tag is required",
                            RowData = GetRowData(row, columnMapping)
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
                            RowData = GetRowData(row, columnMapping)
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
                            RowData = GetRowData(row, columnMapping)
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
                                RowData = GetRowData(row, columnMapping)
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
                        AssetTag = GetCellValueByColumn(row, columnMapping, "Asset Tag") ?? "",
                        SerialNumber = GetCellValueByColumn(row, columnMapping, "Serial Number") ?? "",
                        ErrorMessage = ex.Message,
                        RowData = GetRowData(row, columnMapping)
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
            
            // Get column mapping from header row
            var headerRow = worksheet.Row(1);
            var columnMapping = GetColumnMapping(headerRow);
            
            _logger.LogInformation("Column mapping detected for preview: {ColumnMapping}", 
                string.Join(", ", columnMapping.Select(kvp => $"{kvp.Key}:{kvp.Value}")));
            
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
                    
                    var assetTag = GetCellValueByColumn(row, columnMapping, "Asset Tag");
                    var serialNumber = GetCellValueByColumn(row, columnMapping, "Serial Number");
                    var manufacturer = GetCellValueByColumn(row, columnMapping, "Manufacturer");
                    var model = GetCellValueByColumn(row, columnMapping, "Model");
                    var category = GetCellValueByColumn(row, columnMapping, "Category");
                    var assignedUser = GetCellValueByColumn(row, columnMapping, "Assigned User Name");
                    var department = GetCellValueByColumn(row, columnMapping, "Department");
                    var location = GetCellValueByColumn(row, columnMapping, "Location");
                    var status = GetCellValueByColumn(row, columnMapping, "Status");

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

        try
        {
            // Try to get as DateTime first (handles Excel date formats)
            return cell.GetDateTime();
        }
        catch
        {
            // Try to parse as text
            var textValue = cell.GetString()?.Trim();
            if (string.IsNullOrEmpty(textValue)) return null;

            // Try parsing as text date
            if (DateTime.TryParse(textValue, out var parsedDate))
            {
                return parsedDate;
            }

            // Try parsing as Excel serial number (OADate)
            if (double.TryParse(textValue, out var oaDate))
            {
                try
                {
                    return DateTime.FromOADate(oaDate);
                }
                catch
                {
                    // Invalid OADate
                    return null;
                }
            }

            return null;
        }
    }

    private static Dictionary<string, object> GetRowData(IXLRow row, Dictionary<string, int> columnMapping)
    {
        var rowData = new Dictionary<string, object>();
        foreach (var kvp in columnMapping)
        {
            var header = kvp.Key;
            var columnIndex = kvp.Value; // Already 1-based from GetColumnMapping
            var cell = row.Cell(columnIndex);
            rowData[header] = cell.IsEmpty() ? "" : cell.GetString();
        }
        return rowData;
    }

    private Dictionary<string, int> GetColumnMapping(IXLRow headerRow)
    {
        var columnMapping = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var headers = headerRow.CellsUsed().Select(c => c.GetString().Trim()).ToList();
        var usedColumns = new HashSet<int>();

        _logger.LogInformation("Found headers in Excel file: {Headers}", string.Join(", ", headers));

        // Define expected headers with their typical positions (0-based)
        var expectedHeaders = new[]
        {
            "Asset Tag", "Serial Number", "Service Tag", "Manufacturer", "Model", "Category", "Net Name",
            "Assigned User Name", "Assigned User Email", "Manager", "Department", "Unit", "Location", "Floor", "Desk",
            "Status", "IP Address", "MAC Address", "Wall Port", "Switch Name", "Switch Port", "Phone Number",
            "Extension", "IMEI", "Card Number", "OS Version", "License1", "License2", "License3", "License4", "License5",
            "Purchase Price", "Order Number", "Vendor", "Vendor Invoice", "Purchase Date", "Warranty Start", "Warranty End Date",
            "Notes", "Created At", "Created By"
        };

        // First pass: Try exact matches and aliases
        for (int i = 0; i < expectedHeaders.Length; i++)
        {
            var header = expectedHeaders[i];
            var foundIndex = headers.IndexOf(header);
            if (foundIndex != -1 && !usedColumns.Contains(foundIndex))
            {
                columnMapping[header] = foundIndex + 1;
                usedColumns.Add(foundIndex);
                _logger.LogDebug("Found exact header '{Header}' at column {ColumnIndex}", header, foundIndex + 1);
            }
            else
            {
                // Try aliases for this header
                var aliases = HeaderAliases.Where(kvp => kvp.Value.Equals(header, StringComparison.OrdinalIgnoreCase))
                                         .Select(kvp => kvp.Key);
                foreach (var alias in aliases)
                {
                    var aliasIndex = headers.IndexOf(alias);
                    if (aliasIndex != -1 && !usedColumns.Contains(aliasIndex))
                    {
                        columnMapping[header] = aliasIndex + 1;
                        usedColumns.Add(aliasIndex);
                        _logger.LogDebug("Found alias '{Alias}' for header '{Header}' at column {ColumnIndex}", alias, header, aliasIndex + 1);
                        break;
                    }
                }
            }
        }

        // Second pass: Try partial matches for unmatched headers
        for (int i = 0; i < expectedHeaders.Length; i++)
        {
            var header = expectedHeaders[i];
            if (columnMapping.ContainsKey(header)) continue; // Already mapped

            // Try to find a partial match
            var bestMatch = -1;
            var bestScore = 0.0;
            
            for (int j = 0; j < headers.Count; j++)
            {
                if (usedColumns.Contains(j)) continue; // Already used
                
                var excelHeader = headers[j];
                var score = CalculateHeaderSimilarity(header, excelHeader);
                
                if (score > bestScore && score > 0.7) // Only accept good matches
                {
                    bestScore = score;
                    bestMatch = j;
                }
            }

            if (bestMatch != -1)
            {
                columnMapping[header] = bestMatch + 1;
                usedColumns.Add(bestMatch);
                _logger.LogDebug("Found partial match for '{Header}' at column {ColumnIndex} (score: {Score})", 
                    header, bestMatch + 1, bestScore);
            }
            else
            {
                // Find first unused column
                var unusedColumn = 0;
                while (usedColumns.Contains(unusedColumn) && unusedColumn < headers.Count)
                {
                    unusedColumn++;
                }
                
                if (unusedColumn < headers.Count)
                {
                    columnMapping[header] = unusedColumn + 1;
                    usedColumns.Add(unusedColumn);
                    _logger.LogWarning("No match found for '{Header}'. Using column {ColumnIndex} with header '{ExcelHeader}'.", 
                        header, unusedColumn + 1, headers[unusedColumn]);
                }
                else
                {
                    _logger.LogWarning("No unused columns available for '{Header}'. Using default position {DefaultPosition}.", 
                        header, i + 1);
                    columnMapping[header] = i + 1;
                }
            }
        }

        // Log the final mapping
        _logger.LogInformation("Final column mapping: {Mapping}", 
            string.Join(", ", columnMapping.Select(kvp => $"{kvp.Key}:{kvp.Value}")));

        // Add specific logging for Extension and OS Version mapping
        if (columnMapping.ContainsKey("Extension"))
        {
            _logger.LogInformation("Extension mapped to column {ColumnIndex}", columnMapping["Extension"]);
        }
        if (columnMapping.ContainsKey("OS Version"))
        {
            _logger.LogInformation("OS Version mapped to column {ColumnIndex}", columnMapping["OS Version"]);
        }

        return columnMapping;
    }

    private static double CalculateHeaderSimilarity(string expected, string actual)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual))
            return 0;

        expected = expected.ToLower().Trim();
        actual = actual.ToLower().Trim();

        // Exact match
        if (expected == actual) return 1.0;

        // Contains match
        if (actual.Contains(expected) || expected.Contains(actual)) return 0.9;

        // Word-based matching
        var expectedWords = expected.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        var actualWords = actual.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

        var commonWords = expectedWords.Intersect(actualWords, StringComparer.OrdinalIgnoreCase).Count();
        var totalWords = Math.Max(expectedWords.Length, actualWords.Length);

        if (totalWords > 0)
        {
            return (double)commonWords / totalWords;
        }

        return 0;
    }

    private string? GetCellValueByColumn(IXLRow row, Dictionary<string, int> columnMapping, string header)
    {
        if (columnMapping.TryGetValue(header, out var columnIndex))
        {
            return GetCellValue(row, columnIndex);
        }
        _logger.LogWarning("Column header '{Header}' not found in column mapping. Returning null.", header);
        return null;
    }

    private decimal? GetDecimalValueByColumn(IXLRow row, Dictionary<string, int> columnMapping, string header)
    {
        if (columnMapping.TryGetValue(header, out var columnIndex))
        {
            return GetDecimalValue(row, columnIndex);
        }
        _logger.LogWarning("Column header '{Header}' not found in column mapping. Returning null.", header);
        return null;
    }

    private DateTime? GetDateTimeValueByColumn(IXLRow row, Dictionary<string, int> columnMapping, string header)
    {
        if (columnMapping.TryGetValue(header, out var columnIndex))
        {
            return GetDateTimeValue(row, columnIndex);
        }
        _logger.LogWarning("Column header '{Header}' not found in column mapping. Returning null.", header);
        return null;
    }

    private AssetLifecycleState DetermineLifecycleStateFromImport(string? status, string? category, string? location, string? desk)
    {
        // If status/category indicates salvage, set to Salvaged
        if (!string.IsNullOrEmpty(status) && status.Equals("Salvage", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(category) && category.Equals("Salvage", StringComparison.OrdinalIgnoreCase))
        {
            return AssetLifecycleState.Salvaged;
        }

        // If desk is present, asset is deployed
        if (!string.IsNullOrEmpty(desk))
        {
            return AssetLifecycleState.Deployed;
        }

        // If location indicates storage and no desk, asset is in storage
        if (!string.IsNullOrEmpty(location) && 
            (location.Contains("Storage", StringComparison.OrdinalIgnoreCase) || 
             location.Contains("LIC", StringComparison.OrdinalIgnoreCase) ||
             location.Contains("66JOHN", StringComparison.OrdinalIgnoreCase)))
        {
            return AssetLifecycleState.InStorage;
        }

        // Default to InStorage
        return AssetLifecycleState.InStorage;
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
