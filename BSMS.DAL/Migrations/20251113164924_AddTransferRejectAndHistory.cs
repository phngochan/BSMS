using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BSMS.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferRejectAndHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "BatteryTransfers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfirmedByUserId",
                table: "BatteryTransfers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "BatteryTransfers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BatteryTransfers_ConfirmedByUserId",
                table: "BatteryTransfers",
                column: "ConfirmedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BatteryTransfers_Users_ConfirmedByUserId",
                table: "BatteryTransfers",
                column: "ConfirmedByUserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BatteryTransfers_Users_ConfirmedByUserId",
                table: "BatteryTransfers");

            migrationBuilder.DropIndex(
                name: "IX_BatteryTransfers_ConfirmedByUserId",
                table: "BatteryTransfers");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "BatteryTransfers");

            migrationBuilder.DropColumn(
                name: "ConfirmedByUserId",
                table: "BatteryTransfers");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "BatteryTransfers");
        }
    }
}
