using AssetManagement.Domain.Entities;
using AssetManagement.Domain.Models;
using AssetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AssetManagement.Infrastructure.Services
{
    public interface IAssetSearchService
    {
        Task<AssetSearchResult> SearchAsync(AssetSearchRequest request);
        Task<bool> IsFullTextSearchAvailableAsync();
    }

    public class AssetSearchService : IAssetSearchService
    {
        private readonly AssetManagementDbContext _context;
        private readonly ILogger<AssetSearchService> _logger;
        private static bool? _ftsAvailable;

        public AssetSearchService(AssetManagementDbContext context, ILogger<AssetSearchService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsFullTextSearchAvailableAsync()
        {
            if (_ftsAvailable.HasValue)
                return _ftsAvailable.Value;

            try
            {
                var result = await _context.Database.SqlQueryRaw<bool>(
                    "SELECT CASE WHEN EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'AssetsFTC') THEN 1 ELSE 0 END")
                    .FirstOrDefaultAsync();
                
                _ftsAvailable = result;
                _logger.LogInformation("Full-text search availability: {Available}", _ftsAvailable);
                return _ftsAvailable.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking full-text search availability, falling back to LIKE search");
                _ftsAvailable = false;
                return false;
            }
        }

        public async Task<AssetSearchResult> SearchAsync(AssetSearchRequest request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                var query = _context.Assets.AsQueryable();
                var useFts = await IsFullTextSearchAvailableAsync();

                // Apply text search
                if (!string.IsNullOrWhiteSpace(request.Query))
                {
                    query = useFts 
                        ? ApplyFullTextSearch(query, request.Query)
                        : ApplyLikeSearch(query, request.Query);
                }

                // Apply filters
                query = ApplyFilters(query, request);

                // Get total count
                var total = await query.CountAsync();

                // Apply sorting
                query = ApplySorting(query, request);

                // Apply pagination
                var items = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(a => new AssetSearchItem
                    {
                        Id = a.Id,
                        AssetTag = a.AssetTag,
                        ServiceTag = a.ServiceTag,
                        Category = a.Category,
                        NetName = a.NetName,
                        Desk = a.Desk,
                        WallPort = a.WallPort,
                        Location = a.Location,
                        Floor = a.Floor,
                        Status = a.Status,
                        AssignedUserName = a.AssignedUserName,
                        Department = a.Department,
                        Manufacturer = a.Manufacturer,
                        Model = a.Model,
                        SerialNumber = a.SerialNumber,
                        IpAddress = a.IpAddress,
                        MacAddress = a.MacAddress,
                        SwitchName = a.SwitchName,
                        SwitchPort = a.SwitchPort,
                        PhoneNumber = a.PhoneNumber,
                        Extension = a.Extension,
                        Imei = a.Imei,
                        CardNumber = a.CardNumber,
                        OsVersion = a.OsVersion,
                        License1 = a.License1,
                        License2 = a.License2,
                        License3 = a.License3,
                        License4 = a.License4,
                        License5 = a.License5,
                        Vendor = a.Vendor,
                        OrderNumber = a.OrderNumber,
                        Notes = a.Notes,
                        CreatedAt = a.CreatedAt,
                        WarrantyStart = a.WarrantyStart,
                        WarrantyEnd = a.WarrantyEndDate,
                        LifecycleState = a.LifecycleState
                    })
                    .ToListAsync();

                // Apply highlighting
                if (!string.IsNullOrWhiteSpace(request.Query))
                {
                    foreach (var item in items)
                    {
                        item.Highlights = GenerateHighlights(item, request.Query);
                    }
                }

                stopwatch.Stop();

                return new AssetSearchResult
                {
                    Total = total,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    Items = items,
                    SearchTimeMs = stopwatch.ElapsedMilliseconds,
                    UsedFullTextSearch = useFts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during asset search");
                throw;
            }
        }

        private IQueryable<Asset> ApplyFullTextSearch(IQueryable<Asset> query, string searchTerm)
        {
            var cleanTerm = searchTerm.Trim();
            
            // Check for exact phrase (quoted text)
            if (cleanTerm.StartsWith("\"") && cleanTerm.EndsWith("\""))
            {
                var phrase = cleanTerm.Substring(1, cleanTerm.Length - 2);
                return query.Where(a => 
                    EF.Functions.Contains(a.AssetTag, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.SerialNumber, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.ServiceTag, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.NetName, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.AssignedUserName, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.AssignedUserEmail, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.Manager, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.Department, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.Unit, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.Location, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.Floor, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.Desk, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.Status, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.IpAddress, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.MacAddress, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.WallPort, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.SwitchName, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.SwitchPort, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.PhoneNumber, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.Extension, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.Imei, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.CardNumber, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.OsVersion, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.License1, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.License2, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.License3, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.License4, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.License5, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.Vendor, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.OrderNumber, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.Notes, $"\"{phrase}\"") ||
                    EF.Functions.Contains(a.LifecycleState.ToString(), $"\"{phrase}\"")
                );
            }

            // Build FTS query with inflectional forms and prefix matching
            var terms = cleanTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var ftsConditions = new List<string>();

            foreach (var term in terms)
            {
                var ftsCondition = $"(FORMSOF(INFLECTIONAL, \"{term}\") OR \"{term}*\")";
                ftsConditions.Add(ftsCondition);
            }

            var ftsQuery = string.Join(" AND ", ftsConditions);

            return query.Where(a => 
                EF.Functions.Contains(a.AssetTag, ftsQuery) ||
                EF.Functions.Contains(a.SerialNumber, ftsQuery) ||
                EF.Functions.Contains(a.ServiceTag, ftsQuery) ||
                EF.Functions.Contains(a.NetName, ftsQuery) ||
                EF.Functions.Contains(a.AssignedUserName, ftsQuery) ||
                EF.Functions.Contains(a.AssignedUserEmail, ftsQuery) ||
                EF.Functions.Contains(a.Manager, ftsQuery) ||
                EF.Functions.Contains(a.Department, ftsQuery) ||
                EF.Functions.Contains(a.Unit, ftsQuery) ||
                EF.Functions.Contains(a.Location, ftsQuery) ||
                EF.Functions.Contains(a.Floor, ftsQuery) ||
                EF.Functions.Contains(a.Desk, ftsQuery) ||
                EF.Functions.Contains(a.Status, ftsQuery) ||
                EF.Functions.Contains(a.IpAddress, ftsQuery) ||
                EF.Functions.Contains(a.MacAddress, ftsQuery) ||
                EF.Functions.Contains(a.WallPort, ftsQuery) ||
                EF.Functions.Contains(a.SwitchName, ftsQuery) ||
                EF.Functions.Contains(a.SwitchPort, ftsQuery) ||
                EF.Functions.Contains(a.PhoneNumber, ftsQuery) ||
                EF.Functions.Contains(a.Extension, ftsQuery) ||
                EF.Functions.Contains(a.Imei, ftsQuery) ||
                EF.Functions.Contains(a.CardNumber, ftsQuery) ||
                EF.Functions.Contains(a.OsVersion, ftsQuery) ||
                EF.Functions.Contains(a.License1, ftsQuery) ||
                EF.Functions.Contains(a.License2, ftsQuery) ||
                EF.Functions.Contains(a.License3, ftsQuery) ||
                EF.Functions.Contains(a.License4, ftsQuery) ||
                EF.Functions.Contains(a.License5, ftsQuery) ||
                EF.Functions.Contains(a.Vendor, ftsQuery) ||
                EF.Functions.Contains(a.OrderNumber, ftsQuery) ||
                EF.Functions.Contains(a.Notes, ftsQuery) ||
                EF.Functions.Contains(a.LifecycleState.ToString(), ftsQuery)
            );
        }

        private IQueryable<Asset> ApplyLikeSearch(IQueryable<Asset> query, string searchTerm)
        {
            var cleanTerm = searchTerm.Trim();
            
            // Check for exact phrase (quoted text)
            if (cleanTerm.StartsWith("\"") && cleanTerm.EndsWith("\""))
            {
                var phrase = cleanTerm.Substring(1, cleanTerm.Length - 2);
                return query.Where(a => 
                    (a.AssetTag != null && a.AssetTag.Contains(phrase)) ||
                    (a.SerialNumber != null && a.SerialNumber.Contains(phrase)) ||
                    (a.ServiceTag != null && a.ServiceTag.Contains(phrase)) ||
                    (a.NetName != null && a.NetName.Contains(phrase)) ||
                    (a.AssignedUserName != null && a.AssignedUserName.Contains(phrase)) ||
                    (a.AssignedUserEmail != null && a.AssignedUserEmail.Contains(phrase)) ||
                    (a.Manager != null && a.Manager.Contains(phrase)) ||
                    (a.Department != null && a.Department.Contains(phrase)) ||
                    (a.Unit != null && a.Unit.Contains(phrase)) ||
                    (a.Location != null && a.Location.Contains(phrase)) ||
                    (a.Floor != null && a.Floor.Contains(phrase)) ||
                    (a.Desk != null && a.Desk.Contains(phrase)) ||
                    (a.Status != null && a.Status.Contains(phrase)) ||
                    (a.IpAddress != null && a.IpAddress.Contains(phrase)) ||
                    (a.MacAddress != null && a.MacAddress.Contains(phrase)) ||
                    (a.WallPort != null && a.WallPort.Contains(phrase)) ||
                    (a.SwitchName != null && a.SwitchName.Contains(phrase)) ||
                    (a.SwitchPort != null && a.SwitchPort.Contains(phrase)) ||
                    (a.PhoneNumber != null && a.PhoneNumber.Contains(phrase)) ||
                    (a.Extension != null && a.Extension.Contains(phrase)) ||
                    (a.Imei != null && a.Imei.Contains(phrase)) ||
                    (a.CardNumber != null && a.CardNumber.Contains(phrase)) ||
                    (a.OsVersion != null && a.OsVersion.Contains(phrase)) ||
                    (a.License1 != null && a.License1.Contains(phrase)) ||
                    (a.License2 != null && a.License2.Contains(phrase)) ||
                    (a.License3 != null && a.License3.Contains(phrase)) ||
                    (a.License4 != null && a.License4.Contains(phrase)) ||
                    (a.License5 != null && a.License5.Contains(phrase)) ||
                    (a.Vendor != null && a.Vendor.Contains(phrase)) ||
                    (a.OrderNumber != null && a.OrderNumber.Contains(phrase)) ||
                    (a.Notes != null && a.Notes.Contains(phrase)) ||
                    (a.LifecycleState.ToString().Contains(phrase))
                );
            }

            // Split into terms and search each
            var terms = cleanTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var term in terms)
            {
                var termQuery = term;
                query = query.Where(a => 
                    (a.AssetTag != null && a.AssetTag.Contains(termQuery)) ||
                    (a.SerialNumber != null && a.SerialNumber.Contains(termQuery)) ||
                    (a.ServiceTag != null && a.ServiceTag.Contains(termQuery)) ||
                    (a.NetName != null && a.NetName.Contains(termQuery)) ||
                    (a.AssignedUserName != null && a.AssignedUserName.Contains(termQuery)) ||
                    (a.AssignedUserEmail != null && a.AssignedUserEmail.Contains(termQuery)) ||
                    (a.Manager != null && a.Manager.Contains(termQuery)) ||
                    (a.Department != null && a.Department.Contains(termQuery)) ||
                    (a.Unit != null && a.Unit.Contains(termQuery)) ||
                    (a.Location != null && a.Location.Contains(termQuery)) ||
                    (a.Floor != null && a.Floor.Contains(termQuery)) ||
                    (a.Desk != null && a.Desk.Contains(termQuery)) ||
                    (a.Status != null && a.Status.Contains(termQuery)) ||
                    (a.IpAddress != null && a.IpAddress.Contains(termQuery)) ||
                    (a.MacAddress != null && a.MacAddress.Contains(termQuery)) ||
                    (a.WallPort != null && a.WallPort.Contains(termQuery)) ||
                    (a.SwitchName != null && a.SwitchName.Contains(termQuery)) ||
                    (a.SwitchPort != null && a.SwitchPort.Contains(termQuery)) ||
                    (a.PhoneNumber != null && a.PhoneNumber.Contains(termQuery)) ||
                    (a.Extension != null && a.Extension.Contains(termQuery)) ||
                    (a.Imei != null && a.Imei.Contains(termQuery)) ||
                    (a.CardNumber != null && a.CardNumber.Contains(termQuery)) ||
                    (a.OsVersion != null && a.OsVersion.Contains(termQuery)) ||
                    (a.License1 != null && a.License1.Contains(termQuery)) ||
                    (a.License2 != null && a.License2.Contains(termQuery)) ||
                    (a.License3 != null && a.License3.Contains(termQuery)) ||
                    (a.License4 != null && a.License4.Contains(termQuery)) ||
                    (a.License5 != null && a.License5.Contains(termQuery)) ||
                    (a.Vendor != null && a.Vendor.Contains(termQuery)) ||
                    (a.OrderNumber != null && a.OrderNumber.Contains(termQuery)) ||
                    (a.Notes != null && a.Notes.Contains(termQuery)) ||
                    (a.LifecycleState.ToString().Contains(termQuery))
                );
            }

            return query;
        }

        private IQueryable<Asset> ApplyFilters(IQueryable<Asset> query, AssetSearchRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Category))
                query = query.Where(a => a.Category == request.Category);

            if (!string.IsNullOrWhiteSpace(request.Location))
                query = query.Where(a => a.Location == request.Location);

            if (!string.IsNullOrWhiteSpace(request.Floor))
                query = query.Where(a => a.Floor == request.Floor);

            if (!string.IsNullOrWhiteSpace(request.Status))
                query = query.Where(a => a.Status == request.Status);

            if (!string.IsNullOrWhiteSpace(request.LifecycleState))
                query = query.Where(a => a.LifecycleState.ToString() == request.LifecycleState);

            if (!string.IsNullOrWhiteSpace(request.Vendor))
                query = query.Where(a => a.Vendor == request.Vendor);

            if (request.UnassignedOnly)
                query = query.Where(a => string.IsNullOrWhiteSpace(a.AssignedUserName));

            if (request.CreatedFrom.HasValue)
                query = query.Where(a => a.CreatedAt >= request.CreatedFrom.Value);

            if (request.CreatedTo.HasValue)
                query = query.Where(a => a.CreatedAt <= request.CreatedTo.Value);

            if (request.WarrantyFrom.HasValue)
                query = query.Where(a => a.WarrantyStart >= request.WarrantyFrom.Value);

            if (request.WarrantyTo.HasValue)
                query = query.Where(a => a.WarrantyEndDate <= request.WarrantyTo.Value);

            return query;
        }

        private IQueryable<Asset> ApplySorting(IQueryable<Asset> query, AssetSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SortBy))
                return query.OrderBy(a => a.AssetTag);

            return request.SortBy.ToLower() switch
            {
                "assettag" => request.SortDescending ? query.OrderByDescending(a => a.AssetTag) : query.OrderBy(a => a.AssetTag),
                "servicetag" => request.SortDescending ? query.OrderByDescending(a => a.ServiceTag) : query.OrderBy(a => a.ServiceTag),
                "category" => request.SortDescending ? query.OrderByDescending(a => a.Category) : query.OrderBy(a => a.Category),
                "location" => request.SortDescending ? query.OrderByDescending(a => a.Location) : query.OrderBy(a => a.Location),
                "floor" => request.SortDescending ? query.OrderByDescending(a => a.Floor) : query.OrderBy(a => a.Floor),
                "status" => request.SortDescending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
                "assigneduser" => request.SortDescending ? query.OrderByDescending(a => a.AssignedUserName) : query.OrderBy(a => a.AssignedUserName),
                "department" => request.SortDescending ? query.OrderByDescending(a => a.Department) : query.OrderBy(a => a.Department),
                "createdat" => request.SortDescending ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt),
                _ => query.OrderBy(a => a.AssetTag)
            };
        }

        private Dictionary<string, string> GenerateHighlights(AssetSearchItem item, string searchTerm)
        {
            var highlights = new Dictionary<string, string>();
            var cleanTerm = searchTerm.Trim();
            
            // Remove quotes for highlighting
            if (cleanTerm.StartsWith("\"") && cleanTerm.EndsWith("\""))
                cleanTerm = cleanTerm.Substring(1, cleanTerm.Length - 2);

            var terms = cleanTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            // Highlight fields that are commonly searched
            var fieldsToHighlight = new[] { "AssetTag", "ServiceTag", "NetName", "Desk", "WallPort", "Location", "Floor", "Status", "AssignedUserName", "Department" };
            
            foreach (var field in fieldsToHighlight)
            {
                var value = GetPropertyValue(item, field);
                if (!string.IsNullOrEmpty(value))
                {
                    var highlighted = HighlightText(value, terms);
                    if (highlighted != value)
                    {
                        highlights[field] = highlighted;
                    }
                }
            }

            return highlights;
        }

        private string? GetPropertyValue(AssetSearchItem item, string propertyName)
        {
            return propertyName switch
            {
                "AssetTag" => item.AssetTag,
                "ServiceTag" => item.ServiceTag,
                "NetName" => item.NetName,
                "Desk" => item.Desk,
                "WallPort" => item.WallPort,
                "Location" => item.Location,
                "Floor" => item.Floor,
                "Status" => item.Status,
                "AssignedUserName" => item.AssignedUserName,
                "Department" => item.Department,
                _ => null
            };
        }

        private string HighlightText(string text, string[] terms)
        {
            var result = text;
            
            foreach (var term in terms)
            {
                var pattern = Regex.Escape(term);
                var replacement = $"<mark>{term}</mark>";
                result = Regex.Replace(result, pattern, replacement, RegexOptions.IgnoreCase);
            }
            
            return result;
        }
    }
}
