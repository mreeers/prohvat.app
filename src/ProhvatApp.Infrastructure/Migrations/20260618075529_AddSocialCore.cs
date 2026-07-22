using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProhvatApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Rides_RideId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_VehicleLogs_VehicleLogId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_RideId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_VehicleLogId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "RideId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "VehicleLogId",
                table: "Comments");

            migrationBuilder.AddColumn<Guid>(
                name: "TargetId",
                table: "Comments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "TargetType",
                table: "Comments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Likes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Likes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Likes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_TargetType_TargetId",
                table: "Comments",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Likes_TargetType_TargetId",
                table: "Likes",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Likes_UserId_TargetType_TargetId",
                table: "Likes",
                columns: new[] { "UserId", "TargetType", "TargetId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Likes");

            migrationBuilder.DropIndex(
                name: "IX_Comments_TargetType_TargetId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "TargetId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "TargetType",
                table: "Comments");

            migrationBuilder.AddColumn<Guid>(
                name: "RideId",
                table: "Comments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VehicleLogId",
                table: "Comments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comments_RideId",
                table: "Comments",
                column: "RideId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_VehicleLogId",
                table: "Comments",
                column: "VehicleLogId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Rides_RideId",
                table: "Comments",
                column: "RideId",
                principalTable: "Rides",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_VehicleLogs_VehicleLogId",
                table: "Comments",
                column: "VehicleLogId",
                principalTable: "VehicleLogs",
                principalColumn: "Id");
        }
    }
}
