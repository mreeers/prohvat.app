using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProhvatApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PartReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartName = table.Column<string>(type: "text", nullable: false),
                    VendorCode = table.Column<string>(type: "text", nullable: true),
                    MarketplaceLink = table.Column<string>(type: "text", nullable: true),
                    LogId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartReviews_VehicleCategories_VehicleCategoryId",
                        column: x => x.VehicleCategoryId,
                        principalTable: "VehicleCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartReviews_VehicleLogs_LogId",
                        column: x => x.LogId,
                        principalTable: "VehicleLogs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartReviews_LogId",
                table: "PartReviews",
                column: "LogId");

            migrationBuilder.CreateIndex(
                name: "IX_PartReviews_VehicleCategoryId",
                table: "PartReviews",
                column: "VehicleCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartReviews");
        }
    }
}
