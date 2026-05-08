using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebSuDungDIen.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDBCloud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KhachHang_AspNetUsers_IdentityUserId",
                table: "KhachHang");

            migrationBuilder.DropForeignKey(
                name: "FK_NhanVien_AspNetUsers_IdentityUserId",
                table: "NhanVien");

            migrationBuilder.DropIndex(
                name: "IX_NhanVien_IdentityUserId",
                table: "NhanVien");

            migrationBuilder.AlterColumn<string>(
                name: "IdentityUserId",
                table: "NhanVien",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_IdentityUserId",
                table: "NhanVien",
                column: "IdentityUserId",
                unique: true,
                filter: "[IdentityUserId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_KhachHang_AspNetUsers_IdentityUserId",
                table: "KhachHang",
                column: "IdentityUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NhanVien_AspNetUsers_IdentityUserId",
                table: "NhanVien",
                column: "IdentityUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KhachHang_AspNetUsers_IdentityUserId",
                table: "KhachHang");

            migrationBuilder.DropForeignKey(
                name: "FK_NhanVien_AspNetUsers_IdentityUserId",
                table: "NhanVien");

            migrationBuilder.DropIndex(
                name: "IX_NhanVien_IdentityUserId",
                table: "NhanVien");

            migrationBuilder.AlterColumn<string>(
                name: "IdentityUserId",
                table: "NhanVien",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_IdentityUserId",
                table: "NhanVien",
                column: "IdentityUserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_KhachHang_AspNetUsers_IdentityUserId",
                table: "KhachHang",
                column: "IdentityUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NhanVien_AspNetUsers_IdentityUserId",
                table: "NhanVien",
                column: "IdentityUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
