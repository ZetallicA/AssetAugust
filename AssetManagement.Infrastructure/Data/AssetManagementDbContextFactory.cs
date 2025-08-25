using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AssetManagement.Infrastructure.Data;

public class AssetManagementDbContextFactory : IDesignTimeDbContextFactory<AssetManagementDbContext>
{
    public AssetManagementDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AssetManagementDbContext>();
        
        // Use a default connection string for design-time
        optionsBuilder.UseSqlServer("Server=192.168.8.229;Database=AssetManagement;User ID=sa;Password=MSPress#1;TrustServerCertificate=True;MultipleActiveResultSets=True");
        
        return new AssetManagementDbContext(optionsBuilder.Options);
    }
}
