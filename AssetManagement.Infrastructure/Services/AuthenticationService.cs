using AssetManagement.Domain.Entities;
using AssetManagement.Domain.Models;
using AssetManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AssetManagement.Infrastructure.Services;

public interface IAuthenticationService
{
    Task<AuthenticationResult> ProcessAzureAdUserAsync(ClaimsPrincipal principal, string ipAddress, string userAgent);
    Task<AzureAdUserInfo> ExtractUserInfoFromClaimsAsync(ClaimsPrincipal principal);
    Task LogAuthenticationEventAsync(AuditLogEntry entry);
    Task<ApplicationUser?> GetUserByEmailAsync(string email);
    Task<ApplicationUser> CreateOrUpdateUserAsync(AzureAdUserInfo userInfo);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AssetManagementDbContext _context;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        AssetManagementDbContext context,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    public async Task<AuthenticationResult> ProcessAzureAdUserAsync(ClaimsPrincipal principal, string ipAddress, string userAgent)
    {
        try
        {
            var userInfo = await ExtractUserInfoFromClaimsAsync(principal);
            
            if (string.IsNullOrEmpty(userInfo.Email))
            {
                _logger.LogWarning("Azure AD user has no email address");
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "User email not found in Azure AD claims"
                };
            }

            // Check if user exists in local database
            var user = await GetUserByEmailAsync(userInfo.Email);
            
            if (user == null)
            {
                // Create new user
                user = await CreateOrUpdateUserAsync(userInfo);
                _logger.LogInformation("Created new user from Azure AD: {Email}", userInfo.Email);
            }
            else
            {
                // Update existing user
                user = await CreateOrUpdateUserAsync(userInfo);
                _logger.LogInformation("Updated existing user from Azure AD: {Email}", userInfo.Email);
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Log successful authentication
            await LogAuthenticationEventAsync(new AuditLogEntry
            {
                UserId = user.Id,
                UserEmail = user.Email!,
                Action = "Login",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccess = true
            });

            return new AuthenticationResult
            {
                IsSuccess = true,
                User = user,
                RedirectUrl = "/Dashboard"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Azure AD user authentication");
            
            await LogAuthenticationEventAsync(new AuditLogEntry
            {
                UserId = "Unknown",
                UserEmail = principal.FindFirstValue(ClaimTypes.Email) ?? "Unknown",
                Action = "Login",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccess = false,
                ErrorDetails = ex.Message
            });

            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "Authentication processing failed"
            };
        }
    }

    public async Task<AzureAdUserInfo> ExtractUserInfoFromClaimsAsync(ClaimsPrincipal principal)
    {
        // Log all available claims for debugging
        var allClaims = principal.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
        _logger.LogInformation("Available claims: {Claims}", string.Join(", ", allClaims));

        var userInfo = new AzureAdUserInfo
        {
            Email = principal.FindFirstValue(ClaimTypes.Email) ?? 
                   principal.FindFirstValue("preferred_username") ?? 
                   principal.FindFirstValue("upn") ??
                   principal.FindFirstValue("email") ??
                   principal.FindFirstValue("unique_name"),
            DisplayName = principal.FindFirstValue(ClaimTypes.Name) ??
                         principal.FindFirstValue("name") ??
                         principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"),
            FirstName = principal.FindFirstValue(ClaimTypes.GivenName) ??
                       principal.FindFirstValue("given_name") ??
                       principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname"),
            LastName = principal.FindFirstValue(ClaimTypes.Surname) ??
                      principal.FindFirstValue("family_name") ??
                      principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname"),
            Department = principal.FindFirstValue("department"),
            JobTitle = principal.FindFirstValue("jobTitle") ??
                      principal.FindFirstValue("job_title"),
            ObjectId = principal.FindFirstValue("oid") ??
                      principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier") ??
                      principal.FindFirstValue("sub"),
            TenantId = principal.FindFirstValue("tid") ??
                      principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/tenantid") ??
                      principal.FindFirstValue("tenant_id")
        };

        // Extract roles from claims
        var roleClaims = principal.FindAll(ClaimTypes.Role);
        userInfo.Roles = roleClaims.Select(c => c.Value).ToList();

        // Extract groups from claims (if available)
        var groupClaims = principal.FindAll("groups");
        userInfo.Groups = groupClaims.Select(c => c.Value).ToList();

        // Log extracted user info for debugging
        _logger.LogInformation("Extracted user info - Email: {Email}, Name: {Name}, ObjectId: {ObjectId}", 
            userInfo.Email, userInfo.DisplayName, userInfo.ObjectId);

        return userInfo;
    }

    public async Task LogAuthenticationEventAsync(AuditLogEntry entry)
    {
        try
        {
            // In a production environment, you might want to store this in a separate audit table
            _logger.LogInformation(
                "Authentication Event: User {UserEmail} performed {Action} from {IpAddress} - Success: {IsSuccess}",
                entry.UserEmail, entry.Action, entry.IpAddress, entry.IsSuccess);

            if (!entry.IsSuccess && !string.IsNullOrEmpty(entry.ErrorDetails))
            {
                _logger.LogWarning("Authentication failed: {ErrorDetails}", entry.ErrorDetails);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log authentication event");
        }
    }

    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<ApplicationUser> CreateOrUpdateUserAsync(AzureAdUserInfo userInfo)
    {
        var user = await GetUserByEmailAsync(userInfo.Email!);
        
        if (user == null)
        {
            // Create new user
            user = new ApplicationUser
            {
                UserName = userInfo.Email,
                Email = userInfo.Email,
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                Department = userInfo.Department,
                Title = userInfo.JobTitle,
                EmailConfirmed = true, // Azure AD users are pre-verified
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // Assign default role if no roles are provided
            if (!userInfo.Roles.Any())
            {
                await _userManager.AddToRoleAsync(user, "User");
            }
        }
        else
        {
            // Update existing user
            user.FirstName = userInfo.FirstName ?? user.FirstName;
            user.LastName = userInfo.LastName ?? user.LastName;
            user.Department = userInfo.Department ?? user.Department;
            user.Title = userInfo.JobTitle ?? user.Title;
            user.IsActive = true;

            await _userManager.UpdateAsync(user);
        }

        // Update roles based on Azure AD claims
        if (userInfo.Roles.Any())
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = userInfo.Roles.Except(currentRoles);
            var rolesToRemove = currentRoles.Except(userInfo.Roles);

            if (rolesToAdd.Any())
            {
                await _userManager.AddToRolesAsync(user, rolesToAdd);
            }

            if (rolesToRemove.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }
        }

        return user;
    }
}
