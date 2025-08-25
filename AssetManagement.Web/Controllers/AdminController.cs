using AssetManagement.Domain.Entities;
using AssetManagement.Infrastructure.Services;
using AssetManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Web.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly Infrastructure.Services.IAuthorizationService _authorizationService;
    private readonly AssetManagementDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        Infrastructure.Services.IAuthorizationService authorizationService,
        AssetManagementDbContext context,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _authorizationService = authorizationService;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> UserManagement()
    {
        // Debug: Log current user and roles
        var email = User.FindFirstValue(ClaimTypes.Email);
        var currentUser = !string.IsNullOrEmpty(email) ? await _userManager.FindByEmailAsync(email) : null;
        
        // Fallback to GetUserAsync if email lookup fails
        if (currentUser == null)
        {
            currentUser = await _userManager.GetUserAsync(User);
        }
        
        _logger.LogInformation("Current user: {User}", currentUser?.Email);
        _logger.LogInformation("Email from claims: {Email}", email);
        
        if (currentUser != null)
        {
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
            _logger.LogInformation("Current user roles: {Roles}", string.Join(", ", currentUserRoles));
        }

        var users = await _userManager.Users
            .Include(u => u.AssignedAssets)
            .ToListAsync();

        var userViewModels = new List<UserManagementViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var permissions = await _authorizationService.GetUserPermissionsAsync(user.Id);
            var groups = await _authorizationService.GetUserGroupsAsync(user.Id);

            userViewModels.Add(new UserManagementViewModel
            {
                User = user,
                Roles = roles.ToList(),
                Permissions = permissions.ToList(),
                Groups = groups.ToList()
            });
        }

        return View(userViewModels);
    }

    [HttpGet]
    public async Task<IActionResult> TestAccess()
    {
        // Try to find user by email from claims first (more reliable with Azure AD)
        var email = User.FindFirstValue(ClaimTypes.Email);
        var currentUser = !string.IsNullOrEmpty(email) ? await _userManager.FindByEmailAsync(email) : null;
        
        // Fallback to GetUserAsync if email lookup fails
        if (currentUser == null)
        {
            currentUser = await _userManager.GetUserAsync(User);
        }
        
        var roles = currentUser != null ? await _userManager.GetRolesAsync(currentUser) : new List<string>();
        
        return Json(new { 
            User = currentUser?.Email, 
            EmailFromClaims = email,
            Roles = roles.ToList(),
            IsAdmin = roles.Contains("Admin"),
            Claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> ListAllUsers()
    {
        var allUsers = await _userManager.Users
            .Select(u => new { 
                Email = u.Email, 
                FirstName = u.FirstName, 
                LastName = u.LastName, 
                Department = u.Department,
                Title = u.Title,
                IsActive = u.IsActive
            })
            .ToListAsync();
        
        return Json(new { 
            TotalUsers = allUsers.Count,
            Users = allUsers
        });
    }

    [HttpGet]
    public async Task<IActionResult> AssignRoles(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var userRoles = await _userManager.GetRolesAsync(user);
        var allRoles = await _roleManager.Roles.ToListAsync();

        var viewModel = new AssignRolesViewModel
        {
            User = user,
            CurrentRoles = userRoles.ToList(),
            AvailableRoles = allRoles.Select(r => r.Name).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRoles(string userId, List<string> selectedRoles)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        
        // Remove roles not in selection
        var rolesToRemove = currentRoles.Except(selectedRoles);
        if (rolesToRemove.Any())
        {
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
        }

        // Add new roles
        var rolesToAdd = selectedRoles.Except(currentRoles);
        if (rolesToAdd.Any())
        {
            await _userManager.AddToRolesAsync(user, rolesToAdd);
        }

        TempData["Success"] = $"Roles updated successfully for {user.FirstName} {user.LastName}";
        return RedirectToAction(nameof(UserManagement));
    }

    [HttpGet]
    public async Task<IActionResult> AssignPermissions(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var userPermissions = await _authorizationService.GetUserPermissionsAsync(user.Id);
        var allPermissions = await _context.Permissions.ToListAsync();
        var buildings = await _context.Buildings.ToListAsync();

        var viewModel = new AssignPermissionsViewModel
        {
            User = user,
            CurrentPermissions = userPermissions.ToList(),
            AvailablePermissions = allPermissions,
            Buildings = buildings
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignPermissions(string userId, List<PermissionAssignment> assignments)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        // Remove existing assignments for this user
        var existingAssignments = await _context.Assignments
            .Where(a => a.SubjectType == "User" && a.SubjectId == userId)
            .ToListAsync();
        
        _context.Assignments.RemoveRange(existingAssignments);

        // Add new assignments
        foreach (var assignment in assignments.Where(a => a.IsSelected))
        {
            var permission = await _context.Permissions.FindAsync(assignment.PermissionId);
            if (permission != null)
            {
                var newAssignment = new Assignment
                {
                    SubjectType = "User",
                    SubjectId = userId,
                    PermissionId = assignment.PermissionId,
                    ScopeType = assignment.ScopeType ?? "Global",
                    ScopeId = assignment.ScopeId,
                    ExpiresAtUtc = assignment.ExpiresAt
                };

                _context.Assignments.Add(newAssignment);
            }
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Permissions updated successfully for {user.FirstName} {user.LastName}";
        return RedirectToAction(nameof(UserManagement));
    }

    [HttpGet]
    public async Task<IActionResult> BulkRoleAssignment()
    {
        var users = await _userManager.Users.ToListAsync();
        var roles = await _roleManager.Roles.ToListAsync();

        var viewModel = new BulkRoleAssignmentViewModel
        {
            Users = users,
            AvailableRoles = roles.Select(r => r.Name).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkRoleAssignment(BulkRoleAssignmentViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var role = await _roleManager.FindByNameAsync(model.SelectedRole);
        if (role == null)
        {
            ModelState.AddModelError("SelectedRole", "Invalid role selected");
            return View(model);
        }

        int successCount = 0;
        foreach (var userId in model.SelectedUserIds)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                if (model.Action == "Add")
                {
                    if (!await _userManager.IsInRoleAsync(user, role.Name ?? ""))
                    {
                        await _userManager.AddToRoleAsync(user, role.Name ?? "");
                        successCount++;
                    }
                }
                else if (model.Action == "Remove")
                {
                    if (await _userManager.IsInRoleAsync(user, role.Name ?? ""))
                    {
                        await _userManager.RemoveFromRoleAsync(user, role.Name ?? "");
                        successCount++;
                    }
                }
            }
        }

        TempData["Success"] = $"Successfully {model.Action.ToLower()}ed role '{role.Name}' to {successCount} users";
        return RedirectToAction(nameof(UserManagement));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDepartment(string userId, string department)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        user.Department = department;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = $"Department updated successfully for {user.FirstName} {user.LastName}";
        return RedirectToAction(nameof(UserManagement));
    }

    [HttpGet]
    public IActionResult AddUser(string? email = null, string? firstName = null, string? lastName = null)
    {
        if (!string.IsNullOrEmpty(email) || !string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName))
        {
            var model = new AddUserViewModel
            {
                Email = email ?? "",
                FirstName = firstName ?? "",
                LastName = lastName ?? ""
            };
            return View(model);
        }
        
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddUser(AddUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "A user with this email already exists.");
                return View(model);
            }

            // Validate email domain (optional - can be configured)
            var allowedDomains = new[] { "oathone.com", "oathone.onmicrosoft.com" };
            var emailDomain = model.Email.Split('@').LastOrDefault()?.ToLower();
            if (!allowedDomains.Contains(emailDomain))
            {
                ModelState.AddModelError("Email", "Email must be from an authorized domain (oathone.com).");
                return View(model);
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Department = model.Department,
                Title = model.Title,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                // Assign default role if specified
                if (!string.IsNullOrEmpty(model.DefaultRole))
                {
                    await _userManager.AddToRoleAsync(user, model.DefaultRole);
                }

                TempData["Success"] = $"User {user.FirstName} {user.LastName} ({user.Email}) created successfully. They can now log in using their Azure AD credentials.";
                return RedirectToAction(nameof(UserManagement));
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Email}", model.Email);
            ModelState.AddModelError("", "An error occurred while creating the user. Please try again.");
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult SearchUsers()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SearchUsers(SearchUsersViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var searchResults = new List<AzureAdUserViewModel>();

            // Search in existing local users
            var existingUsers = await _userManager.Users
                .Where(u => (u.Email != null && u.Email.Contains(model.SearchTerm)) || 
                           (u.FirstName != null && u.FirstName.Contains(model.SearchTerm)) || 
                           (u.LastName != null && u.LastName.Contains(model.SearchTerm)) ||
                           (u.Department != null && u.Department.Contains(model.SearchTerm)) ||
                           (u.Title != null && u.Title.Contains(model.SearchTerm)))
                .Take(10)
                .ToListAsync();

            foreach (var user in existingUsers)
            {
                searchResults.Add(new AzureAdUserViewModel
                {
                    Email = user.Email ?? "",
                    FirstName = user.FirstName ?? "",
                    LastName = user.LastName ?? "",
                    Department = user.Department,
                    IsExistingUser = true,
                    LocalUserId = user.Id
                });
            }

            // If no existing users found, suggest creating the user manually
            if (!existingUsers.Any())
            {
                // Check if the search term looks like an email
                if (model.SearchTerm.Contains("@"))
                {
                    var emailDomain = model.SearchTerm.Split('@').LastOrDefault()?.ToLower();
                    var allowedDomains = new[] { "oathone.com", "oathone.onmicrosoft.com" };
                    
                    if (allowedDomains.Contains(emailDomain))
                    {
                        // Suggest creating this user manually
                        ViewBag.SuggestedEmail = model.SearchTerm;
                        ViewBag.SuggestedFirstName = model.SearchTerm.Split('@')[0].Split('.')[0];
                        ViewBag.SuggestedLastName = model.SearchTerm.Split('@')[0].Split('.').Length > 1 
                            ? model.SearchTerm.Split('@')[0].Split('.')[1] 
                            : "";
                    }
                }
            }

            ViewBag.SearchResults = searchResults;
            ViewBag.SearchTerm = model.SearchTerm;
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users with term: {SearchTerm}", model.SearchTerm);
            ModelState.AddModelError("", "An error occurred while searching. Please try again.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportUser(string email, string firstName, string lastName, string department, string defaultRole)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return Json(new { success = false, message = "User already exists in the system." });
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Department = department,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                // Assign default role if specified
                if (!string.IsNullOrEmpty(defaultRole))
                {
                    await _userManager.AddToRoleAsync(user, defaultRole);
                }

                return Json(new { success = true, message = $"User {firstName} {lastName} imported successfully." });
            }
            else
            {
                return Json(new { success = false, message = "Failed to create user: " + string.Join(", ", result.Errors.Select(e => e.Description)) });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing user {Email}", email);
            return Json(new { success = false, message = "An error occurred while importing the user." });
        }
    }
}

public class UserManagementViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public List<string> Groups { get; set; } = new();
}

public class AssignRolesViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public List<string> CurrentRoles { get; set; } = new();
    public List<string> AvailableRoles { get; set; } = new();
}

public class AssignPermissionsViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public List<string> CurrentPermissions { get; set; } = new();
    public List<Permission> AvailablePermissions { get; set; } = new();
    public List<Building> Buildings { get; set; } = new();
}

public class PermissionAssignment
{
    public int PermissionId { get; set; }
    public bool IsSelected { get; set; }
    public string? ScopeType { get; set; }
    public string? ScopeId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class BulkRoleAssignmentViewModel
{
    public List<ApplicationUser> Users { get; set; } = new();
    public List<string> SelectedUserIds { get; set; } = new();
    public string SelectedRole { get; set; } = string.Empty;
    public List<string> AvailableRoles { get; set; } = new();
    public string Action { get; set; } = "Add"; // Add or Remove
}

public class AddUserViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Department")]
    public string? Department { get; set; }

    [Display(Name = "Job Title")]
    public string? Title { get; set; }

    [Display(Name = "Default Role")]
    public string? DefaultRole { get; set; }
}

public class SearchUsersViewModel
{
    [Required]
    [Display(Name = "Search Term")]
    public string SearchTerm { get; set; } = string.Empty;
}

public class AzureAdUserViewModel
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Title { get; set; }
    public bool IsExistingUser { get; set; }
    public string? LocalUserId { get; set; }
}
