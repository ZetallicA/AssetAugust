using AssetManagement.Domain.Entities;
using AssetManagement.Infrastructure.Data;
using AssetManagement.Infrastructure.Services;
using AssetManagement.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ClosedXML.Excel;
using System.IO;

namespace AssetManagement.Web.Controllers;

[Authorize]
public class AssetsController : Controller
{
    private readonly AssetManagementDbContext _context;
    private readonly IExcelImportService _excelImportService;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(
        AssetManagementDbContext context,
        IExcelImportService excelImportService,
        ILogger<AssetsController> logger)
    {
        _context = context;
        _excelImportService = excelImportService;
        _logger = logger;
    }

    // GET: Assets
    public async Task<IActionResult> Index(string searchTerm, string sortOrder, int page = 1, int pageSize = 25)
    {
        ViewData["CurrentSort"] = sortOrder;
        ViewData["AssetTagSortParm"] = String.IsNullOrEmpty(sortOrder) ? "assetTag_desc" : "";
        ViewData["SerialNumberSortParm"] = sortOrder == "serialNumber" ? "serialNumber_desc" : "serialNumber";
        ViewData["ServiceTagSortParm"] = sortOrder == "serviceTag" ? "serviceTag_desc" : "serviceTag";
        ViewData["ManufacturerSortParm"] = sortOrder == "manufacturer" ? "manufacturer_desc" : "manufacturer";
        ViewData["ModelSortParm"] = sortOrder == "model" ? "model_desc" : "model";
        ViewData["CategorySortParm"] = sortOrder == "category" ? "category_desc" : "category";
        ViewData["NetNameSortParm"] = sortOrder == "netName" ? "netName_desc" : "netName";
        ViewData["AssignedUserSortParm"] = sortOrder == "assignedUser" ? "assignedUser_desc" : "assignedUser";
        ViewData["AssignedUserEmailSortParm"] = sortOrder == "assignedUserEmail" ? "assignedUserEmail_desc" : "assignedUserEmail";
        ViewData["ManagerSortParm"] = sortOrder == "manager" ? "manager_desc" : "manager";
        ViewData["DepartmentSortParm"] = sortOrder == "department" ? "department_desc" : "department";
        ViewData["UnitSortParm"] = sortOrder == "unit" ? "unit_desc" : "unit";
        ViewData["LocationSortParm"] = sortOrder == "location" ? "location_desc" : "location";
        ViewData["FloorSortParm"] = sortOrder == "floor" ? "floor_desc" : "floor";
        ViewData["DeskSortParm"] = sortOrder == "desk" ? "desk_desc" : "desk";
        ViewData["StatusSortParm"] = sortOrder == "status" ? "status_desc" : "status";
        ViewData["IpAddressSortParm"] = sortOrder == "ipAddress" ? "ipAddress_desc" : "ipAddress";
        ViewData["MacAddressSortParm"] = sortOrder == "macAddress" ? "macAddress_desc" : "macAddress";
        ViewData["WallPortSortParm"] = sortOrder == "wallPort" ? "wallPort_desc" : "wallPort";
        ViewData["SwitchNameSortParm"] = sortOrder == "switchName" ? "switchName_desc" : "switchName";
        ViewData["SwitchPortSortParm"] = sortOrder == "switchPort" ? "switchPort_desc" : "switchPort";
        ViewData["PhoneNumberSortParm"] = sortOrder == "phoneNumber" ? "phoneNumber_desc" : "phoneNumber";
        ViewData["ExtensionSortParm"] = sortOrder == "extension" ? "extension_desc" : "extension";
        ViewData["ImeiSortParm"] = sortOrder == "imei" ? "imei_desc" : "imei";
        ViewData["CardNumberSortParm"] = sortOrder == "cardNumber" ? "cardNumber_desc" : "cardNumber";
        ViewData["OsVersionSortParm"] = sortOrder == "osVersion" ? "osVersion_desc" : "osVersion";
        ViewData["License1SortParm"] = sortOrder == "license1" ? "license1_desc" : "license1";
        ViewData["License2SortParm"] = sortOrder == "license2" ? "license2_desc" : "license2";
        ViewData["License3SortParm"] = sortOrder == "license3" ? "license3_desc" : "license3";
        ViewData["License4SortParm"] = sortOrder == "license4" ? "license4_desc" : "license4";
        ViewData["License5SortParm"] = sortOrder == "license5" ? "license5_desc" : "license5";
        ViewData["PurchaseOrderNumberSortParm"] = sortOrder == "purchaseOrderNumber" ? "purchaseOrderNumber_desc" : "purchaseOrderNumber";
        ViewData["VendorSortParm"] = sortOrder == "vendor" ? "vendor_desc" : "vendor";
        ViewData["VendorInvoiceSortParm"] = sortOrder == "vendorInvoice" ? "vendorInvoice_desc" : "vendorInvoice";
        ViewData["PurchaseDateSortParm"] = sortOrder == "purchaseDate" ? "purchaseDate_desc" : "purchaseDate";
        ViewData["WarrantyStartSortParm"] = sortOrder == "warrantyStart" ? "warrantyStart_desc" : "warrantyStart";
        ViewData["WarrantyEndSortParm"] = sortOrder == "warrantyEnd" ? "warrantyEnd_desc" : "warrantyEnd";
        ViewData["NotesSortParm"] = sortOrder == "notes" ? "notes_desc" : "notes";
        ViewData["CreatedAtSortParm"] = sortOrder == "createdAt" ? "createdAt_desc" : "createdAt";
        ViewData["CreatedBySortParm"] = sortOrder == "createdBy" ? "createdBy_desc" : "createdBy";
        ViewData["CurrentFilter"] = searchTerm;

        var assetsQuery = _context.Assets.AsQueryable();

        // Apply search filter
        if (!String.IsNullOrEmpty(searchTerm))
        {
            assetsQuery = assetsQuery.Where(a => 
                a.AssetTag.Contains(searchTerm) ||
                a.SerialNumber.Contains(searchTerm) ||
                a.Manufacturer.Contains(searchTerm) ||
                a.Model.Contains(searchTerm) ||
                a.AssignedUserName.Contains(searchTerm) ||
                a.Department.Contains(searchTerm) ||
                a.Location.Contains(searchTerm)
            );
        }

        // Apply sorting
        assetsQuery = sortOrder switch
        {
            "assetTag_desc" => assetsQuery.OrderByDescending(a => a.AssetTag),
            "serialNumber" => assetsQuery.OrderBy(a => a.SerialNumber),
            "serialNumber_desc" => assetsQuery.OrderByDescending(a => a.SerialNumber),
            "serviceTag" => assetsQuery.OrderBy(a => a.ServiceTag),
            "serviceTag_desc" => assetsQuery.OrderByDescending(a => a.ServiceTag),
            "manufacturer" => assetsQuery.OrderBy(a => a.Manufacturer),
            "manufacturer_desc" => assetsQuery.OrderByDescending(a => a.Manufacturer),
            "model" => assetsQuery.OrderBy(a => a.Model),
            "model_desc" => assetsQuery.OrderByDescending(a => a.Model),
            "category" => assetsQuery.OrderBy(a => a.Category),
            "category_desc" => assetsQuery.OrderByDescending(a => a.Category),
            "netName" => assetsQuery.OrderBy(a => a.NetName),
            "netName_desc" => assetsQuery.OrderByDescending(a => a.NetName),
            "assignedUser" => assetsQuery.OrderBy(a => a.AssignedUserName),
            "assignedUser_desc" => assetsQuery.OrderByDescending(a => a.AssignedUserName),
            "assignedUserEmail" => assetsQuery.OrderBy(a => a.AssignedUserEmail),
            "assignedUserEmail_desc" => assetsQuery.OrderByDescending(a => a.AssignedUserEmail),
            "manager" => assetsQuery.OrderBy(a => a.Manager),
            "manager_desc" => assetsQuery.OrderByDescending(a => a.Manager),
            "department" => assetsQuery.OrderBy(a => a.Department),
            "department_desc" => assetsQuery.OrderByDescending(a => a.Department),
            "unit" => assetsQuery.OrderBy(a => a.Unit),
            "unit_desc" => assetsQuery.OrderByDescending(a => a.Unit),
            "location" => assetsQuery.OrderBy(a => a.Location),
            "location_desc" => assetsQuery.OrderByDescending(a => a.Location),
            "floor" => assetsQuery.OrderBy(a => a.Floor),
            "floor_desc" => assetsQuery.OrderByDescending(a => a.Floor),
            "desk" => assetsQuery.OrderBy(a => a.Desk),
            "desk_desc" => assetsQuery.OrderByDescending(a => a.Desk),
            "status" => assetsQuery.OrderBy(a => a.Status),
            "status_desc" => assetsQuery.OrderByDescending(a => a.Status),
            "ipAddress" => assetsQuery.OrderBy(a => a.IpAddress),
            "ipAddress_desc" => assetsQuery.OrderByDescending(a => a.IpAddress),
            "macAddress" => assetsQuery.OrderBy(a => a.MacAddress),
            "macAddress_desc" => assetsQuery.OrderByDescending(a => a.MacAddress),
            "wallPort" => assetsQuery.OrderBy(a => a.WallPort),
            "wallPort_desc" => assetsQuery.OrderByDescending(a => a.WallPort),
            "switchName" => assetsQuery.OrderBy(a => a.SwitchName),
            "switchName_desc" => assetsQuery.OrderByDescending(a => a.SwitchName),
            "switchPort" => assetsQuery.OrderBy(a => a.SwitchPort),
            "switchPort_desc" => assetsQuery.OrderByDescending(a => a.SwitchPort),
            "phoneNumber" => assetsQuery.OrderBy(a => a.PhoneNumber),
            "phoneNumber_desc" => assetsQuery.OrderByDescending(a => a.PhoneNumber),
            "extension" => assetsQuery.OrderBy(a => a.Extension),
            "extension_desc" => assetsQuery.OrderByDescending(a => a.Extension),
            "imei" => assetsQuery.OrderBy(a => a.Imei),
            "imei_desc" => assetsQuery.OrderByDescending(a => a.Imei),
            "cardNumber" => assetsQuery.OrderBy(a => a.CardNumber),
            "cardNumber_desc" => assetsQuery.OrderByDescending(a => a.CardNumber),
            "osVersion" => assetsQuery.OrderBy(a => a.OsVersion),
            "osVersion_desc" => assetsQuery.OrderByDescending(a => a.OsVersion),
            "license1" => assetsQuery.OrderBy(a => a.License1),
            "license1_desc" => assetsQuery.OrderByDescending(a => a.License1),
            "license2" => assetsQuery.OrderBy(a => a.License2),
            "license2_desc" => assetsQuery.OrderByDescending(a => a.License2),
            "license3" => assetsQuery.OrderBy(a => a.License3),
            "license3_desc" => assetsQuery.OrderByDescending(a => a.License3),
            "license4" => assetsQuery.OrderBy(a => a.License4),
            "license4_desc" => assetsQuery.OrderByDescending(a => a.License4),
            "license5" => assetsQuery.OrderBy(a => a.License5),
            "license5_desc" => assetsQuery.OrderByDescending(a => a.License5),
            "purchaseOrderNumber" => assetsQuery.OrderBy(a => a.OrderNumber),
            "purchaseOrderNumber_desc" => assetsQuery.OrderByDescending(a => a.OrderNumber),
            "vendor" => assetsQuery.OrderBy(a => a.Vendor),
            "vendor_desc" => assetsQuery.OrderByDescending(a => a.Vendor),
            "vendorInvoice" => assetsQuery.OrderBy(a => a.VendorInvoice),
            "vendorInvoice_desc" => assetsQuery.OrderByDescending(a => a.VendorInvoice),
            "purchaseDate" => assetsQuery.OrderBy(a => a.PurchaseDate),
            "purchaseDate_desc" => assetsQuery.OrderByDescending(a => a.PurchaseDate),
            "warrantyStart" => assetsQuery.OrderBy(a => a.WarrantyStart),
            "warrantyStart_desc" => assetsQuery.OrderByDescending(a => a.WarrantyStart),
            "warrantyEnd" => assetsQuery.OrderBy(a => a.WarrantyEndDate),
            "warrantyEnd_desc" => assetsQuery.OrderByDescending(a => a.WarrantyEndDate),
            "notes" => assetsQuery.OrderBy(a => a.Notes),
            "notes_desc" => assetsQuery.OrderByDescending(a => a.Notes),
            "createdAt" => assetsQuery.OrderBy(a => a.CreatedAt),
            "createdAt_desc" => assetsQuery.OrderByDescending(a => a.CreatedAt),
            "createdBy" => assetsQuery.OrderBy(a => a.CreatedBy),
            "createdBy_desc" => assetsQuery.OrderByDescending(a => a.CreatedBy),
            _ => assetsQuery.OrderBy(a => a.AssetTag)
        };

        var paginatedAssets = await PaginatedList<Asset>.CreateAsync(assetsQuery, page, pageSize);
        
        return View(paginatedAssets);
    }

