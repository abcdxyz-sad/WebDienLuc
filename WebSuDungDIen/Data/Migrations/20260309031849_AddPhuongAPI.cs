using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebSuDungDIen.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhuongAPI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KhachHang_Phuong_PhuongId",
                table: "KhachHang");

            migrationBuilder.DropTable(
                name: "Phuong");

            migrationBuilder.DropIndex(
                name: "IX_KhachHang_PhuongId",
                table: "KhachHang");

            migrationBuilder.DropColumn(
                name: "PhuongId",
                table: "KhachHang");

            migrationBuilder.AddColumn<string>(
                name: "DiaChiDayDu",
                table: "KhachHang",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaPhuongApi",
                table: "KhachHang",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiaChiDayDu",
                table: "KhachHang");

            migrationBuilder.DropColumn(
                name: "MaPhuongApi",
                table: "KhachHang");

            migrationBuilder.AddColumn<int>(
                name: "PhuongId",
                table: "KhachHang",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Phuong",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenPhuong = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Phuong", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KhachHang_PhuongId",
                table: "KhachHang",
                column: "PhuongId");

            migrationBuilder.AddForeignKey(
                name: "FK_KhachHang_Phuong_PhuongId",
                table: "KhachHang",
                column: "PhuongId",
                principalTable: "Phuong",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
