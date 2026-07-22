using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProhvatApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVehicleLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartReviews_VehicleLogs_LogId",
                table: "PartReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleLogs_Vehicles_VehicleId",
                table: "VehicleLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VehicleLogs",
                table: "VehicleLogs");

            migrationBuilder.DropIndex(
                name: "IX_VehicleLogs_VehicleId",
                table: "VehicleLogs");

            migrationBuilder.RenameTable(
                name: "VehicleLogs",
                newName: "VehicleLog");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VehicleLog",
                table: "VehicleLog",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartReviews_VehicleLog_LogId",
                table: "PartReviews",
                column: "LogId",
                principalTable: "VehicleLog",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartReviews_VehicleLog_LogId",
                table: "PartReviews");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VehicleLog",
                table: "VehicleLog");

            migrationBuilder.RenameTable(
                name: "VehicleLog",
                newName: "VehicleLogs");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VehicleLogs",
                table: "VehicleLogs",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLogs_VehicleId",
                table: "VehicleLogs",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_PartReviews_VehicleLogs_LogId",
                table: "PartReviews",
                column: "LogId",
                principalTable: "VehicleLogs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleLogs_Vehicles_VehicleId",
                table: "VehicleLogs",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
