using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLifecycleAndTransferSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentDesk",
                table: "Assets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentSite",
                table: "Assets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentStorageLocation",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeployedAt",
                table: "Assets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeployedBy",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeployedToEmail",
                table: "Assets",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeployedToUser",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LifecycleState",
                table: "Assets",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SalvageBatchId",
                table: "Assets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Assets_AssetTag",
                table: "Assets",
                column: "AssetTag");

            migrationBuilder.CreateTable(
                name: "AssetEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetTag = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetEvents_Assets_AssetTag",
                        column: x => x.AssetTag,
                        principalTable: "Assets",
                        principalColumn: "AssetTag",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetTag = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FromSite = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ToSite = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FromStorageBin = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ToStorageBin = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Carrier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ShippedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReceivedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetTransfers_Assets_AssetTag",
                        column: x => x.AssetTag,
                        principalTable: "Assets",
                        principalColumn: "AssetTag",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalvageBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PickupVendor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PickedUpAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PickupManifestNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalvageBatches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CurrentSite",
                table: "Assets",
                column: "CurrentSite");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_LifecycleState",
                table: "Assets",
                column: "LifecycleState");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_SalvageBatchId",
                table: "Assets",
                column: "SalvageBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetEvents_AssetTag",
                table: "AssetEvents",
                column: "AssetTag");

            migrationBuilder.CreateIndex(
                name: "IX_AssetEvents_AssetTag_CreatedAt",
                table: "AssetEvents",
                columns: new[] { "AssetTag", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetEvents_CreatedAt",
                table: "AssetEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AssetEvents_Type",
                table: "AssetEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransfers_AssetTag",
                table: "AssetTransfers",
                column: "AssetTag");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransfers_AssetTag_CreatedAt",
                table: "AssetTransfers",
                columns: new[] { "AssetTag", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransfers_CreatedAt",
                table: "AssetTransfers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransfers_State",
                table: "AssetTransfers",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransfers_TrackingNumber",
                table: "AssetTransfers",
                column: "TrackingNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SalvageBatches_BatchCode",
                table: "SalvageBatches",
                column: "BatchCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalvageBatches_CreatedAt",
                table: "SalvageBatches",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SalvageBatches_PickupVendor",
                table: "SalvageBatches",
                column: "PickupVendor");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_SalvageBatches_SalvageBatchId",
                table: "Assets",
                column: "SalvageBatchId",
                principalTable: "SalvageBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_SalvageBatches_SalvageBatchId",
                table: "Assets");

            migrationBuilder.DropTable(
                name: "AssetEvents");

            migrationBuilder.DropTable(
                name: "AssetTransfers");

            migrationBuilder.DropTable(
                name: "SalvageBatches");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Assets_AssetTag",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_CurrentSite",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_LifecycleState",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_SalvageBatchId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "CurrentDesk",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "CurrentSite",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "CurrentStorageLocation",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DeployedAt",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DeployedBy",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DeployedToEmail",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DeployedToUser",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "LifecycleState",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "SalvageBatchId",
                table: "Assets");
        }
    }
}
