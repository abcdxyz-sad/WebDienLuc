using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebSuDungDIen.Data.Migrations
{
    /// <inheritdoc />
    public partial class ThemNgayLapHD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NgayLap",
                table: "HoaDon",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NgayLap",
                table: "HoaDon");
        }
    }
}
