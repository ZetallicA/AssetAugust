using System.Security.Claims;
using AssetManagement.Domain.Entities;

namespace AssetManagement.Domain.Models;

public class AzureAdUserInfo
{
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? ObjectId { get; set; }
    public string? TenantId { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Groups { get; set; } = new();
}

public class AuthenticationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public ApplicationUser? User { get; set; }
    public string? RedirectUrl { get; set; }
}

public class AuditLogEntry
{
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsSuccess { get; set; }
    public string? ErrorDetails { get; set; }
}
