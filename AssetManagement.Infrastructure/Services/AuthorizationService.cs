using AssetManagement.Domain.Constants;
using AssetManagement.Domain.Entities;
using AssetManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Services;

public interface IAuthorizationService
{
    Task<bool> HasPermissionAsync(string userId, string permission);
    Task<bool> HasPermissionAsync(string userId, string permission, int? buildingId = null, int? floorId = null, string? unitId = null);
    Task<bool> HasRoleAsync(string userId, string role);
    Task<bool> HasRoleAsync(string userId, string role, int? buildingId = null, int? floorId = null, string? unitId = null);
    Task<string[]> GetUserPermissionsAsync(string userId);
    Task<string[]> GetUserRolesAsync(string userId);
    Task<string[]> GetUserGroupsAsync(string userId);
    Task<bool> IsInScopeAsync(string userId, int? buildingId = null, int? floorId = null, string? unitId = null);
}

public class AuthorizationService : IAuthorizationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AssetManagementDbContext _context;

    public AuthorizationService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        AssetManagementDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission)
    {
        // Check direct assignments first
        var directAssignment = await _context.Assignments
            .Include(a => a.Permission)
            .Where(a => a.SubjectType == "User" && a.SubjectId == userId && a.Permission.Code == permission)
            .FirstOrDefaultAsync();

        if (directAssignment != null)
            return true;

        // Check role-based permissions
        var userRoles = await GetUserRolesAsync(userId);
        
        foreach (var role in userRoles)
        {
            var rolePermission = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => rp.RoleId == role && rp.Permission.Code == permission)
                .FirstOrDefaultAsync();

            if (rolePermission != null)
                return true;
        }

        // Check group-based permissions
        var userGroups = await _context.UserGroups
            .Include(ug => ug.Group)
            .Where(ug => ug.UserId == userId)
            .ToListAsync();

        foreach (var userGroup in userGroups)
        {
            var groupAssignment = await _context.Assignments
                .Include(a => a.Permission)
                .Where(a => a.SubjectType == "Group" && a.SubjectId == userGroup.GroupId.ToString() && a.Permission.Code == permission)
                .FirstOrDefaultAsync();

            if (groupAssignment != null)
                return true;
        }
        
        return false;
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission, int? buildingId = null, int? floorId = null, string? unitId = null)
    {
        // First check if user has the permission globally
        if (await HasPermissionAsync(userId, permission))
        {
            // If no scope is specified, permission is granted
            if (!buildingId.HasValue && !floorId.HasValue && string.IsNullOrEmpty(unitId))
                return true;
            
            // Check if user has access to the specified scope
            return await IsInScopeAsync(userId, buildingId, floorId, unitId);
        }
        
        return false;
    }

    public async Task<bool> HasRoleAsync(string userId, string role)
    {
        return await _userManager.IsInRoleAsync(await _userManager.FindByIdAsync(userId)!, role);
    }

    public async Task<bool> HasRoleAsync(string userId, string role, int? buildingId = null, int? floorId = null, string? unitId = null)
    {
        // Check if user has the role globally
        if (await HasRoleAsync(userId, role))
        {
            // If no scope is specified, role is granted
            if (!buildingId.HasValue && !floorId.HasValue && string.IsNullOrEmpty(unitId))
                return true;
            
            // Check if user has access to the specified scope
            return await IsInScopeAsync(userId, buildingId, floorId, unitId);
        }
        
        return false;
    }

    public async Task<string[]> GetUserPermissionsAsync(string userId)
    {
        var permissions = new HashSet<string>();
        
        // Get direct assignments
        var directAssignments = await _context.Assignments
            .Include(a => a.Permission)
            .Where(a => a.SubjectType == "User" && a.SubjectId == userId)
            .ToListAsync();

        foreach (var assignment in directAssignments)
        {
            permissions.Add(assignment.Permission.Code);
        }

        // Get role-based permissions
        var userRoles = await GetUserRolesAsync(userId);
        
        foreach (var role in userRoles)
        {
            var rolePermissions = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => rp.RoleId == role)
                .ToListAsync();

            foreach (var rolePermission in rolePermissions)
            {
                permissions.Add(rolePermission.Permission.Code);
            }
        }

        // Get group-based permissions
        var userGroups = await _context.UserGroups
            .Include(ug => ug.Group)
            .Where(ug => ug.UserId == userId)
            .ToListAsync();

        foreach (var userGroup in userGroups)
        {
            var groupAssignments = await _context.Assignments
                .Include(a => a.Permission)
                .Where(a => a.SubjectType == "Group" && a.SubjectId == userGroup.GroupId.ToString())
                .ToListAsync();

            foreach (var assignment in groupAssignments)
            {
                permissions.Add(assignment.Permission.Code);
            }
        }
        
        return permissions.ToArray();
    }

    public async Task<string[]> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Array.Empty<string>();
            
        return (await _userManager.GetRolesAsync(user)).ToArray();
    }

    public async Task<string[]> GetUserGroupsAsync(string userId)
    {
        var userGroups = await _context.UserGroups
            .Include(ug => ug.Group)
            .Where(ug => ug.UserId == userId)
            .ToListAsync();

        return userGroups.Select(ug => ug.Group.Name).ToArray();
    }

    public async Task<bool> IsInScopeAsync(string userId, int? buildingId = null, int? floorId = null, string? unitId = null)
    {
        // SuperAdmin has access to everything
        if (await HasRoleAsync(userId, Roles.SuperAdmin))
            return true;
        
        // Check assignments with scope
        var assignments = await _context.Assignments
            .Where(a => a.SubjectType == "User" && a.SubjectId == userId)
            .ToListAsync();
        
        foreach (var assignment in assignments)
        {
            // If assignment has no scope restrictions, user has access
            if (string.IsNullOrEmpty(assignment.ScopeId))
                return true;
            
            // Check building scope
            if (buildingId.HasValue && assignment.ScopeType == "Building" && assignment.ScopeId == buildingId.ToString())
                return true;
            
            // Check floor scope
            if (floorId.HasValue && assignment.ScopeType == "Floor" && assignment.ScopeId == floorId.ToString())
                return true;
            
            // Check unit scope
            if (!string.IsNullOrEmpty(unitId) && assignment.ScopeType == "Unit" && assignment.ScopeId == unitId)
                return true;
        }
        
        return false;
    }
}
