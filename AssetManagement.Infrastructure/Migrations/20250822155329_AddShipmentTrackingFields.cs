using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Carrier",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeliveredAt",
                table: "Assets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveredBy",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationSite",
                table: "Assets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PickedUpAt",
                table: "Assets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickedUpBy",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReadyForPickupAt",
                table: "Assets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReadyForPickupBy",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingNumber",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Carrier",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DeliveredBy",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DestinationSite",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "PickedUpAt",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "PickedUpBy",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ReadyForPickupAt",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ReadyForPickupBy",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "TrackingNumber",
                table: "Assets");
        }
    }
}
