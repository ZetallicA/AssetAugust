using AssetManagement.Domain.Entities;
using AssetManagement.Domain.Models;
using AssetManagement.Infrastructure.Data;
using AssetManagement.Infrastructure.Services;
using AssetManagement.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ClosedXML.Excel;
using System.IO;
using AssetManagement.Web.Models;

namespace AssetManagement.Web.Controllers;

[Authorize]
public class AssetsController : Controller
{
    private readonly AssetManagementDbContext _context;
    private readonly IExcelImportService _excelImportService;
    private readonly IAssetSearchService _searchService;
    private readonly AssetLifecycleService _lifecycleService;
    private readonly TransferService _transferService;
    private readonly SalvageService _salvageService;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(
        AssetManagementDbContext context,
        IExcelImportService excelImportService,
        IAssetSearchService searchService,
        AssetLifecycleService lifecycleService,
        TransferService transferService,
        SalvageService salvageService,
        ILogger<AssetsController> logger)
    {
        _context = context;
        _excelImportService = excelImportService;
        _searchService = searchService;
        _lifecycleService = lifecycleService;
        _transferService = transferService;
        _salvageService = salvageService;
        _logger = logger;
    }

    // GET: Assets
    public async Task<IActionResult> Index(string searchTerm, string sortOrder, int page = 1, int pageSize = 25,
        string? filterAssetTag = null, string? filterSerialNumber = null, string? filterServiceTag = null,
        string? filterManufacturer = null, string? filterModel = null, string? filterCategory = null,
        string? filterNetName = null, string? filterAssignedUser = null, string? filterManager = null, string? filterDepartment = null,
        string? filterLocation = null, string? filterFloor = null, string? filterDesk = null, string? filterStatus = null,
        string? filterVendor = null, string? filterWarrantyEnd = null)
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
        ViewData["LifecycleSortParm"] = sortOrder == "lifecycle" ? "lifecycle_desc" : "lifecycle";
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
                (a.LifecycleState.ToString().Contains(searchTerm)) ||
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

        // Apply column filters
        if (!String.IsNullOrEmpty(filterAssetTag))
        {
            var assetTags = filterAssetTag.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            assetsQuery = assetsQuery.Where(a => assetTags.Contains(a.AssetTag));
        }

        if (!String.IsNullOrEmpty(filterSerialNumber))
        {
            var serialNumbers = filterSerialNumber.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            assetsQuery = assetsQuery.Where(a => a.SerialNumber != null && serialNumbers.Contains(a.SerialNumber));
        }

        if (!String.IsNullOrEmpty(filterServiceTag))
        {
            var serviceTags = filterServiceTag.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            assetsQuery = assetsQuery.Where(a => a.ServiceTag != null && serviceTags.Contains(a.ServiceTag));
        }

        if (!String.IsNullOrEmpty(filterManufacturer))
        {
            var manufacturers = filterManufacturer.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            assetsQuery = assetsQuery.Where(a => a.Manufacturer != null && manufacturers.Contains(a.Manufacturer));
        }

        if (!String.IsNullOrEmpty(filterModel))
        {
            var models = filterModel.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            assetsQuery = assetsQuery.Where(a => a.Model != null && models.Contains(a.Model));
        }

        if (!String.IsNullOrEmpty(filterCategory))
        {
            var categories = filterCategory.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            assetsQuery = assetsQuery.Where(a => a.Category != null && categories.Contains(a.Category));
        }

        if (!String.IsNullOrEmpty(filterNetName))
        {
            var netNames = filterNetName.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            assetsQuery = assetsQuery.Where(a => a.NetName != null && netNames.Contains(a.NetName));
        }

        if (!String.IsNullOrEmpty(filterAssignedUser))
        {
            var assignedUsers = filterAssignedUser.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            assetsQuery = assetsQuery.Where(a => a.AssignedUserName != null && assignedUsers.Contains(a.AssignedUserName));
        }

        if (!String.IsNullOrEmpty(filterManager))
        {
            var managers = filterManager.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            assetsQuery = assetsQuery.Where(a => a.Manager != null && managers.Contains(a.Manager));
        }

        if (!String.IsNullOrEmpty(filterDepartment))
        {
            var departments = filterDepartment.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            assetsQuery = assetsQuery.Where(a => a.Department != null && departments.Contains(a.Department));
        }

        if (!String.IsNullOrEmpty(filterLocation))
        {
            var locations = filterLocation.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            assetsQuery = assetsQuery.Where(a => a.Location != null && locations.Contains(a.Location));
        }

        if (!String.IsNullOrEmpty(filterFloor))
        {
            var floors = filterFloor.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            assetsQuery = assetsQuery.Where(a => a.Floor != null && floors.Contains(a.Floor));
        }

