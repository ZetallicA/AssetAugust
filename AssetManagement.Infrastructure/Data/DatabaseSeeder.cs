using AssetManagement.Domain.Entities;
using AssetManagement.Domain.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Infrastructure.Data;

public class DatabaseSeeder
{
    private readonly AssetManagementDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        AssetManagementDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
                    // Seed legacy roles (keep for backward compatibility)
        await SeedRolesAsync();

            // Seed admin user
            await SeedAdminUserAsync();

            // Seed sample buildings and floors
            await SeedBuildingsAndFloorsAsync();

            // Seed sample equipment categories
            await SeedEquipmentCategoriesAsync();

            // Add 9th floor to 66 John Street if it doesn't exist
            await Add9thFloorTo66JohnStreetAsync();

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during database seeding");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[] { "Admin", "IT", "Facilities", "Procurement", "Storage", "Manager", "User" };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
                _logger.LogInformation("Created role: {Role}", role);
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        var adminEmail = "admin@assetmanagement.com";
        var adminUser = await _userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                Department = "IT",
                Title = "System Administrator",
                EmailConfirmed = true,

                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(adminUser, "Admin123!");
            
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                _logger.LogInformation("Created admin user: {Email}", adminEmail);
            }
            else
            {
                _logger.LogError("Failed to create admin user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedBuildingsAndFloorsAsync()
    {
        // Check if 100 Church Street building exists, if not add it
        var churchStreetBuilding = await _context.Buildings.FirstOrDefaultAsync(b => b.BuildingCode == "100CHURCH");
        if (churchStreetBuilding == null)
        {
            // Add 100 Church Street building
            var newBuilding = new Building
            {
                Name = "100 Church Street",
                Address = "100 Church Street",
                City = "New York",
                State = "NY",
                ZipCode = "10007",
                BuildingCode = "100CHURCH",
                CreatedBy = "System"
            };

            await _context.Buildings.AddAsync(newBuilding);
            await _context.SaveChangesAsync();

            // Add the 12th floor
            var newFloor = new Floor
            {
                Name = "12th Floor",
                FloorNumber = "12",
                Description = "100 Church Street - 12th Floor",
                BuildingId = newBuilding.Id,
                CreatedBy = "System"
            };

            await _context.Floors.AddAsync(newFloor);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added 100 Church Street building with 12th floor");
        }

        // Only seed other buildings if none exist
        if (!await _context.Buildings.AnyAsync())
        {
            var buildings = new List<Building>
            {
                new Building
                {
                    Name = "100 Church Street",
                    Address = "100 Church Street",
                    City = "New York",
                    State = "NY",
                    ZipCode = "10007",
                    BuildingCode = "100CHURCH",
                    CreatedBy = "System"
                },
                new Building
                {
                    Name = "66 John Street",
                    Address = "66 John Street",
                    City = "New York",
                    State = "NY",
                    ZipCode = "10038",
                    BuildingCode = "66JOHN",
                    CreatedBy = "System"
                },
                new Building
                {
                    Name = "9 Bond Street",
                    Address = "9 Bond Street",
                    City = "Brooklyn",
                    State = "NY",
                    ZipCode = "11201",
                    BuildingCode = "BROOKLYN",
                    CreatedBy = "System"
                },
                new Building
                {
                    Name = "260 E. 161 Street",
                    Address = "260 E. 161 Street",
                    City = "Bronx",
                    State = "NY",
                    ZipCode = "10451",
                    BuildingCode = "BRONX",
                    CreatedBy = "System"
                },
                new Building
                {
                    Name = "31-00 47 Avenue",
                    Address = "31-00 47 Avenue",
                    City = "Long Island City",
                    State = "NY",
                    ZipCode = "11101",
                    BuildingCode = "LIC",
                    CreatedBy = "System"
                },
                new Building
                {
                    Name = "350 St. Marks Place",
                    Address = "350 St. Marks Place",
                    City = "Staten Island",
                    State = "NY",
                    ZipCode = "10301",
                    BuildingCode = "STATEN ISLAND",
                    CreatedBy = "System"
                }
            };

            await _context.Buildings.AddRangeAsync(buildings);
            await _context.SaveChangesAsync();

            // Add floors for each building based on actual configurations
            foreach (var building in buildings)
            {
                var floors = new List<Floor>();
                
                // Add floors based on actual building configurations
                var floorNumbers = building.BuildingCode switch
                {
                    "100CHURCH" => new[] { 12 }, // 100 Church Street - 12th Floor
                    "66JOHN" => new[] { 9, 10, 11 }, // 66 John Street - 9th, 10th and 11th Floors
                    "BROOKLYN" => new[] { 6, 7 }, // 9 Bond Street - 6th and 7th Floors
                    "BRONX" => new[] { 6 }, // 260 E. 161 Street - 6th Floor
                    "LIC" => new[] { 3, 4 }, // 31-00 47 Avenue - 3rd and 4th Floor
                    "STATEN ISLAND" => new[] { 1 }, // 350 St. Marks Place - Main Floor
                    _ => new[] { 1 }
                };

                foreach (var floorNumber in floorNumbers)
                {
                    floors.Add(new Floor
                    {
                        Name = floorNumber == 1 ? "Main Floor" : $"{floorNumber}th Floor",
                        FloorNumber = floorNumber.ToString(),
                        Description = $"{building.Name} - {(floorNumber == 1 ? "Main Floor" : $"{floorNumber}th Floor")}",
                        BuildingId = building.Id,
                        CreatedBy = "System"
                    });
                }

                await _context.Floors.AddRangeAsync(floors);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} buildings and their floors", buildings.Count);
        }
    }

    private async Task SeedEquipmentCategoriesAsync()
    {
        // This method can be used to seed equipment categories if needed
        // For now, we'll rely on the Excel import to populate categories
        _logger.LogInformation("Equipment categories will be populated through Excel imports");
    }

    private async Task Add9thFloorTo66JohnStreetAsync()
    {
        try
        {
            // Find the 66 John Street building
            var building = await _context.Buildings
                .Include(b => b.Floors)
                .FirstOrDefaultAsync(b => b.BuildingCode == "66JOHN");

            if (building == null)
            {
                _logger.LogWarning("66 John Street building not found!");
                return;
            }

            // Check if 9th floor already exists
            var existingFloor = building.Floors.FirstOrDefault(f => f.FloorNumber == "9");
            if (existingFloor != null)
            {
                _logger.LogInformation("9th Floor already exists for 66 John Street building");
                return;
            }

            // Add the 9th floor
            var newFloor = new Floor
            {
                Name = "9th Floor",
                FloorNumber = "9",
                Description = "66 John Street - 9th Floor",
                BuildingId = building.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            await _context.Floors.AddAsync(newFloor);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully added 9th Floor to 66 John Street building");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding 9th floor to 66 John Street building");
        }
    }

}
