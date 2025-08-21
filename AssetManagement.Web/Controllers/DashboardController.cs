using AssetManagement.Domain.Entities;
using AssetManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly AssetManagementDbContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(AssetManagementDbContext context, ILogger<DashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        // Simple dashboard without complex models for now
        return View();
    }

    // GET: Dashboard/Assets
    public async Task<IActionResult> Assets()
    {
        var assets = await _context.Assets
            .Include(a => a.Building)
            .Include(a => a.Floor)
            .Include(a => a.AssignedUser)
            .OrderBy(a => a.AssetTag)
            .Take(50)
            .ToListAsync();

        return View(assets);
    }

    // GET: Dashboard/Buildings
    public async Task<IActionResult> Buildings()
    {
        var buildings = await _context.Buildings
            .Include(b => b.Floors)
            .OrderBy(b => b.Name)
            .ToListAsync();

        return View(buildings);
    }

    // GET: Dashboard/Reports
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Reports()
    {
        // Simple reports view for now
        return View();
    }
}
