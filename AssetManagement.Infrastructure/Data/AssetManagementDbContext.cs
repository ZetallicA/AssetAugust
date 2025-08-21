using AssetManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Data;

public class AssetManagementDbContext : IdentityDbContext<ApplicationUser>
{
    public AssetManagementDbContext(DbContextOptions<AssetManagementDbContext> options) : base(options)
    {
    }

    public DbSet<Asset> Assets { get; set; }
    public DbSet<Building> Buildings { get; set; }
    public DbSet<Floor> Floors { get; set; }
    public DbSet<AssetRequest> AssetRequests { get; set; }
    public DbSet<AssetHistory> AssetHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Asset configuration
        builder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AssetTag).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.AssetTag).IsUnique();
            entity.HasIndex(e => e.SerialNumber);
            entity.HasIndex(e => e.ServiceTag);
            entity.HasIndex(e => e.IpAddress);
            entity.HasIndex(e => e.MacAddress);
            
            // Configure decimal properties
            entity.Property(e => e.PurchasePrice).HasColumnType("decimal(18,2)");
            
            // Relationships
            entity.HasOne(e => e.Building)
                .WithMany(e => e.Assets)
                .HasForeignKey(e => e.BuildingId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.FloorEntity)
                .WithMany(e => e.Assets)
                .HasForeignKey(e => e.FloorId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.AssignedUser)
                .WithMany(e => e.AssignedAssets)
                .HasForeignKey(e => e.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Building configuration
        builder.Entity<Building>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.BuildingCode).IsUnique();
        });

        // Floor configuration
        builder.Entity<Floor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            
            entity.HasOne(e => e.Building)
                .WithMany(e => e.Floors)
                .HasForeignKey(e => e.BuildingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AssetRequest configuration
        builder.Entity<AssetRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.RequestType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Priority).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            
            // Configure decimal properties
            entity.Property(e => e.EstimatedCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ActualCost).HasColumnType("decimal(18,2)");
            
            // Relationships
            entity.HasOne(e => e.Requester)
                .WithMany(e => e.SubmittedRequests)
                .HasForeignKey(e => e.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Approver)
                .WithMany(e => e.ApprovedRequests)
                .HasForeignKey(e => e.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.AssignedTo)
                .WithMany()
                .HasForeignKey(e => e.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Asset)
                .WithMany(e => e.AssetRequests)
                .HasForeignKey(e => e.AssetId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // AssetHistory configuration
        builder.Entity<AssetHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            
            // Relationships
            entity.HasOne(e => e.Asset)
                .WithMany(e => e.AssetHistory)
                .HasForeignKey(e => e.AssetId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.User)
                .WithMany(e => e.AssetHistory)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ApplicationUser configuration
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(100);
        });
    }
}