    // GET: Assets/Create
    [Authorize(Roles = "Admin,IT,Procurement")]
    public async Task<IActionResult> Create()
    {
        return View();
    }

    // POST: Assets/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,IT,Procurement")]
    public async Task<IActionResult> Create([Bind("AssetTag,SerialNumber,ServiceTag,Manufacturer,Model,Category,NetName,AssignedUserName,AssignedUserEmail,Manager,Department,Unit,Location,Floor,Desk,Status,IpAddress,MacAddress,WallPort,SwitchName,SwitchPort,PhoneNumber,Extension,Imei,CardNumber,OsVersion,License1,License2,License3,License4,License5,PurchasePrice,OrderNumber,Vendor,VendorInvoice,PurchaseDate,WarrantyStart,WarrantyEndDate,Notes,BuildingId,FloorId,AssignedUserId")] Asset asset)
    {
        // Role-based validation
        var isProcurement = User.IsInRole("Procurement");
        var isIT = User.IsInRole("IT");
        var isFacilities = User.IsInRole("Facilities");
        var isAdmin = User.IsInRole("Admin");

        // Clear model state for role-based validation
        ModelState.Clear();

        // Validate required fields based on role
        if (string.IsNullOrWhiteSpace(asset.SerialNumber))
        {
            ModelState.AddModelError("SerialNumber", "Serial Number is required for all users.");
        }

        if (string.IsNullOrWhiteSpace(asset.Manufacturer))
        {
            ModelState.AddModelError("Manufacturer", "Manufacturer is required for all users.");
        }

        if (string.IsNullOrWhiteSpace(asset.Category))
        {
            ModelState.AddModelError("Category", "Category is required for all users.");
        }

        // Asset Tag validation - required for IT, Facilities, and Admin, optional for Procurement
        if (!isProcurement && string.IsNullOrWhiteSpace(asset.AssetTag))
        {
            ModelState.AddModelError("AssetTag", "Asset Tag is required for IT, Facilities, and Admin users.");
        }

        // For Procurement users, if Asset Tag is empty, generate a temporary one
        if (isProcurement && string.IsNullOrWhiteSpace(asset.AssetTag))
        {
            asset.AssetTag = $"TEMP_{DateTime.Now:yyyyMMdd}_{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        if (ModelState.IsValid)
        {
            asset.CreatedAt = DateTime.UtcNow;
            asset.CreatedBy = User.Identity?.Name ?? "System";

            // Set default status if not provided
            if (string.IsNullOrWhiteSpace(asset.Status))
            {
                asset.Status = "Active";
            }

            _context.Add(asset);
            await _context.SaveChangesAsync();

            // Track asset history (synchronous)
            var assetHistory = new AssetHistory
            {
                AssetId = asset.Id,
                Action = "Created",
                Description = $"Asset {asset.AssetTag} was created by {User.Identity?.Name}",
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System",
                Timestamp = DateTime.UtcNow
            };
            await _context.AssetHistory.AddAsync(assetHistory);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Asset {AssetTag} created by {User} (Role: {Role})", 
                asset.AssetTag, User.Identity?.Name, 
                isProcurement ? "Procurement" : isIT ? "IT" : isFacilities ? "Facilities" : "Admin");

            var successMessage = isProcurement && asset.AssetTag.StartsWith("TEMP_") 
                ? $"Asset created successfully with temporary Asset Tag: {asset.AssetTag}. IT/Facilities staff can update this later."
                : $"Asset {asset.AssetTag} created successfully.";

            TempData["SuccessMessage"] = successMessage;
            return RedirectToAction(nameof(Index));
        }

        return View(asset);
    }

