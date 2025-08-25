using AssetManagement.Domain.Entities;
using AssetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AssetManagement.Infrastructure.Services;

public interface IAuditService
{
    Task LogActionAsync(string userId, string userEmail, string action, string entityType, string entityId, 
        object? oldValues = null, object? newValues = null, string? description = null, string? requestId = null, 
        string? ipAddress = null, string? userAgent = null);
    Task<List<AuditLog>> GetAuditLogsAsync(string? userId = null, string? entityType = null, string? entityId = null, 
        DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 50);
    Task<List<AuditLog>> GetUserAuditLogsAsync(string userId, int page = 1, int pageSize = 50);
    Task<List<AuditLog>> GetEntityAuditLogsAsync(string entityType, string entityId, int page = 1, int pageSize = 50);
}

public class AuditService : IAuditService
{
    private readonly AssetManagementDbContext _context;

    public AuditService(AssetManagementDbContext context)
    {
        _context = context;
    }

    public async Task LogActionAsync(string userId, string userEmail, string action, string entityType, string entityId, 
        object? oldValues = null, object? newValues = null, string? description = null, string? requestId = null,
        string? ipAddress = null, string? userAgent = null)
    {
        var details = new Dictionary<string, object?>
        {
            ["entityType"] = entityType,
            ["entityId"] = entityId,
            ["description"] = description,
            ["requestId"] = requestId,
            ["ipAddress"] = ipAddress,
            ["userAgent"] = userAgent
        };

        if (oldValues != null)
            details["oldValues"] = oldValues;

        if (newValues != null)
            details["newValues"] = newValues;

        var auditLog = new AuditLog
        {
            WhenUtc = DateTime.UtcNow,
            ActorUserId = userId,
            Action = action,
            Target = $"{entityType}:{entityId}",
            DetailsJson = JsonSerializer.Serialize(details, new JsonSerializerOptions 
            { 
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetAuditLogsAsync(string? userId = null, string? entityType = null, string? entityId = null, 
        DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 50)
    {
        var query = _context.AuditLogs
            .Include(al => al.ActorUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(al => al.ActorUserId == userId);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(al => al.Target != null && al.Target.StartsWith(entityType + ":"));

        if (!string.IsNullOrEmpty(entityId))
            query = query.Where(al => al.Target != null && al.Target.EndsWith(":" + entityId));

        if (fromDate.HasValue)
            query = query.Where(al => al.WhenUtc >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(al => al.WhenUtc <= toDate.Value);

        return await query
            .OrderByDescending(al => al.WhenUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetUserAuditLogsAsync(string userId, int page = 1, int pageSize = 50)
    {
        return await GetAuditLogsAsync(userId: userId, page: page, pageSize: pageSize);
    }

    public async Task<List<AuditLog>> GetEntityAuditLogsAsync(string entityType, string entityId, int page = 1, int pageSize = 50)
    {
        return await GetAuditLogsAsync(entityType: entityType, entityId: entityId, page: page, pageSize: pageSize);
    }
}
