using AssetManagement.Domain.Entities;

namespace AssetManagement.Web.Models;

public class ProfileViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public IEnumerable<string> Roles { get; set; } = new List<string>();
    public IEnumerable<ClaimInfo> Claims { get; set; } = new List<ClaimInfo>();
}

public class ClaimInfo
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
