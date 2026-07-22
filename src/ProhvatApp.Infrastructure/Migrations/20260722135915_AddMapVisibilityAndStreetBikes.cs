using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProhvatApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMapVisibilityAndStreetBikes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVisibleOnMap",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "VehicleCategories",
                columns: new[] { "Id", "Metric", "Name", "Season" },
                values: new object[] { new Guid("66666666-6666-6666-6666-666666666666"), 2, "Дорожный мотоцикл", 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "VehicleCategories",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"));

            migrationBuilder.DropColumn(
                name: "IsVisibleOnMap",
                table: "Users");
        }
    }
}
