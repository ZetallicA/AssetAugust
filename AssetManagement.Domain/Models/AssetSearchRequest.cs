using AssetManagement.Domain.Entities;

namespace AssetManagement.Domain.Models
{
    public sealed class AssetSearchRequest
    {
        public string? Query { get; set; }
        public string? Category { get; set; }
        public string? Location { get; set; }
        public string? Floor { get; set; }
        public string? Status { get; set; }
        public string? LifecycleState { get; set; }
        public string? Vendor { get; set; }
        public bool UnassignedOnly { get; set; }
        public DateTimeOffset? CreatedFrom { get; set; }
        public DateTimeOffset? CreatedTo { get; set; }
        public DateTimeOffset? WarrantyFrom { get; set; }
        public DateTimeOffset? WarrantyTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
    }

    public sealed class AssetSearchResult
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
        public List<AssetSearchItem> Items { get; set; } = new();
        public long SearchTimeMs { get; set; }
        public bool UsedFullTextSearch { get; set; }
    }

    public sealed class AssetSearchItem
    {
        public int Id { get; set; }
        public string? AssetTag { get; set; }
        public string? ServiceTag { get; set; }
        public string? Category { get; set; }
        public string? NetName { get; set; }
        public string? Desk { get; set; }
        public string? WallPort { get; set; }
        public string? Location { get; set; }
        public string? Floor { get; set; }
        public string? Status { get; set; }
        public string? AssignedUserName { get; set; }
        public string? Department { get; set; }
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public string? IpAddress { get; set; }
        public string? MacAddress { get; set; }
        public string? SwitchName { get; set; }
        public string? SwitchPort { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Extension { get; set; }
        public string? Imei { get; set; }
        public string? CardNumber { get; set; }
        public string? OsVersion { get; set; }
        public string? License1 { get; set; }
        public string? License2 { get; set; }
        public string? License3 { get; set; }
        public string? License4 { get; set; }
        public string? License5 { get; set; }
        public string? Vendor { get; set; }
        public string? OrderNumber { get; set; }
        public string? Notes { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? WarrantyStart { get; set; }
        public DateTimeOffset? WarrantyEnd { get; set; }
        public AssetLifecycleState LifecycleState { get; set; }
        public Dictionary<string, string> Highlights { get; set; } = new();
    }
}

