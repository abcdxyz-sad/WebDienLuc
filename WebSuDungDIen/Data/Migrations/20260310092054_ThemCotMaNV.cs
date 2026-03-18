using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebSuDungDIen.Data.Migrations
{
    /// <inheritdoc />
    public partial class ThemCotMaNV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MaNV",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaNV",
                table: "AspNetUsers");
        }
    }
}