    // GET: Assets/Import
    [Authorize(Roles = "Admin,IT,Procurement")]
    public IActionResult Import(bool confirmImport = false, string fileName = null)
    {
        if (confirmImport && !string.IsNullOrEmpty(fileName))
        {
            TempData["ErrorMessage"] = "Please re-upload the file to confirm the import. The preview was successful, but we need the file again for the actual import.";
        }
        return View();
    }

    // POST: Assets/Import
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,IT,Procurement")]
    public async Task<IActionResult> Import(IFormFile file, bool previewData = false, bool confirmImport = false)
    {
        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a file to import.";
            return View();
        }

        if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
        {
            TempData["ErrorMessage"] = "Please select an Excel file (.xlsx or .xls).";
            return View();
        }

        try
        {
            using var stream = file.OpenReadStream();
            
            // Clear any previous import data before starting new import
            HttpContext.Session.Remove("LastImportErrors");
            HttpContext.Session.Remove("TempImportFile");
            HttpContext.Session.Remove("TempImportFileName");
            
            if (previewData && !confirmImport)
            {
                // Store file in session for later use
                var tempFileName = $"temp_{Guid.NewGuid()}_{file.FileName}";
                var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
                
                using (var fileStream = new FileStream(tempPath, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }
                
                // Store temp file info in session
                HttpContext.Session.SetString("TempImportFile", tempPath);
                HttpContext.Session.SetString("TempImportFileName", file.FileName);
                
                // Preview the data
                var previewResult = await _excelImportService.PreviewImportAsync(tempPath, file.FileName);
                if (previewResult.Success)
                {
                    ViewBag.PreviewResult = previewResult;
                    ViewBag.FileName = file.FileName;
                    return View("ImportPreview");
                }
                else
                {
                    TempData["ErrorMessage"] = $"Preview failed: {string.Join("; ", previewResult.ErrorMessages)}";
                    return View();
                }
            }
            else
            {
                // Import the data (either direct import or confirmed import)
                // For direct import, we need to save the file temporarily
                var tempFileName = $"temp_{Guid.NewGuid()}_{file.FileName}";
                var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
                
                using (var fileStream = new FileStream(tempPath, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }
                
                // Process import synchronously
                var result = await _excelImportService.ImportAssetsAsync(tempPath, file.FileName, User.Identity?.Name ?? "System");
                
                // Clean up temp file
                try
                {
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up temporary file: {FilePath}", tempPath);
                }
                
                if (result.Success)
                {
                    // Store error details in session for error report download
                    if (result.ErrorDetails.Any())
                    {
                        var errorJson = System.Text.Json.JsonSerializer.Serialize(result.ErrorDetails);
                        HttpContext.Session.SetString("LastImportErrors", errorJson);
                    }
                    
                    TempData["SuccessMessage"] = $"Import completed successfully! Imported: {result.Imported} records, Errors: {result.Errors}";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Import failed: {string.Join("; ", result.ErrorMessages)}";
                }
                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Excel file {FileName}", file.FileName);
            TempData["ErrorMessage"] = $"Error processing file: {ex.Message}";
            return View();
        }
    }

    // POST: Assets/ImportConfirm
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,IT,Procurement")]
    public async Task<IActionResult> ImportConfirm()
    {
        var tempFilePath = HttpContext.Session.GetString("TempImportFile");
        var fileName = HttpContext.Session.GetString("TempImportFileName");
        
        if (string.IsNullOrEmpty(tempFilePath) || string.IsNullOrEmpty(fileName))
        {
            TempData["ErrorMessage"] = "File information is missing. Please try the import again.";
            return RedirectToAction(nameof(Import));
        }

        if (!System.IO.File.Exists(tempFilePath))
        {
            TempData["ErrorMessage"] = "Temporary file not found. Please try the import again.";
            return RedirectToAction(nameof(Import));
        }

        try
        {
            // Process import synchronously
            var result = await _excelImportService.ImportAssetsAsync(tempFilePath, fileName, User.Identity?.Name ?? "System");
            
            // Clean up session and temp file
            HttpContext.Session.Remove("TempImportFile");
            HttpContext.Session.Remove("TempImportFileName");
            
            // Clean up temp file
            try
            {
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up temporary file: {FilePath}", tempFilePath);
            }
            
            if (result.Success)
            {
                // Store error details in session for error report download
                if (result.ErrorDetails.Any())
                {
                    var errorJson = System.Text.Json.JsonSerializer.Serialize(result.ErrorDetails);
                    HttpContext.Session.SetString("LastImportErrors", errorJson);
                }
                
                TempData["SuccessMessage"] = $"Import completed successfully! Imported: {result.Imported} records, Errors: {result.Errors}";
            }
            else
            {
                TempData["ErrorMessage"] = $"Import failed: {string.Join("; ", result.ErrorMessages)}";
            }
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing Excel file {FileName}", fileName);
            TempData["ErrorMessage"] = $"Error importing file: {ex.Message}";
            return RedirectToAction(nameof(Import));
        }
    }

    // GET: Assets/DownloadTemplate
    [Authorize(Roles = "Admin,IT,Procurement")]
    public IActionResult DownloadTemplate()
    {
        try
        {
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Asset Template");

            // Define the column headers based on the 41-column structure
            var headers = new[]
            {
                "Asset Tag", "Serial Number", "Service Tag", "Manufacturer", "Model", "Category", "Net Name",
                "Assigned User Name", "Assigned User Email", "Manager", "Department", "Unit", "Location", "Floor", "Desk",
                "Status", "IP Address", "MAC Address", "Wall Port", "Switch Name", "Switch Port", "Phone Number",
                "Extension", "IMEI", "Card Number", "OS Version", "License1", "License2", "License3", "License4", "License5",
                "Purchase Price", "Order Number", "Vendor", "Vendor Invoice", "Purchase Date", "Warranty Start", "Warranty End Date",
                "Notes", "Created At", "Created By"
            };

            // Add headers
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Add sample data row
            worksheet.Cell(2, 1).Value = "ASSET001";
            worksheet.Cell(2, 2).Value = "SN123456789";
            worksheet.Cell(2, 3).Value = "ST987654321";
            worksheet.Cell(2, 4).Value = "Dell";
            worksheet.Cell(2, 5).Value = "OptiPlex 7090";
            worksheet.Cell(2, 6).Value = "Desktop";
            worksheet.Cell(2, 7).Value = "DESKTOP-ABC123";
            worksheet.Cell(2, 8).Value = "John Doe";
            worksheet.Cell(2, 9).Value = "john.doe@company.com";
            worksheet.Cell(2, 10).Value = "Jane Manager";
            worksheet.Cell(2, 11).Value = "IT";
            worksheet.Cell(2, 12).Value = "Infrastructure";
                         worksheet.Cell(2, 13).Value = "66JOHN";
            worksheet.Cell(2, 14).Value = "2nd Floor";
            worksheet.Cell(2, 15).Value = "A-15";
            worksheet.Cell(2, 16).Value = "Active";
            worksheet.Cell(2, 17).Value = "192.168.1.100";
            worksheet.Cell(2, 18).Value = "00:11:22:33:44:55";
            worksheet.Cell(2, 19).Value = "WP-2A-15";
            worksheet.Cell(2, 20).Value = "SW-CORE-01";
            worksheet.Cell(2, 21).Value = "Gi1/0/15";
            worksheet.Cell(2, 22).Value = "";
            worksheet.Cell(2, 23).Value = "";
            worksheet.Cell(2, 24).Value = "";
            worksheet.Cell(2, 25).Value = "";
            worksheet.Cell(2, 26).Value = "Windows 11 Pro";
            worksheet.Cell(2, 27).Value = "LIC-001";
            worksheet.Cell(2, 28).Value = "";
            worksheet.Cell(2, 29).Value = "";
            worksheet.Cell(2, 30).Value = "";
            worksheet.Cell(2, 31).Value = "";
            worksheet.Cell(2, 32).Value = 1299.99;
            worksheet.Cell(2, 33).Value = "PO-2024-001";
            worksheet.Cell(2, 34).Value = "Dell Technologies";
            worksheet.Cell(2, 35).Value = "INV-2024-001";
            worksheet.Cell(2, 36).Value = DateTime.Now.AddMonths(-6).ToString("yyyy-MM-dd");
            worksheet.Cell(2, 37).Value = DateTime.Now.AddMonths(-6).ToString("yyyy-MM-dd");
            worksheet.Cell(2, 38).Value = DateTime.Now.AddYears(3).ToString("yyyy-MM-dd");
            worksheet.Cell(2, 39).Value = "Standard office desktop";
            worksheet.Cell(2, 40).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            worksheet.Cell(2, 41).Value = User.Identity?.Name ?? "System";

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Create memory stream
            var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            memoryStream.Position = 0;

            var fileName = $"Asset_Import_Template_{DateTime.Now:yyyyMMdd}.xlsx";
            
            // Return the file without disposing the stream - ASP.NET Core will handle disposal
            return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Excel template");
            TempData["ErrorMessage"] = "Error generating template file.";
            return RedirectToAction(nameof(Import));
        }
    }

    // GET: Assets/TestImport
    [Authorize(Roles = "Admin,IT,Procurement")]
    public async Task<IActionResult> TestImport()
    {
        try
        {
            // Create a test asset directly in the database
            var testAsset = new Asset
            {
                AssetTag = "TEST001",
                SerialNumber = "TEST-SN-001",
                Manufacturer = "Test Manufacturer",
                Model = "Test Model",
                Category = "Test Category",
                Status = "Active",
                AssignedUserName = "Test User",
                Department = "Test Department",
                Location = "Test Location",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "System"
            };

            _context.Assets.Add(testAsset);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Test asset created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test asset");
            TempData["ErrorMessage"] = $"Error creating test asset: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: Assets/DebugCount
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DebugCount()
    {
        try
        {
            var totalAssets = await _context.Assets.CountAsync();
            var recentAssets = await _context.Assets
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(a => new { a.Id, a.AssetTag, a.CreatedAt })
                .ToListAsync();

            var debugInfo = new
            {
                TotalAssets = totalAssets,
                RecentAssets = recentAssets,
                Timestamp = DateTime.UtcNow
            };

            return Json(debugInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting debug count");
            return Json(new { Error = ex.Message });
        }
    }

    // GET: Assets/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var asset = await _context.Assets
            .Include(a => a.Building)
            .Include(a => a.Floor)
            .Include(a => a.AssignedUser)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (asset == null)
        {
            return NotFound();
        }

        return View(asset);
    }

    // GET: Assets/Edit/5
    [Authorize(Roles = "Admin,IT,Procurement")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
        {
            return NotFound();
        }

        return View(asset);
    }

    // POST: Assets/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,IT,Procurement")]
    public async Task<IActionResult> Edit(int id, [Bind("Id,AssetTag,SerialNumber,ServiceTag,Manufacturer,Model,Category,NetName,AssignedUserName,AssignedUserEmail,Manager,Department,Unit,Location,Floor,Desk,Status,IpAddress,MacAddress,WallPort,SwitchName,SwitchPort,PhoneNumber,Extension,Imei,CardNumber,OsVersion,License1,License2,License3,License4,License5,PurchasePrice,OrderNumber,Vendor,VendorInvoice,PurchaseDate,WarrantyStart,WarrantyEndDate,Notes,BuildingId,FloorId,AssignedUserId")] Asset asset)
    {
        if (id != asset.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingAsset = await _context.Assets.FindAsync(id);
                if (existingAsset == null)
                {
                    return NotFound();
                }

                // Update properties
                existingAsset.AssetTag = asset.AssetTag;
                existingAsset.SerialNumber = asset.SerialNumber;
                existingAsset.ServiceTag = asset.ServiceTag;
                existingAsset.Manufacturer = asset.Manufacturer;
                existingAsset.Model = asset.Model;
                existingAsset.Category = asset.Category;
                existingAsset.NetName = asset.NetName;
                existingAsset.AssignedUserName = asset.AssignedUserName;
                existingAsset.AssignedUserEmail = asset.AssignedUserEmail;
                existingAsset.Manager = asset.Manager;
                existingAsset.Department = asset.Department;
                existingAsset.Unit = asset.Unit;
                existingAsset.Location = asset.Location;
                existingAsset.Floor = asset.Floor;
                existingAsset.Desk = asset.Desk;
                existingAsset.Status = asset.Status;
                existingAsset.IpAddress = asset.IpAddress;
                existingAsset.MacAddress = asset.MacAddress;
                existingAsset.WallPort = asset.WallPort;
                existingAsset.SwitchName = asset.SwitchName;
                existingAsset.SwitchPort = asset.SwitchPort;
                existingAsset.PhoneNumber = asset.PhoneNumber;
                existingAsset.Extension = asset.Extension;
                existingAsset.Imei = asset.Imei;
                existingAsset.CardNumber = asset.CardNumber;
                existingAsset.OsVersion = asset.OsVersion;
                existingAsset.License1 = asset.License1;
                existingAsset.License2 = asset.License2;
                existingAsset.License3 = asset.License3;
                existingAsset.License4 = asset.License4;
                existingAsset.License5 = asset.License5;
                existingAsset.PurchasePrice = asset.PurchasePrice;
                existingAsset.OrderNumber = asset.OrderNumber;
                existingAsset.Vendor = asset.Vendor;
                existingAsset.VendorInvoice = asset.VendorInvoice;
                existingAsset.PurchaseDate = asset.PurchaseDate;
                existingAsset.WarrantyStart = asset.WarrantyStart;
                existingAsset.WarrantyEndDate = asset.WarrantyEndDate;
                existingAsset.Notes = asset.Notes;
                existingAsset.UpdatedAt = DateTime.UtcNow;
                existingAsset.UpdatedBy = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                // Track asset history (synchronous)
                var assetHistory = new AssetHistory
                {
                    AssetId = asset.Id,
                    Action = "Updated",
                    Description = $"Asset {asset.AssetTag} was updated",
                    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System",
                    Timestamp = DateTime.UtcNow
                };
                await _context.AssetHistory.AddAsync(assetHistory);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Asset {AssetTag} updated by {User}", asset.AssetTag, User.Identity?.Name);
                TempData["SuccessMessage"] = $"Asset {asset.AssetTag} updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssetExists(asset.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(asset);
    }

    // GET: Assets/Delete/5
    [Authorize(Roles = "Admin,IT")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var asset = await _context.Assets
            .FirstOrDefaultAsync(m => m.Id == id);

        if (asset == null)
        {
            return NotFound();
        }

        return View(asset);
    }

    // POST: Assets/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,IT")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                TempData["ErrorMessage"] = "Asset not found.";
                return RedirectToAction(nameof(Index));
            }

            var assetTag = asset.AssetTag;
            var beforeCount = await _context.Assets.CountAsync();

            // Delete AssetHistory records first to avoid foreign key constraint
            var assetHistoryRecords = await _context.AssetHistory
                .Where(ah => ah.AssetId == id)
                .ToListAsync();
            
            if (assetHistoryRecords.Any())
            {
                _context.AssetHistory.RemoveRange(assetHistoryRecords);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} AssetHistory records for asset {AssetTag}", 
                    assetHistoryRecords.Count, assetTag);
            }

            // Hard delete - permanently remove from database
            _context.Assets.Remove(asset);
            var result = await _context.SaveChangesAsync();

            var afterCount = await _context.Assets.CountAsync();

            _logger.LogInformation("Asset {AssetTag} permanently deleted by {User}. Before: {BeforeCount}, After: {AfterCount}, Result: {Result}", 
                assetTag, User.Identity?.Name, beforeCount, afterCount, result);
            
            TempData["SuccessMessage"] = $"Asset {assetTag} permanently deleted. Database count: {beforeCount} → {afterCount}";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting asset with ID {Id}", id);
            TempData["ErrorMessage"] = $"Error deleting asset: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    private bool AssetExists(int id)
    {
        return _context.Assets.Any(e => e.Id == id);
    }

    // POST: Assets/BulkDelete
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkDelete(int[] selectedIds)
    {
        if (selectedIds == null || selectedIds.Length == 0)
        {
            TempData["ErrorMessage"] = "No assets selected for deletion.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var assetsToDelete = await _context.Assets
                .Where(a => selectedIds.Contains(a.Id))
                .ToListAsync();

            if (!assetsToDelete.Any())
            {
                TempData["ErrorMessage"] = "No valid assets found for deletion.";
                return RedirectToAction(nameof(Index));
            }

            var beforeCount = await _context.Assets.CountAsync();

            // Delete AssetHistory records first to avoid foreign key constraint
            var assetIds = assetsToDelete.Select(a => a.Id).ToList();
            var assetHistoryRecords = await _context.AssetHistory
                .Where(ah => assetIds.Contains(ah.AssetId))
                .ToListAsync();
            
            if (assetHistoryRecords.Any())
            {
                _context.AssetHistory.RemoveRange(assetHistoryRecords);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} AssetHistory records for bulk delete operation", 
                    assetHistoryRecords.Count);
            }

            // Hard delete - permanently remove from database
            _context.Assets.RemoveRange(assetsToDelete);
            var result = await _context.SaveChangesAsync();

            var afterCount = await _context.Assets.CountAsync();
            var deletedCount = assetsToDelete.Count;

            _logger.LogInformation("Bulk permanently deleted {Count} assets by {User}. Before: {BeforeCount}, After: {AfterCount}, Result: {Result}", 
                deletedCount, User.Identity?.Name, beforeCount, afterCount, result);
            TempData["SuccessMessage"] = $"Successfully permanently deleted {deletedCount} assets. Database count: {beforeCount} → {afterCount}";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk delete operation");
            TempData["ErrorMessage"] = $"Error during bulk delete: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }



    // GET: Assets/ReviewErrors
    [Authorize(Roles = "Admin,IT,Procurement")]
    public IActionResult ReviewErrors()
    {
        try
        {
            // Get the last import result from session
            var errorDetails = HttpContext.Session.GetString("LastImportErrors");
            if (string.IsNullOrEmpty(errorDetails))
            {
                TempData["ErrorMessage"] = "No error report available. Please run an import first.";
                return RedirectToAction(nameof(Index));
            }

            // Deserialize the error details
            var importErrors = System.Text.Json.JsonSerializer.Deserialize<List<ImportError>>(errorDetails);
            if (importErrors == null || !importErrors.Any())
            {
                TempData["ErrorMessage"] = "No errors found in the last import.";
                return RedirectToAction(nameof(Index));
            }

            return View(importErrors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading error review page");
            TempData["ErrorMessage"] = "Error loading error review page.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: Assets/ClearImportErrors
    [Authorize(Roles = "Admin,IT,Procurement")]
    public IActionResult ClearImportErrors()
    {
        try
        {
            // Clear the import errors from session
            HttpContext.Session.Remove("LastImportErrors");
            TempData["SuccessMessage"] = "Import errors cleared successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing import errors");
            TempData["ErrorMessage"] = "Error clearing import errors.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: Assets/ClearAllImportData
    [Authorize(Roles = "Admin,IT,Procurement")]
    public IActionResult ClearAllImportData()
    {
        try
        {
            // Clear all import-related session data
            HttpContext.Session.Remove("LastImportErrors");
            HttpContext.Session.Remove("TempImportFile");
            HttpContext.Session.Remove("TempImportFileName");
            
            TempData["SuccessMessage"] = "All import data cleared successfully. You can now start fresh.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all import data");
            TempData["ErrorMessage"] = "Error clearing import data.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: Assets/ResetImportState
    [Authorize(Roles = "Admin,IT,Procurement")]
    public IActionResult ResetImportState()
    {
        try
        {
            // Clear all session data completely
            HttpContext.Session.Clear();
            
            TempData["SuccessMessage"] = "Import state completely reset. All session data cleared. You can now start a completely fresh import.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting import state");
            TempData["ErrorMessage"] = "Error resetting import state.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Assets/DownloadCorrectedErrors
    [Authorize(Roles = "Admin,IT,Procurement")]
    [HttpPost]
    public IActionResult DownloadCorrectedErrors([FromBody] List<ImportError> correctedErrors, [FromQuery] bool includeErrorColumns = true)
    {
        try
        {
            if (correctedErrors == null || !correctedErrors.Any())
            {
                return BadRequest("No corrected errors provided.");
            }

            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Corrected Import Data");

            // Define headers - include error columns only if requested
            var baseHeaders = new[]
            {
                "Asset Tag", "Serial Number", "Service Tag", "Manufacturer", "Model", "Category", "Net Name",
                "Assigned User Name", "Assigned User Email", "Manager", "Department", "Unit", "Location", "Floor", "Desk",
                "Status", "IP Address", "MAC Address", "Wall Port", "Switch Name", "Switch Port", "Phone Number",
                "Extension", "IMEI", "Card Number", "OS Version", "License1", "License2", "License3", "License4", "License5",
                "Purchase Price", "Order Number", "Vendor", "Vendor Invoice", "Purchase Date", "Warranty Start", "Warranty End Date",
                "Notes"
            };

            var headers = includeErrorColumns 
                ? baseHeaders.Concat(new[] { "Row Number", "Error Message" }).ToArray()
                : baseHeaders;

            // Add headers
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Add corrected data
            int row = 2;
            foreach (var error in correctedErrors)
            {
                int col = 1;
                
                // Add the corrected row data
                if (error.RowData != null)
                {
                    foreach (var header in baseHeaders)
                    {
                        if (error.RowData.ContainsKey(header))
                        {
                            var value = error.RowData[header];
                            worksheet.Cell(row, col).Value = value?.ToString() ?? "";
                        }
                        col++;
                    }
                }

                // Add Row Number and Error Message at the end if requested
                if (includeErrorColumns)
                {
                    worksheet.Cell(row, col).Value = error.RowNumber;
                    worksheet.Cell(row, col + 1).Value = error.ErrorMessage;
                }

                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Create memory stream
            var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            memoryStream.Position = 0;

            var fileName = $"Corrected_Import_Data_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            
            return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating corrected error report");
            return BadRequest("Error generating corrected error report.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportVisibleColumns(string columnIds, string searchTerm = "", string sortOrder = "")
    {
        if (string.IsNullOrEmpty(columnIds))
        {
            return BadRequest("No columns specified for export");
        }

        // Parse the column IDs
        var requestedColumns = columnIds.Split(',').ToList();
        
        // Debug logging
        _logger.LogInformation($"Export requested with columns: {string.Join(", ", requestedColumns)}");
        _logger.LogInformation($"Search term: {searchTerm}, Sort order: {sortOrder}");
        
        // Get the assets with same filtering and sorting as the main view
        var assets = _context.Assets.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(searchTerm))
        {
            assets = assets.Where(a => 
                (a.AssetTag != null && a.AssetTag.Contains(searchTerm)) ||
                (a.SerialNumber != null && a.SerialNumber.Contains(searchTerm)) ||
                (a.ServiceTag != null && a.ServiceTag.Contains(searchTerm)) ||
                (a.Manufacturer != null && a.Manufacturer.Contains(searchTerm)) ||
                (a.Model != null && a.Model.Contains(searchTerm)) ||
                (a.Category != null && a.Category.Contains(searchTerm)) ||
                (a.NetName != null && a.NetName.Contains(searchTerm)) ||
                (a.AssignedUserName != null && a.AssignedUserName.Contains(searchTerm)) ||
                (a.AssignedUserEmail != null && a.AssignedUserEmail.Contains(searchTerm)) ||
                (a.Manager != null && a.Manager.Contains(searchTerm)) ||
                (a.Department != null && a.Department.Contains(searchTerm)) ||
                (a.Unit != null && a.Unit.Contains(searchTerm)) ||
                (a.Location != null && a.Location.Contains(searchTerm)) ||
                (a.Floor != null && a.Floor.Contains(searchTerm)) ||
                (a.Desk != null && a.Desk.Contains(searchTerm)) ||
                (a.Status != null && a.Status.Contains(searchTerm)) ||
                (a.IpAddress != null && a.IpAddress.Contains(searchTerm)) ||
                (a.MacAddress != null && a.MacAddress.Contains(searchTerm)) ||
                (a.WallPort != null && a.WallPort.Contains(searchTerm)) ||
                (a.SwitchName != null && a.SwitchName.Contains(searchTerm)) ||
                (a.SwitchPort != null && a.SwitchPort.Contains(searchTerm)) ||
                (a.PhoneNumber != null && a.PhoneNumber.Contains(searchTerm)) ||
                (a.Extension != null && a.Extension.Contains(searchTerm)) ||
                (a.Imei != null && a.Imei.Contains(searchTerm)) ||
                (a.CardNumber != null && a.CardNumber.Contains(searchTerm)) ||
                (a.OsVersion != null && a.OsVersion.Contains(searchTerm)) ||
                (a.License1 != null && a.License1.Contains(searchTerm)) ||
                (a.License2 != null && a.License2.Contains(searchTerm)) ||
                (a.License3 != null && a.License3.Contains(searchTerm)) ||
                (a.License4 != null && a.License4.Contains(searchTerm)) ||
                (a.License5 != null && a.License5.Contains(searchTerm)) ||
                (a.OrderNumber != null && a.OrderNumber.Contains(searchTerm)) ||
                (a.Vendor != null && a.Vendor.Contains(searchTerm)) ||
                (a.VendorInvoice != null && a.VendorInvoice.Contains(searchTerm)) ||
                (a.Notes != null && a.Notes.Contains(searchTerm)) ||
                (a.CreatedBy != null && a.CreatedBy.Contains(searchTerm))
            );
        }

        // Apply sorting
        assets = sortOrder switch
        {
            "assetTag_desc" => assets.OrderByDescending(a => a.AssetTag),
            "serialNumber" => assets.OrderBy(a => a.SerialNumber),
            "serialNumber_desc" => assets.OrderByDescending(a => a.SerialNumber),
            "serviceTag" => assets.OrderBy(a => a.ServiceTag),
            "serviceTag_desc" => assets.OrderByDescending(a => a.ServiceTag),
            "manufacturer" => assets.OrderBy(a => a.Manufacturer),
            "manufacturer_desc" => assets.OrderByDescending(a => a.Manufacturer),
            "model" => assets.OrderBy(a => a.Model),
            "model_desc" => assets.OrderByDescending(a => a.Model),
            "category" => assets.OrderBy(a => a.Category),
            "category_desc" => assets.OrderByDescending(a => a.Category),
            "netName" => assets.OrderBy(a => a.NetName),
            "netName_desc" => assets.OrderByDescending(a => a.NetName),
            "assignedUser" => assets.OrderBy(a => a.AssignedUserName),
            "assignedUser_desc" => assets.OrderByDescending(a => a.AssignedUserName),
            "assignedUserEmail" => assets.OrderBy(a => a.AssignedUserEmail),
            "assignedUserEmail_desc" => assets.OrderByDescending(a => a.AssignedUserEmail),
            "manager" => assets.OrderBy(a => a.Manager),
            "manager_desc" => assets.OrderByDescending(a => a.Manager),
            "department" => assets.OrderBy(a => a.Department),
            "department_desc" => assets.OrderByDescending(a => a.Department),
            "unit" => assets.OrderBy(a => a.Unit),
            "unit_desc" => assets.OrderByDescending(a => a.Unit),
            "location" => assets.OrderBy(a => a.Location),
            "location_desc" => assets.OrderByDescending(a => a.Location),
            "floor" => assets.OrderBy(a => a.Floor),
            "floor_desc" => assets.OrderByDescending(a => a.Floor),
            "desk" => assets.OrderBy(a => a.Desk),
            "desk_desc" => assets.OrderByDescending(a => a.Desk),
            "status" => assets.OrderBy(a => a.Status),
            "status_desc" => assets.OrderByDescending(a => a.Status),
            "ipAddress" => assets.OrderBy(a => a.IpAddress),
            "ipAddress_desc" => assets.OrderByDescending(a => a.IpAddress),
            "macAddress" => assets.OrderBy(a => a.MacAddress),
            "macAddress_desc" => assets.OrderByDescending(a => a.MacAddress),
            "wallPort" => assets.OrderBy(a => a.WallPort),
            "wallPort_desc" => assets.OrderByDescending(a => a.WallPort),
            "switchName" => assets.OrderBy(a => a.SwitchName),
            "switchName_desc" => assets.OrderByDescending(a => a.SwitchName),
            "switchPort" => assets.OrderBy(a => a.SwitchPort),
            "switchPort_desc" => assets.OrderByDescending(a => a.SwitchPort),
            "phoneNumber" => assets.OrderBy(a => a.PhoneNumber),
            "phoneNumber_desc" => assets.OrderByDescending(a => a.PhoneNumber),
            "extension" => assets.OrderBy(a => a.Extension),
            "extension_desc" => assets.OrderByDescending(a => a.Extension),
            "imei" => assets.OrderBy(a => a.Imei),
            "imei_desc" => assets.OrderByDescending(a => a.Imei),
            "cardNumber" => assets.OrderBy(a => a.CardNumber),
            "cardNumber_desc" => assets.OrderByDescending(a => a.CardNumber),
            "osVersion" => assets.OrderBy(a => a.OsVersion),
            "osVersion_desc" => assets.OrderByDescending(a => a.OsVersion),
            "license1" => assets.OrderBy(a => a.License1),
            "license1_desc" => assets.OrderByDescending(a => a.License1),
            "license2" => assets.OrderBy(a => a.License2),
            "license2_desc" => assets.OrderByDescending(a => a.License2),
            "license3" => assets.OrderBy(a => a.License3),
            "license3_desc" => assets.OrderByDescending(a => a.License3),
            "license4" => assets.OrderBy(a => a.License4),
            "license4_desc" => assets.OrderByDescending(a => a.License4),
            "license5" => assets.OrderBy(a => a.License5),
            "license5_desc" => assets.OrderByDescending(a => a.License5),
            "purchaseOrderNumber" => assets.OrderBy(a => a.OrderNumber),
            "purchaseOrderNumber_desc" => assets.OrderByDescending(a => a.OrderNumber),
            "vendor" => assets.OrderBy(a => a.Vendor),
            "vendor_desc" => assets.OrderByDescending(a => a.Vendor),
            "vendorInvoice" => assets.OrderBy(a => a.VendorInvoice),
            "vendorInvoice_desc" => assets.OrderByDescending(a => a.VendorInvoice),
            "purchaseDate" => assets.OrderBy(a => a.PurchaseDate),
            "purchaseDate_desc" => assets.OrderByDescending(a => a.PurchaseDate),
            "warrantyStart" => assets.OrderBy(a => a.WarrantyStart),
            "warrantyStart_desc" => assets.OrderByDescending(a => a.WarrantyStart),
            "warrantyEnd" => assets.OrderBy(a => a.WarrantyEndDate),
            "warrantyEnd_desc" => assets.OrderByDescending(a => a.WarrantyEndDate),
            "notes" => assets.OrderBy(a => a.Notes),
            "notes_desc" => assets.OrderByDescending(a => a.Notes),
            "createdAt" => assets.OrderBy(a => a.CreatedAt),
            "createdAt_desc" => assets.OrderByDescending(a => a.CreatedAt),
            "createdBy" => assets.OrderBy(a => a.CreatedBy),
            "createdBy_desc" => assets.OrderByDescending(a => a.CreatedBy),
            _ => assets.OrderBy(a => a.AssetTag),
        };

        var assetList = await assets.ToListAsync();

        // Create Excel workbook
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Assets");

        // Define column mappings
        var columnMappings = new Dictionary<string, (string Header, Func<Domain.Entities.Asset, object> GetValue)>
        {
            ["assetTag"] = ("Asset Tag", a => a.AssetTag ?? ""),
            ["serialNumber"] = ("Serial Number", a => a.SerialNumber ?? ""),
            ["serviceTag"] = ("Service Tag", a => a.ServiceTag ?? ""),
            ["manufacturer"] = ("Manufacturer", a => a.Manufacturer ?? ""),
            ["model"] = ("Model", a => a.Model ?? ""),
            ["category"] = ("Category", a => a.Category ?? ""),
            ["netName"] = ("Net Name", a => a.NetName ?? ""),
            ["assignedUser"] = ("Assigned User", a => a.AssignedUserName ?? ""),
            ["assignedUserEmail"] = ("Assigned User Email", a => a.AssignedUserEmail ?? ""),
            ["manager"] = ("Manager", a => a.Manager ?? ""),
            ["department"] = ("Department", a => a.Department ?? ""),
            ["unit"] = ("Unit", a => a.Unit ?? ""),
            ["location"] = ("Location", a => a.Location ?? ""),
            ["floor"] = ("Floor", a => a.Floor ?? ""),
            ["desk"] = ("Desk", a => a.Desk ?? ""),
            ["status"] = ("Status", a => a.Status ?? ""),
            ["ipAddress"] = ("IP Address", a => a.IpAddress ?? ""),
            ["macAddress"] = ("MAC Address", a => a.MacAddress ?? ""),
            ["wallPort"] = ("Wall Port", a => a.WallPort ?? ""),
            ["switchName"] = ("Switch Name", a => a.SwitchName ?? ""),
            ["switchPort"] = ("Switch Port", a => a.SwitchPort ?? ""),
            ["phoneNumber"] = ("Phone Number", a => a.PhoneNumber ?? ""),
            ["extension"] = ("Extension", a => a.Extension ?? ""),
            ["imei"] = ("IMEI", a => a.Imei ?? ""),
            ["cardNumber"] = ("Card Number", a => a.CardNumber ?? ""),
            ["osVersion"] = ("OS Version", a => a.OsVersion ?? ""),
            ["license1"] = ("License1", a => a.License1 ?? ""),
            ["license2"] = ("License2", a => a.License2 ?? ""),
            ["license3"] = ("License3", a => a.License3 ?? ""),
            ["license4"] = ("License4", a => a.License4 ?? ""),
            ["license5"] = ("License5", a => a.License5 ?? ""),
            ["purchaseOrderNumber"] = ("Purchase Order Number", a => a.OrderNumber ?? ""),
            ["vendor"] = ("Vendor", a => a.Vendor ?? ""),
            ["vendorInvoice"] = ("Vendor Invoice", a => a.VendorInvoice ?? ""),
            ["purchaseDate"] = ("Purchase Date", a => a.PurchaseDate?.ToString("MM/dd/yyyy") ?? ""),
            ["warrantyStart"] = ("Warranty Start", a => a.WarrantyStart?.ToString("MM/dd/yyyy") ?? ""),
            ["warrantyEnd"] = ("Warranty End", a => a.WarrantyEndDate?.ToString("MM/dd/yyyy") ?? ""),
            ["notes"] = ("Notes", a => a.Notes ?? ""),
            ["createdAt"] = ("Created At", a => a.CreatedAt.ToString("MM/dd/yyyy")),
            ["createdBy"] = ("Created By", a => a.CreatedBy ?? "")
        };

        // Add headers for requested columns - maintain the order as sent
        var validColumns = new List<string>();
        foreach (var col in requestedColumns)
        {
            if (columnMappings.ContainsKey(col))
            {
                validColumns.Add(col);
            }
        }
        
        // Debug logging
        _logger.LogInformation($"Valid columns for export: {string.Join(", ", validColumns)}");
        _logger.LogInformation($"Invalid columns (not found in mappings): {string.Join(", ", requestedColumns.Where(col => !columnMappings.ContainsKey(col)))}");
        
        for (int i = 0; i < validColumns.Count; i++)
        {
            worksheet.Cell(1, i + 1).Value = columnMappings[validColumns[i]].Header;
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Add data rows
        for (int row = 0; row < assetList.Count; row++)
        {
            var asset = assetList[row];
            for (int col = 0; col < validColumns.Count; col++)
            {
                var value = columnMappings[validColumns[col]].GetValue(asset);
                worksheet.Cell(row + 2, col + 1).Value = value?.ToString() ?? "";
            }
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Generate file
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"Assets_Custom_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(stream.ToArray(), 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
            fileName);
    }

    // POST: Assets/UpdateField - Inline editing
    [HttpPost]
    [Authorize(Roles = "Admin,IT,Procurement")]
    public async Task<IActionResult> UpdateField(int id, string field, string value)
    {
        try
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return Json(new { success = false, message = "Asset not found" });
            }

            // Validate field name
            var validFields = new[] { 
                "AssetTag", "SerialNumber", "ServiceTag", "Manufacturer", "Model", "Category",
                "NetName", "AssignedUserName", "AssignedUserEmail", "Manager", "Department", "Unit",
                "Location", "Floor", "Desk", "Status", "IpAddress", "MacAddress", "WallPort",
                "SwitchName", "SwitchPort", "PhoneNumber", "Extension", "Imei", "CardNumber",
                "OsVersion", "License1", "License2", "License3", "License4", "License5",
                "OrderNumber", "Vendor", "VendorInvoice", "Notes"
            };

            if (!validFields.Contains(field))
            {
                return Json(new { success = false, message = "Invalid field" });
            }

            // Validate and set the field value
            switch (field)
            {
                case "AssetTag":
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return Json(new { success = false, message = "Asset Tag is required" });
                    }
                    if (value.Length > 50)
                    {
                        return Json(new { success = false, message = "Asset Tag cannot exceed 50 characters" });
                    }
                    // Check for duplicate Asset Tag
                    var existingAsset = await _context.Assets.FirstOrDefaultAsync(a => a.AssetTag == value && a.Id != id);
                    if (existingAsset != null)
                    {
                        return Json(new { success = false, message = "Asset Tag already exists" });
                    }
                    asset.AssetTag = value;
                    break;

                case "Location":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        var validLocations = new[] { "LIC", "BROOKLYN", "BRONX", "STATEN ISLAND", "66JOHN" };
                        if (!validLocations.Contains(value.ToUpper()))
                        {
                            return Json(new { success = false, message = "Invalid location. Valid locations: LIC, BROOKLYN, BRONX, STATEN ISLAND, 66JOHN" });
                        }
                        asset.Location = value.ToUpper();
                    }
                    else
                    {
                        asset.Location = value;
                    }
                    break;

                case "Status":
                    var validStatuses = new[] { "Active", "Inactive", "Maintenance", "Retired" };
                    if (!validStatuses.Contains(value))
                    {
                        return Json(new { success = false, message = "Invalid status. Valid statuses: Active, Inactive, Maintenance, Retired" });
                    }
                    asset.Status = value;
                    break;

                case "PhoneNumber":
                    if (!string.IsNullOrWhiteSpace(value) && value.Length > 50)
                    {
                        return Json(new { success = false, message = "Phone Number cannot exceed 50 characters" });
                    }
                    asset.PhoneNumber = value;
                    break;

                case "IpAddress":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        if (!System.Net.IPAddress.TryParse(value, out _))
                        {
                            return Json(new { success = false, message = "Invalid IP Address format" });
                        }
                    }
                    asset.IpAddress = value;
                    break;

                case "MacAddress":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        var macRegex = new System.Text.RegularExpressions.Regex(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$");
                        if (!macRegex.IsMatch(value))
                        {
                            return Json(new { success = false, message = "Invalid MAC Address format (use XX:XX:XX:XX:XX:XX)" });
                        }
                    }
                    asset.MacAddress = value;
                    break;

                case "AssignedUserEmail":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        try
                        {
                            var email = new System.Net.Mail.MailAddress(value);
                        }
                        catch
                        {
                            return Json(new { success = false, message = "Invalid email format" });
                        }
                    }
                    asset.AssignedUserEmail = value;
                    break;

                default:
                    // For other string fields, just set the value
                    var property = typeof(Domain.Entities.Asset).GetProperty(field);
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(asset, value);
                    }
                    break;
            }

            // Update audit fields
            asset.UpdatedAt = DateTime.UtcNow;
            asset.UpdatedBy = User.Identity?.Name ?? "System";

            // Track asset history
            var assetHistory = new AssetHistory
            {
                AssetId = asset.Id,
                Action = "Updated",
                Description = $"Field '{field}' updated to '{value}' by {User.Identity?.Name}",
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System",
                Timestamp = DateTime.UtcNow
            };

            await _context.AssetHistory.AddAsync(assetHistory);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Asset {AssetId} field {Field} updated to {Value} by {User}", 
                id, field, value, User.Identity?.Name);

            return Json(new { success = true, message = "Field updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating asset field {Field} to {Value} for asset {Id}", field, value, id);
            return Json(new { success = false, message = "Error updating field: " + ex.Message });
        }
    }

    // GET: Assets/ExportAll
    [HttpGet]
    [Authorize(Roles = "Admin,IT")]
    public async Task<IActionResult> ExportAll()
    {
        try
        {
            var assets = await _context.Assets
                .OrderBy(a => a.AssetTag)
                .ToListAsync();

            // Create Excel workbook
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Assets");

            // Define all columns for complete export
            var columns = new (string Header, Func<Domain.Entities.Asset, object> GetValue)[]
            {
                ("Asset Tag", a => a.AssetTag ?? ""),
                ("Serial Number", a => a.SerialNumber ?? ""),
                ("Service Tag", a => a.ServiceTag ?? ""),
                ("Manufacturer", a => a.Manufacturer ?? ""),
                ("Model", a => a.Model ?? ""),
                ("Category", a => a.Category ?? ""),
                ("Net Name", a => a.NetName ?? ""),
                ("Assigned User", a => a.AssignedUserName ?? ""),
                ("Assigned User Email", a => a.AssignedUserEmail ?? ""),
                ("Manager", a => a.Manager ?? ""),
                ("Department", a => a.Department ?? ""),
                ("Unit", a => a.Unit ?? ""),
                ("Location", a => a.Location ?? ""),
                ("Floor", a => a.Floor ?? ""),
                ("Desk", a => a.Desk ?? ""),
                ("Status", a => a.Status ?? ""),
                ("IP Address", a => a.IpAddress ?? ""),
                ("MAC Address", a => a.MacAddress ?? ""),
                ("Wall Port", a => a.WallPort ?? ""),
                ("Switch Name", a => a.SwitchName ?? ""),
                ("Switch Port", a => a.SwitchPort ?? ""),
                ("Phone Number", a => a.PhoneNumber ?? ""),
                ("Extension", a => a.Extension ?? ""),
                ("IMEI", a => a.Imei ?? ""),
                ("Card Number", a => a.CardNumber ?? ""),
                ("OS Version", a => a.OsVersion ?? ""),
                ("License1", a => a.License1 ?? ""),
                ("License2", a => a.License2 ?? ""),
                ("License3", a => a.License3 ?? ""),
                ("License4", a => a.License4 ?? ""),
                ("License5", a => a.License5 ?? ""),
                ("Purchase Price", a => a.PurchasePrice?.ToString() ?? ""),
                ("Order Number", a => a.OrderNumber ?? ""),
                ("Vendor", a => a.Vendor ?? ""),
                ("Vendor Invoice", a => a.VendorInvoice ?? ""),
                ("Purchase Date", a => a.PurchaseDate?.ToString("MM/dd/yyyy") ?? ""),
                ("Warranty Start", a => a.WarrantyStart?.ToString("MM/dd/yyyy") ?? ""),
                ("Warranty End", a => a.WarrantyEndDate?.ToString("MM/dd/yyyy") ?? ""),
                ("Notes", a => a.Notes ?? ""),
                ("Created At", a => a.CreatedAt.ToString("MM/dd/yyyy HH:mm:ss")),
                ("Created By", a => a.CreatedBy ?? ""),
                ("Updated At", a => a.UpdatedAt?.ToString("MM/dd/yyyy HH:mm:ss") ?? ""),
                ("Updated By", a => a.UpdatedBy ?? ""),
                ("Is Active", a => a.IsActive ? "Yes" : "No")
            };

            // Add headers
            for (int i = 0; i < columns.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = columns[i].Header;
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Add data rows
            for (int row = 0; row < assets.Count; row++)
            {
                var asset = assets[row];
                for (int col = 0; col < columns.Length; col++)
                {
                    var value = columns[col].GetValue(asset);
                    worksheet.Cell(row + 2, col + 1).Value = value?.ToString() ?? "";
                }
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Generate file
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"All_Assets_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting all assets");
            return BadRequest("Error exporting assets: " + ex.Message);
        }
    }

    // POST: Assets/DeleteAll
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAll()
    {
        try
        {
            // Get count before deletion for logging
            var assetCount = await _context.Assets.CountAsync();
            
            // Delete all assets
            var assets = await _context.Assets.ToListAsync();
            _context.Assets.RemoveRange(assets);
            
            // Also delete related asset history records
            var assetHistory = await _context.AssetHistory.ToListAsync();
            _context.AssetHistory.RemoveRange(assetHistory);
            
            // Delete asset requests
            var assetRequests = await _context.AssetRequests.ToListAsync();
            _context.AssetRequests.RemoveRange(assetRequests);
            
            await _context.SaveChangesAsync();

            _logger.LogWarning("All {AssetCount} assets deleted by user {User}", assetCount, User.Identity?.Name);

            return Json(new { success = true, message = $"Successfully deleted {assetCount} assets and related records." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all assets");
            return Json(new { success = false, message = "Error deleting assets: " + ex.Message });
        }
    }
}
