using AssetManagement.Domain.Entities;
using AssetManagement.Domain.Models;
using AssetManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AssetManagement.Web.Controllers;

public class AccountController : Controller
{
    private readonly AssetManagement.Infrastructure.Services.IAuthenticationService _authService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        AssetManagement.Infrastructure.Services.IAuthenticationService authService,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountController> logger)
    {
        _authService = authService;
        _userManager = userManager;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("Account/SignIn")]
    public IActionResult SignIn(string? returnUrl = "/")
    {
        var props = new AuthenticationProperties { RedirectUri = returnUrl ?? "/" };
        return Challenge(props, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpPost("Account/SignOut")]
    [ValidateAntiForgeryToken]
    public IActionResult SignOutPost()
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Content("~/Account/SignedOut")
        };
        return SignOut(props, OpenIdConnectDefaults.AuthenticationScheme, "Cookies");
    }

    [AllowAnonymous]
    [HttpGet("Account/SignedOut")]
    public IActionResult SignedOut() => View();

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        _logger.LogWarning("Access denied for user");
        return View();
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);
        
        // Create a view model with user info and roles
        var profileViewModel = new
        {
            User = user,
            Roles = roles,
            Claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList()
        };

        return View(profileViewModel);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> EditProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(ApplicationUser model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            // Update user information
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Department = model.Department;
            user.Title = model.Title;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Profile));
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
        }

        return View(user);
    }
}
