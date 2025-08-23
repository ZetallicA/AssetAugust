using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable full-text search if not already enabled
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'AssetsFTC')
                BEGIN
                    CREATE FULLTEXT CATALOG AssetsFTC AS DEFAULT;
                END
            ");

            // Create full-text index on Assets table
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    CREATE FULLTEXT INDEX ON dbo.Assets
                    (
                        AssetTag LANGUAGE 1033,
                        SerialNumber LANGUAGE 1033,
                        ServiceTag LANGUAGE 1033,
                        NetName LANGUAGE 1033,
                        AssignedUserName LANGUAGE 1033,
                        AssignedUserEmail LANGUAGE 1033,
                        Manager LANGUAGE 1033,
                        Department LANGUAGE 1033,
                        Unit LANGUAGE 1033,
                        Location LANGUAGE 1033,
                        Floor LANGUAGE 1033,
                        Desk LANGUAGE 1033,
                        Status LANGUAGE 1033,
                        IpAddress LANGUAGE 1033,
                        MacAddress LANGUAGE 1033,
                        WallPort LANGUAGE 1033,
                        SwitchName LANGUAGE 1033,
                        SwitchPort LANGUAGE 1033,
                        PhoneNumber LANGUAGE 1033,
                        Extension LANGUAGE 1033,
                        Imei LANGUAGE 1033,
                        CardNumber LANGUAGE 1033,
                        OsVersion LANGUAGE 1033,
                        License1 LANGUAGE 1033,
                        License2 LANGUAGE 1033,
                        License3 LANGUAGE 1033,
                        License4 LANGUAGE 1033,
                        License5 LANGUAGE 1033,
                        Vendor LANGUAGE 1033,
                        OrderNumber LANGUAGE 1033,
                        Notes LANGUAGE 1033
                    )
                    KEY INDEX PK_Assets ON AssetsFTC WITH CHANGE_TRACKING AUTO;
                END
            ");

            // Add nonclustered indexes for filtering
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Assets_Category' AND object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_Assets_Category ON dbo.Assets(Category);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Assets_Location_Floor' AND object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_Assets_Location_Floor ON dbo.Assets(Location, Floor);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Assets_Status' AND object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_Assets_Status ON dbo.Assets(Status);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Assets_AssignedUser' AND object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_Assets_AssignedUser ON dbo.Assets(AssignedUserName, AssignedUserEmail);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Assets_Vendor' AND object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_Assets_Vendor ON dbo.Assets(Vendor);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Assets_CreatedAt' AND object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_Assets_CreatedAt ON dbo.Assets(CreatedAt);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Assets_WarrantyDates' AND object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_Assets_WarrantyDates ON dbo.Assets(WarrantyStart, WarrantyEndDate);
                END
            ");

            // Add computed column for fallback search if FTS is not available
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'SearchBlob' AND object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    ALTER TABLE dbo.Assets ADD SearchBlob AS 
                        ISNULL(AssetTag, '') + ' ' +
                        ISNULL(SerialNumber, '') + ' ' +
                        ISNULL(ServiceTag, '') + ' ' +
                        ISNULL(NetName, '') + ' ' +
                        ISNULL(AssignedUserName, '') + ' ' +
                        ISNULL(AssignedUserEmail, '') + ' ' +
                        ISNULL(Manager, '') + ' ' +
                        ISNULL(Department, '') + ' ' +
                        ISNULL(Unit, '') + ' ' +
                        ISNULL(Location, '') + ' ' +
                        ISNULL(Floor, '') + ' ' +
                        ISNULL(Desk, '') + ' ' +
                        ISNULL(Status, '') + ' ' +
                        ISNULL(IpAddress, '') + ' ' +
                        ISNULL(MacAddress, '') + ' ' +
                        ISNULL(WallPort, '') + ' ' +
                        ISNULL(SwitchName, '') + ' ' +
                        ISNULL(SwitchPort, '') + ' ' +
                        ISNULL(PhoneNumber, '') + ' ' +
                        ISNULL(Extension, '') + ' ' +
                        ISNULL(Imei, '') + ' ' +
                        ISNULL(CardNumber, '') + ' ' +
                        ISNULL(OsVersion, '') + ' ' +
                        ISNULL(License1, '') + ' ' +
                        ISNULL(License2, '') + ' ' +
                        ISNULL(License3, '') + ' ' +
                        ISNULL(License4, '') + ' ' +
                        ISNULL(License5, '') + ' ' +
                        ISNULL(Vendor, '') + ' ' +
                        ISNULL(OrderNumber, '') + ' ' +
                        ISNULL(Notes, '')
                    PERSISTED;
                END
            ");

            // Add index on SearchBlob for fallback
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Assets_SearchBlob' AND object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_Assets_SearchBlob ON dbo.Assets(SearchBlob);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop full-text index
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    DROP FULLTEXT INDEX ON dbo.Assets;
                END
            ");

            // Drop full-text catalog
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'AssetsFTC')
                BEGIN
                    DROP FULLTEXT CATALOG AssetsFTC;
                END
            ");

            // Drop nonclustered indexes
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Assets_Category' AND object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    DROP INDEX IX_Assets_Category ON dbo.Assets;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Assets_Location_Floor' AND object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    DROP INDEX IX_Assets_Location_Floor ON dbo.Assets;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Assets_Status' AND object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    DROP INDEX IX_Assets_Status ON dbo.Assets;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Assets_AssignedUser' AND object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    DROP INDEX IX_Assets_AssignedUser ON dbo.Assets;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Assets_Vendor' AND object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    DROP INDEX IX_Assets_Vendor ON dbo.Assets;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Assets_CreatedAt' AND object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    DROP INDEX IX_Assets_CreatedAt ON dbo.Assets;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Assets_WarrantyDates' AND object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    DROP INDEX IX_Assets_WarrantyDates ON dbo.Assets;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Assets_SearchBlob' AND object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    DROP INDEX IX_Assets_SearchBlob ON dbo.Assets;
                END
            ");

            // Drop computed column
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE name = 'SearchBlob' AND object_id = OBJECT_ID('dbo.Assets'))
                BEGIN
                    ALTER TABLE dbo.Assets DROP COLUMN SearchBlob;
                END
            ");
        }
    }
}