        if (!String.IsNullOrEmpty(filterDesk))
        {
            var desks = filterDesk.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            assetsQuery = assetsQuery.Where(a => a.Desk != null && desks.Contains(a.Desk));
        }

        if (!String.IsNullOrEmpty(filterStatus))
        {
            var statuses = filterStatus.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            assetsQuery = assetsQuery.Where(a => a.Status != null && statuses.Contains(a.Status));
        }

        if (!String.IsNullOrEmpty(filterVendor))
        {
            var vendors = filterVendor.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            assetsQuery = assetsQuery.Where(a => a.Vendor != null && vendors.Contains(a.Vendor));
        }

        if (!String.IsNullOrEmpty(filterWarrantyEnd))
        {
            var warrantyEnds = filterWarrantyEnd.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            assetsQuery = assetsQuery.Where(a => a.WarrantyEndDate != null && warrantyEnds.Contains(a.WarrantyEndDate.Value.ToString("yyyy-MM-dd")));
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
            "lifecycle" => assetsQuery.OrderBy(a => a.LifecycleState),
            "lifecycle_desc" => assetsQuery.OrderByDescending(a => a.LifecycleState),
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
            var assetIds = assetsToDelete.Select(a => a.Id).ToList();
            var assetTags = assetsToDelete.Select(a => a.AssetTag).ToList();

            // Delete related records first to avoid foreign key constraint violations
            // Delete AssetEvents (has Restrict constraint)
            var assetEvents = await _context.AssetEvents
                .Where(ae => assetTags.Contains(ae.AssetTag))
                .ToListAsync();
            if (assetEvents.Any())
            {
                _context.AssetEvents.RemoveRange(assetEvents);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} AssetEvents for bulk delete operation", assetEvents.Count);
            }

