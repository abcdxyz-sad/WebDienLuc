using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebSuDungDIen.Data.Migrations
{
    /// <inheritdoc />
    public partial class CapNhatEFCoreChoPhepIdentityNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KhachHang_IdentityUserId",
                table: "KhachHang");

            migrationBuilder.AlterColumn<string>(
                name: "IdentityUserId",
                table: "KhachHang",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "NhanVienId",
                table: "ChiSoDien",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_KhachHang_IdentityUserId",
                table: "KhachHang",
                column: "IdentityUserId",
                unique: true,
                filter: "[IdentityUserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KhachHang_IdentityUserId",
                table: "KhachHang");

            migrationBuilder.AlterColumn<string>(
                name: "IdentityUserId",
                table: "KhachHang",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NhanVienId",
                table: "ChiSoDien",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KhachHang_IdentityUserId",
                table: "KhachHang",
                column: "IdentityUserId",
                unique: true);
        }
    }
}
