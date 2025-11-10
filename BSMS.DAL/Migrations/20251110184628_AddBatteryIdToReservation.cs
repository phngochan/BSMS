using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BSMS.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddBatteryIdToReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BatteryId",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BatteryId",
                table: "Reservations",
                column: "BatteryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Batteries_BatteryId",
                table: "Reservations",
                column: "BatteryId",
                principalTable: "Batteries",
                principalColumn: "BatteryId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Batteries_BatteryId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_BatteryId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "BatteryId",
                table: "Reservations");
        }
    }
}