            // Delete AssetTransfers (has Restrict constraint)
            var assetTransfers = await _context.AssetTransfers
                .Where(at => assetTags.Contains(at.AssetTag))
                .ToListAsync();
            if (assetTransfers.Any())
            {
                _context.AssetTransfers.RemoveRange(assetTransfers);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} AssetTransfers for bulk delete operation", assetTransfers.Count);
            }

            // Delete AssetHistory records (has Restrict constraint)
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

            // Delete AssetRequests
            var assetRequests = await _context.AssetRequests
                .Where(ar => ar.AssetId.HasValue && assetIds.Contains(ar.AssetId.Value))
                .ToListAsync();
            if (assetRequests.Any())
            {
                _context.AssetRequests.RemoveRange(assetRequests);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} AssetRequests for bulk delete operation", assetRequests.Count);
            }

            // Finally delete the assets
            _context.Assets.RemoveRange(assetsToDelete);
            var result = await _context.SaveChangesAsync();

            var afterCount = await _context.Assets.CountAsync();
            var deletedCount = assetsToDelete.Count;

            _logger.LogInformation("Bulk permanently deleted {Count} assets by {User}. Before: {BeforeCount}, After: {AfterCount}, Result: {Result}", 
                deletedCount, User.Identity?.Name, beforeCount, afterCount, result);
            TempData["SuccessMessage"] = $"Successfully deleted {deletedCount} assets and related records. Database count: {beforeCount} → {afterCount}";

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

    // GET: Assets/ExportVisibleColumns
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
            "lifecycle" => assets.OrderBy(a => a.LifecycleState),
            "lifecycle_desc" => assets.OrderByDescending(a => a.LifecycleState),
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
            _ => assets.OrderBy(a => a.AssetTag)
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
                        var validLocations = new[] { "100CHURCH", "LIC", "BROOKLYN", "BRONX", "STATEN ISLAND", "66JOHN" };
                        if (!validLocations.Contains(value.ToUpper()))
                        {
                            return Json(new { success = false, message = "Invalid location. Valid locations: 100CHURCH, LIC, BROOKLYN, BRONX, STATEN ISLAND, 66JOHN" });
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
                ("Lifecycle State", a => a.LifecycleState.ToString()),
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

    // GET: Assets/ExportSelectedAssets
    [HttpGet]
    [Authorize(Roles = "Admin,IT")]
    public async Task<IActionResult> ExportSelectedAssets(string assetTags, string searchTerm = "", string sortOrder = "")
    {
        try
        {
            if (string.IsNullOrEmpty(assetTags))
            {
                return BadRequest("No asset tags provided for export");
            }

            var assetTagList = assetTags.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            // Build query based on asset tags and any additional filters
            var assetsQuery = _context.Assets.AsQueryable();
            
            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                assetsQuery = assetsQuery.Where(a => 
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
                    (a.Notes != null && a.Notes.Contains(searchTerm))
                );
            }
            
            // Apply sorting if provided
            if (!string.IsNullOrEmpty(sortOrder))
            {
                assetsQuery = sortOrder switch
                {
                    "assetTag" => assetsQuery.OrderBy(a => a.AssetTag),
                    "assetTag_desc" => assetsQuery.OrderByDescending(a => a.AssetTag),
                    "serialNumber" => assetsQuery.OrderBy(a => a.SerialNumber),
                    "serialNumber_desc" => assetsQuery.OrderByDescending(a => a.SerialNumber),
                    "manufacturer" => assetsQuery.OrderBy(a => a.Manufacturer),
                    "manufacturer_desc" => assetsQuery.OrderByDescending(a => a.Manufacturer),
                    "model" => assetsQuery.OrderBy(a => a.Model),
                    "model_desc" => assetsQuery.OrderByDescending(a => a.Model),
                    "category" => assetsQuery.OrderBy(a => a.Category),
                    "category_desc" => assetsQuery.OrderByDescending(a => a.Category),
                    "assignedUser" => assetsQuery.OrderBy(a => a.AssignedUserName),
                    "assignedUser_desc" => assetsQuery.OrderByDescending(a => a.AssignedUserName),
                    "department" => assetsQuery.OrderBy(a => a.Department),
                    "department_desc" => assetsQuery.OrderByDescending(a => a.Department),
                    "location" => assetsQuery.OrderBy(a => a.Location),
                    "location_desc" => assetsQuery.OrderByDescending(a => a.Location),
                    "status" => assetsQuery.OrderBy(a => a.Status),
                    "status_desc" => assetsQuery.OrderByDescending(a => a.Status),
                    "createdAt" => assetsQuery.OrderBy(a => a.CreatedAt),
                    "createdAt_desc" => assetsQuery.OrderByDescending(a => a.CreatedAt),
                    _ => assetsQuery.OrderBy(a => a.AssetTag)
                };
            }
            else
            {
                assetsQuery = assetsQuery.OrderBy(a => a.AssetTag);
            }
            
            // Filter by selected asset tags
            assetsQuery = assetsQuery.Where(a => assetTagList.Contains(a.AssetTag));
            
            var assets = await assetsQuery.ToListAsync();

            if (!assets.Any())
            {
                return BadRequest("No assets found matching the selected criteria");
            }

            // Create Excel workbook
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Selected Assets");

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
                ("Lifecycle State", a => a.LifecycleState.ToString()),
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

            var fileName = $"Selected_Assets_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting selected assets");
            return BadRequest("Error exporting selected assets: " + ex.Message);
        }
    }

    // GET: Assets/ExportCurrentView
    [HttpGet]
    [Authorize(Roles = "Admin,IT")]
    public async Task<IActionResult> ExportCurrentView(string searchTerm = "", string sortOrder = "", string columnConfig = "")
    {
        try
        {
            // Build query based on current view filters
            var assetsQuery = _context.Assets.AsQueryable();
            
            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                assetsQuery = assetsQuery.Where(a => 
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
                    (a.Notes != null && a.Notes.Contains(searchTerm))
                );
            }
            
            // Apply sorting if provided
            if (!string.IsNullOrEmpty(sortOrder))
            {
                assetsQuery = sortOrder switch
                {
                    "assetTag" => assetsQuery.OrderBy(a => a.AssetTag),
                    "assetTag_desc" => assetsQuery.OrderByDescending(a => a.AssetTag),
                    "serialNumber" => assetsQuery.OrderBy(a => a.SerialNumber),
                    "serialNumber_desc" => assetsQuery.OrderByDescending(a => a.SerialNumber),
                    "manufacturer" => assetsQuery.OrderBy(a => a.Manufacturer),
                    "manufacturer_desc" => assetsQuery.OrderByDescending(a => a.Manufacturer),
                    "model" => assetsQuery.OrderBy(a => a.Model),
                    "model_desc" => assetsQuery.OrderByDescending(a => a.Model),
                    "category" => assetsQuery.OrderBy(a => a.Category),
                    "category_desc" => assetsQuery.OrderByDescending(a => a.Category),
                    "assignedUser" => assetsQuery.OrderBy(a => a.AssignedUserName),
                    "assignedUser_desc" => assetsQuery.OrderByDescending(a => a.AssignedUserName),
                    "department" => assetsQuery.OrderBy(a => a.Department),
                    "department_desc" => assetsQuery.OrderByDescending(a => a.Department),
                    "location" => assetsQuery.OrderBy(a => a.Location),
                    "location_desc" => assetsQuery.OrderByDescending(a => a.Location),
                    "status" => assetsQuery.OrderBy(a => a.Status),
                    "status_desc" => assetsQuery.OrderByDescending(a => a.Status),
                    "createdAt" => assetsQuery.OrderBy(a => a.CreatedAt),
                    "createdAt_desc" => assetsQuery.OrderByDescending(a => a.CreatedAt),
                    _ => assetsQuery.OrderBy(a => a.AssetTag)
                };
            }
            else
            {
                assetsQuery = assetsQuery.OrderBy(a => a.AssetTag);
            }
            
            var assets = await assetsQuery.ToListAsync();

            if (!assets.Any())
            {
                return BadRequest("No assets found matching the current view criteria");
            }

            // Create Excel workbook
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Current View Assets");

            // Define all possible columns
            var allColumns = new Dictionary<string, (string Header, Func<Domain.Entities.Asset, object> GetValue)>
            {
                { "assetTag", ("Asset Tag", a => a.AssetTag ?? "") },
                { "serialNumber", ("Serial Number", a => a.SerialNumber ?? "") },
                { "serviceTag", ("Service Tag", a => a.ServiceTag ?? "") },
                { "manufacturer", ("Manufacturer", a => a.Manufacturer ?? "") },
                { "model", ("Model", a => a.Model ?? "") },
                { "category", ("Category", a => a.Category ?? "") },
                { "netName", ("Net Name", a => a.NetName ?? "") },
                { "assignedUser", ("Assigned User", a => a.AssignedUserName ?? "") },
                { "assignedUserEmail", ("Assigned User Email", a => a.AssignedUserEmail ?? "") },
                { "manager", ("Manager", a => a.Manager ?? "") },
                { "department", ("Department", a => a.Department ?? "") },
                { "unit", ("Unit", a => a.Unit ?? "") },
                { "location", ("Location", a => a.Location ?? "") },
                { "floor", ("Floor", a => a.Floor ?? "") },
                { "desk", ("Desk", a => a.Desk ?? "") },
                { "status", ("Status", a => a.Status ?? "") },
                { "ipAddress", ("IP Address", a => a.IpAddress ?? "") },
                { "macAddress", ("MAC Address", a => a.MacAddress ?? "") },
                { "wallPort", ("Wall Port", a => a.WallPort ?? "") },
                { "switchName", ("Switch Name", a => a.SwitchName ?? "") },
                { "switchPort", ("Switch Port", a => a.SwitchPort ?? "") },
                { "phoneNumber", ("Phone Number", a => a.PhoneNumber ?? "") },
                { "extension", ("Extension", a => a.Extension ?? "") },
                { "imei", ("IMEI", a => a.Imei ?? "") },
                { "cardNumber", ("Card Number", a => a.CardNumber ?? "") },
                { "osVersion", ("OS Version", a => a.OsVersion ?? "") },
                { "license1", ("License1", a => a.License1 ?? "") },
                { "license2", ("License2", a => a.License2 ?? "") },
                { "license3", ("License3", a => a.License3 ?? "") },
                { "license4", ("License4", a => a.License4 ?? "") },
                { "license5", ("License5", a => a.License5 ?? "") },
                { "purchaseOrderNumber", ("Purchase Order Number", a => a.OrderNumber ?? "") },
                { "vendor", ("Vendor", a => a.Vendor ?? "") },
                { "vendorInvoice", ("Vendor Invoice", a => a.VendorInvoice ?? "") },
                { "purchaseDate", ("Purchase Date", a => a.PurchaseDate?.ToString("MM/dd/yyyy") ?? "") },
                { "warrantyStart", ("Warranty Start", a => a.WarrantyStart?.ToString("MM/dd/yyyy") ?? "") },
                { "warrantyEnd", ("Warranty End", a => a.WarrantyEndDate?.ToString("MM/dd/yyyy") ?? "") },
                { "notes", ("Notes", a => a.Notes ?? "") },
                { "createdAt", ("Created At", a => a.CreatedAt.ToString("MM/dd/yyyy HH:mm:ss")) },
                { "createdBy", ("Created By", a => a.CreatedBy ?? "") },
                { "updatedAt", ("Updated At", a => a.UpdatedAt?.ToString("MM/dd/yyyy HH:mm:ss") ?? "") },
                { "updatedBy", ("Updated By", a => a.UpdatedBy ?? "") },
                { "isActive", ("Is Active", a => a.IsActive ? "Yes" : "No") },
                { "lifecycleState", ("Lifecycle State", a => a.LifecycleState.ToString()) }
            };

            // Parse column configuration if provided
            var columns = new List<(string Header, Func<Domain.Entities.Asset, object> GetValue)>();
            
            if (!string.IsNullOrEmpty(columnConfig))
            {
                try
                {
                    var columnIds = columnConfig.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var columnId in columnIds)
                    {
                        if (allColumns.ContainsKey(columnId))
                        {
                            columns.Add(allColumns[columnId]);
                        }
                    }
                }
                catch
                {
                    // If parsing fails, use default columns
                }
            }

            // If no valid column configuration, use default visible columns
            if (columns.Count == 0)
            {
                var defaultColumnIds = new[] { "assetTag", "serialNumber", "manufacturer", "model", "category", "assignedUser", "department", "location", "status" };
                foreach (var columnId in defaultColumnIds)
                {
                    if (allColumns.ContainsKey(columnId))
                    {
                        columns.Add(allColumns[columnId]);
                    }
                }
            }

            // Add headers
            for (int i = 0; i < columns.Count; i++)
            {
                worksheet.Cell(1, i + 1).Value = columns[i].Header;
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Add data rows
            for (int row = 0; row < assets.Count; row++)
            {
                var asset = assets[row];
                for (int col = 0; col < columns.Count; col++)
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

            var fileName = $"Current_View_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting current view");
            return BadRequest("Error exporting current view: " + ex.Message);
        }
    }

    // GET: Assets/GetFloorsForLocation
    [HttpGet]
    [Authorize(Roles = "Admin,IT")]
    public async Task<IActionResult> GetFloorsForLocation(string location)
    {
        try
        {
            if (string.IsNullOrEmpty(location))
            {
                return BadRequest("Location is required");
            }

            // Get floors for the specified location/building
            var floors = await _context.Floors
                .Where(f => f.Building.BuildingCode == location && f.IsActive)
                .OrderBy(f => f.FloorNumber)
                .Select(f => new { f.Name, f.FloorNumber, f.Description })
                .ToListAsync();

            // Add "Storage" as an option for all locations
            var floorOptions = new List<object>
            {
                new { Name = "Storage", FloorNumber = "Storage", Description = "Storage area" }
            };

            floorOptions.AddRange(floors);

            return Json(new { success = true, floors = floorOptions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting floors for location: {Location}", location);
            return BadRequest("Error getting floors: " + ex.Message);
        }
    }

    // POST: Assets/GetSelectedAssets
    [HttpPost]
    [Authorize(Roles = "Admin,IT")]
    public async Task<IActionResult> GetSelectedAssets([FromBody] List<string> assetTags)
    {
        try
        {
            if (assetTags == null || !assetTags.Any())
            {
                return Json(new { success = false, message = "No asset tags provided" });
            }

            var assets = await _context.Assets
                .Where(a => assetTags.Contains(a.AssetTag))
                .Select(a => new
                {
                    assetTag = a.AssetTag,
                    category = a.Category,
                    manufacturer = a.Manufacturer,
                    model = a.Model,
                    location = a.Location,
                    status = a.Status,
                    lifecycleState = a.LifecycleState.ToString()
                })
                .ToListAsync();

            return Json(new { success = true, assets = assets });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting selected assets");
            return Json(new { success = false, message = "Error retrieving selected assets" });
        }
    }

    // GET: Assets/Cart
    [HttpGet]
    [Authorize(Roles = "Admin,IT")]
    public async Task<IActionResult> Cart(string assetTags)
    {
        try
        {
            if (string.IsNullOrEmpty(assetTags))
            {
                return RedirectToAction("Index");
            }

            var assetTagList = assetTags.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            var assets = await _context.Assets
                .Where(a => assetTagList.Contains(a.AssetTag))
                .ToListAsync();

            ViewBag.AssetTags = assetTags;
            return View(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading cart page");
            return RedirectToAction("Index");
        }
    }

    // POST: Assets/BulkCheckout
    [HttpPost]
    [Authorize(Roles = "Admin,IT")]
    public async Task<IActionResult> BulkCheckout([FromBody] BulkCheckoutRequest request)
    {
        try
        {
            if (request?.AssetTags == null || !request.AssetTags.Any())
            {
                return Json(new { success = false, message = "No asset tags provided" });
            }

            var assets = await _context.Assets
                .Where(a => request.AssetTags.Contains(a.AssetTag))
                .ToListAsync();

            if (!assets.Any())
            {
                return Json(new { success = false, message = "No assets found" });
            }

            foreach (var asset in assets)
            {
                // Update asset location and assignment
                asset.Location = request.Location;
                asset.Floor = request.Floor;
                asset.Desk = request.Desk;
                asset.AssignedUserName = request.User;
                asset.AssignedUserEmail = request.Email;
                asset.Status = "Active";
                asset.LifecycleState = AssetLifecycleState.Deployed;
                asset.UpdatedAt = DateTime.UtcNow;
                asset.UpdatedBy = User.Identity?.Name ?? "System";

                // Add to asset history
                var historyEntry = new AssetHistory
                {
                    AssetId = asset.Id,
                    Action = "Bulk Checkout",
                    Description = $"Checked out to {request.Location} - {request.Floor} (Desk: {request.Desk}) for {request.User}",
                    UserName = User.Identity?.Name ?? "System",
                    Timestamp = DateTime.UtcNow
                };

                _context.AssetHistory.Add(historyEntry);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk checkout of {Count} assets to {Location} by {User}", 
                assets.Count, request.Location, User.Identity?.Name);

            return Json(new { success = true, message = $"Successfully checked out {assets.Count} assets to {request.Location}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk checkout");
            return Json(new { success = false, message = "Error during checkout: " + ex.Message });
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
            
            // Delete related records first to avoid foreign key constraint violations
            // Delete AssetEvents (has Restrict constraint)
            var assetEvents = await _context.AssetEvents.ToListAsync();
            if (assetEvents.Any())
            {
                _context.AssetEvents.RemoveRange(assetEvents);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} AssetEvents", assetEvents.Count);
            }
            
            // Delete AssetTransfers (has Restrict constraint)
            var assetTransfers = await _context.AssetTransfers.ToListAsync();
            if (assetTransfers.Any())
            {
                _context.AssetTransfers.RemoveRange(assetTransfers);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} AssetTransfers", assetTransfers.Count);
            }
            
            // Delete AssetHistory (has Restrict constraint)
            var assetHistory = await _context.AssetHistory.ToListAsync();
            if (assetHistory.Any())
            {
                _context.AssetHistory.RemoveRange(assetHistory);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} AssetHistory records", assetHistory.Count);
            }
            
            // Delete AssetRequests
            var assetRequests = await _context.AssetRequests.ToListAsync();
            if (assetRequests.Any())
            {
                _context.AssetRequests.RemoveRange(assetRequests);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} AssetRequests", assetRequests.Count);
            }
            
            // Finally delete all assets
            var assets = await _context.Assets.ToListAsync();
            _context.Assets.RemoveRange(assets);
            await _context.SaveChangesAsync();

            _logger.LogWarning("All {AssetCount} assets and related records deleted by user {User}", assetCount, User.Identity?.Name);

            return Json(new { success = true, message = $"Successfully deleted {assetCount} assets and all related records." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all assets");
            return Json(new { success = false, message = "Error deleting assets: " + ex.Message });
        }
    }

    // GET: Assets/Search
    [HttpGet]
    [Route("api/assets/search")]
    public async Task<IActionResult> Search(
        string? query = null,
        string? category = null,
        string? location = null,
        string? floor = null,
        string? status = null,
        string? vendor = null,
        bool unassignedOnly = false,
        DateTimeOffset? createdFrom = null,
        DateTimeOffset? createdTo = null,
        DateTimeOffset? warrantyFrom = null,
        DateTimeOffset? warrantyTo = null,
        int page = 1,
        int pageSize = 50,
        string? sortBy = null,
        bool sortDescending = false)
    {
        try
        {
            var request = new AssetSearchRequest
            {
                Query = query,
                Category = category,
                Location = location,
                Floor = floor,
                Status = status,
                Vendor = vendor,
                UnassignedOnly = unassignedOnly,
                CreatedFrom = createdFrom,
                CreatedTo = createdTo,
                WarrantyFrom = warrantyFrom,
                WarrantyTo = warrantyTo,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDescending = sortDescending
            };

            var result = await _searchService.SearchAsync(request);
            
            _logger.LogInformation("Search completed: {Query}, {Total} results in {Time}ms, FTS: {FtsUsed}", 
                query, result.Total, result.SearchTimeMs, result.UsedFullTextSearch);

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during asset search");
            return BadRequest(new { error = "Search failed", message = ex.Message });
        }
    }

    // GET: Assets/Search
    [HttpGet]
    public IActionResult Search()
    {
        return View();
    }

    // POST: Assets/MarkForSalvage
    [HttpPost]
    [Authorize(Roles = "SiteTech,JohnStOps,Admin")]
    public async Task<IActionResult> MarkForSalvage(string assetTag)
    {
        try
        {
            var success = await _lifecycleService.MarkSalvagePending(assetTag, User.Identity?.Name ?? "Unknown");
            if (success)
            {
                return Json(new { success = true, message = $"Asset {assetTag} marked for salvage" });
            }
            return Json(new { success = false, message = "Failed to mark asset for salvage" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking asset {AssetTag} for salvage", assetTag);
            return Json(new { success = false, message = "Error: " + ex.Message });
        }
    }

    // POST: Assets/BulkMarkForSalvage
    [HttpPost]
    [Authorize(Roles = "SiteTech,JohnStOps,Admin")]
    public async Task<IActionResult> BulkMarkForSalvage([FromBody] List<string> assetTags)
    {
        try
        {
            var results = new List<object>();
            var successCount = 0;
            var failCount = 0;

            foreach (var assetTag in assetTags)
            {
                var success = await _lifecycleService.MarkSalvagePending(assetTag, User.Identity?.Name ?? "Unknown");
                if (success)
                {
                    successCount++;
                    results.Add(new { assetTag, success = true });
                }
                else
                {
                    failCount++;
                    results.Add(new { assetTag, success = false, message = "Failed to mark for salvage" });
                }
            }

            return Json(new { 
                success = true, 
                message = $"Marked {successCount} assets for salvage, {failCount} failed",
                results 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk mark for salvage");
            return Json(new { success = false, message = "Error: " + ex.Message });
        }
    }

    // POST: Assets/DeployAsset
    [HttpPost]
    [Authorize(Roles = "SiteTech,JohnStOps,Admin")]
    public async Task<IActionResult> DeployAsset([FromBody] DeployAssetRequest request)
    {
        try
        {
            var success = await _lifecycleService.DeployAsset(
                request.AssetTag, 
                request.Desk, 
                request.UserName, 
                request.UserEmail, 
                User.Identity?.Name ?? "Unknown"
            );
            
            if (success)
            {
                return Json(new { success = true, message = $"Asset {request.AssetTag} deployed successfully" });
            }
            return Json(new { success = false, message = "Failed to deploy asset" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying asset {AssetTag}", request.AssetTag);
            return Json(new { success = false, message = "Error: " + ex.Message });
        }
    }

    // POST: Assets/MoveToStorage
    [HttpPost]
    [Authorize(Roles = "SiteTech,JohnStOps,Admin")]
    public async Task<IActionResult> MoveToStorage(string assetTag)
    {
        try
        {
            var success = await _lifecycleService.TransitionToState(
                assetTag, 
                AssetLifecycleState.InStorage, 
                User.Identity?.Name ?? "Unknown"
            );
            
            if (success)
            {
                return Json(new { success = true, message = $"Asset {assetTag} moved to storage" });
            }
            return Json(new { success = false, message = "Failed to move asset to storage" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving asset {AssetTag} to storage", assetTag);
            return Json(new { success = false, message = "Error: " + ex.Message });
        }
    }

    // GET: Assets/GetLifecycleActions
    [HttpGet]
    public async Task<IActionResult> GetLifecycleActions(string assetTag)
    {
        try
        {
            var asset = await _context.Assets.FirstOrDefaultAsync(a => a.AssetTag == assetTag);
            if (asset == null)
            {
                return Json(new { success = false, message = "Asset not found" });
            }

            var availableActions = new List<string>();
            
            switch (asset.LifecycleState)
            {
                case AssetLifecycleState.InStorage:
                case AssetLifecycleState.Delivered:
                    availableActions.AddRange(new[] { "Deploy", "ReadyForShipment", "MarkForSalvage" });
                    break;
                case AssetLifecycleState.Deployed:
                    availableActions.AddRange(new[] { "Replace", "Redeploy", "MoveToStorage", "ReadyForShipment", "MarkForSalvage" });
                    break;
                case AssetLifecycleState.RedeployPending:
                    availableActions.AddRange(new[] { "Redeploy", "MoveToStorage", "ReadyForShipment" });
                    break;
                case AssetLifecycleState.ReadyForShipment:
                    availableActions.AddRange(new[] { "PickupAsset" }); // Only Facilities Drivers
                    break;
                case AssetLifecycleState.InTransit:
                    availableActions.AddRange(new[] { "DeliverAsset" }); // Only Facilities Drivers
                    break;
                case AssetLifecycleState.SalvagePending:
                    availableActions.AddRange(new[] { "ReadyForShipment" }); // Cannot be redeployed, only shipped for salvage
                    break;
                case AssetLifecycleState.Salvaged:
                    availableActions.AddRange(new[] { "View" });
                    break;
            }

            return Json(new { success = true, actions = availableActions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lifecycle actions for asset {AssetTag}", assetTag);
            return Json(new { success = false, message = "Error: " + ex.Message });
        }
    }

    // POST: Assets/BulkMoveToStorage
    [HttpPost]
    [Authorize(Roles = "Admin,IT")]
    public async Task<IActionResult> BulkMoveToStorage([FromBody] List<string> assetTags)
    {
        try
        {
            if (assetTags == null || !assetTags.Any())
            {
                return Json(new { success = false, message = "No asset tags provided" });
            }

            var results = new List<object>();
            var successCount = 0;
            var failCount = 0;

            foreach (var assetTag in assetTags)
            {
                var success = await _lifecycleService.TransitionToState(
                    assetTag, 
                    AssetLifecycleState.InStorage, 
                    User.Identity?.Name ?? "Unknown"
                );
                
                if (success)
                {
                    successCount++;
                    results.Add(new { assetTag, success = true });
                }
                else
                {
                    failCount++;
                    results.Add(new { assetTag, success = false, message = "Failed to move to storage" });
                }
            }

            return Json(new { 
                success = true, 
                message = $"Moved {successCount} assets to storage, {failCount} failed",
                results 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk move to storage");
            return Json(new { success = false, message = "Error: " + ex.Message });
        }
    }

    // GET: Assets/GetFilterValues
    [HttpGet]
    [Authorize(Roles = "Admin,IT")]
    public async Task<IActionResult> GetFilterValues(string column)
    {
        try
        {
            var values = column.ToLower() switch
            {
                "assettag" => await _context.Assets.Where(a => !string.IsNullOrEmpty(a.AssetTag)).Select(a => a.AssetTag!).Distinct().OrderBy(v => v).ToListAsync(),
                "serialnumber" => await _context.Assets.Where(a => !string.IsNullOrEmpty(a.SerialNumber)).Select(a => a.SerialNumber!).Distinct().OrderBy(v => v).ToListAsync(),
                "servicetag" => await _context.Assets.Where(a => !string.IsNullOrEmpty(a.ServiceTag)).Select(a => a.ServiceTag!).Distinct().OrderBy(v => v).ToListAsync(),
                "manufacturer" => await _context.Assets.Where(a => !string.IsNullOrEmpty(a.Manufacturer)).Select(a => a.Manufacturer!).Distinct().OrderBy(v => v).ToListAsync(),
                "model" => await _context.Assets.Where(a => !string.IsNullOrEmpty(a.Model)).Select(a => a.Model!).Distinct().OrderBy(v => v).ToListAsync(),
                "category" => await _context.Assets.Where(a => !string.IsNullOrEmpty(a.Category)).Select(a => a.Category!).Distinct().OrderBy(v => v).ToListAsync(),
                "netname" => await _context.Assets.Where(a => !string.IsNullOrEmpty(a.NetName)).Select(a => a.NetName!).Distinct().OrderBy(v => v).ToListAsync(),
                "assigneduser" => await _context.Assets.Where(a => !string.IsNullOrEmpty(a.AssignedUserName)).Select(a => a.AssignedUserName!).Distinct().OrderBy(v => v).ToListAsync(),
                "manager" => await _context.Assets.Where(a => !string.IsNullOrEmpty(a.Manager)).Select(a => a.Manager!).Distinct().OrderBy(v => v).ToListAsync(),
                "department" => await _context.Assets.Where(a => !string.IsNullOrEmpty(a.Department)).Select(a => a.Department!).Distinct().OrderBy(v => v).ToListAsync(),
                "location" => await _context.Assets.Where(a => !string.IsNullOrEmpty(a.Location)).Select(a => a.Location!).Distinct().OrderBy(v => v).ToListAsync(),
                "floor" => await _context.Assets.Where(a => !string.IsNullOrEmpty(a.Floor)).Select(a => a.Floor!).Distinct().OrderBy(v => v).ToListAsync(),
                "desk" => await _context.Assets.Where(a => !string.IsNullOrEmpty(a.Desk)).Select(a => a.Desk!).Distinct().OrderBy(v => v).ToListAsync(),
                "status" => await _context.Assets.Where(a => !string.IsNullOrEmpty(a.Status)).Select(a => a.Status!).Distinct().OrderBy(v => v).ToListAsync(),
                "vendor" => await _context.Assets.Where(a => !string.IsNullOrEmpty(a.Vendor)).Select(a => a.Vendor!).Distinct().OrderBy(v => v).ToListAsync(),
                "warrantyend" => await _context.Assets.Where(a => a.WarrantyEndDate.HasValue).Select(a => a.WarrantyEndDate.Value.ToString("yyyy-MM-dd")).Distinct().OrderBy(v => v).ToListAsync(),
                _ => new List<string>()
            };

            return Json(new { success = true, values = values });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filter values for column: {Column}", column);
            return Json(new { success = false, message = "Error getting filter values: " + ex.Message });
        }
    }
}
