using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BSMS.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftInfoToStationStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "StationStaffs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ShiftEnd",
                table: "StationStaffs",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ShiftStart",
                table: "StationStaffs",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "StationStaffs");

            migrationBuilder.DropColumn(
                name: "ShiftEnd",
                table: "StationStaffs");

            migrationBuilder.DropColumn(
                name: "ShiftStart",
                table: "StationStaffs");
        }
    }
}
