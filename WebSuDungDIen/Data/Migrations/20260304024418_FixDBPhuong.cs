using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebSuDungDIen.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixDBPhuong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HoTen",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DonGiaDien",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Bac = table.Column<int>(type: "int", nullable: false),
                    Gia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonGiaDien", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NhanVien",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IdentityUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaNV = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenNV = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DienThoai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChucVu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhanVien", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NhanVien_AspNetUsers_IdentityUserId",
                        column: x => x.IdentityUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "KhachHang",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaKh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenKh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DienThoai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhuongId = table.Column<int>(type: "int", nullable: false),
                    IdentityUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhachHang", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KhachHang_AspNetUsers_IdentityUserId",
                        column: x => x.IdentityUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KhachHang_Phuong_PhuongId",
                        column: x => x.PhuongId,
                        principalTable: "Phuong",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiSoDien",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    KhachHangId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NhanVienId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Thang = table.Column<int>(type: "int", nullable: false),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    ChiSoCu = table.Column<int>(type: "int", nullable: false),
                    ChiSoMoi = table.Column<int>(type: "int", nullable: false),
                    KhachHangId1 = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiSoDien", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiSoDien_KhachHang_KhachHangId",
                        column: x => x.KhachHangId,
                        principalTable: "KhachHang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiSoDien_KhachHang_KhachHangId1",
                        column: x => x.KhachHangId1,
                        principalTable: "KhachHang",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChiSoDien_NhanVien_NhanVienId",
                        column: x => x.NhanVienId,
                        principalTable: "NhanVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HoaDon",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaHd = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChiSoDienId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    KhachHangId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NhanVienId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DonGiaId = table.Column<int>(type: "int", nullable: false),
                    SoDienTieuThu = table.Column<int>(type: "int", nullable: false),
                    TienDien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PhanTramVAT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ThueVAT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongThanhToan = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayThanhToan = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoaDon", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HoaDon_ChiSoDien_ChiSoDienId",
                        column: x => x.ChiSoDienId,
                        principalTable: "ChiSoDien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HoaDon_DonGiaDien_DonGiaId",
                        column: x => x.DonGiaId,
                        principalTable: "DonGiaDien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HoaDon_KhachHang_KhachHangId",
                        column: x => x.KhachHangId,
                        principalTable: "KhachHang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HoaDon_NhanVien_NhanVienId",
                        column: x => x.NhanVienId,
                        principalTable: "NhanVien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiSoDien_KhachHangId",
                table: "ChiSoDien",
                column: "KhachHangId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiSoDien_KhachHangId1",
                table: "ChiSoDien",
                column: "KhachHangId1");

            migrationBuilder.CreateIndex(
                name: "IX_ChiSoDien_NhanVienId",
                table: "ChiSoDien",
                column: "NhanVienId");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_ChiSoDienId",
                table: "HoaDon",
                column: "ChiSoDienId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_DonGiaId",
                table: "HoaDon",
                column: "DonGiaId");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_KhachHangId",
                table: "HoaDon",
                column: "KhachHangId");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_NhanVienId",
                table: "HoaDon",
                column: "NhanVienId");

            migrationBuilder.CreateIndex(
                name: "IX_KhachHang_IdentityUserId",
                table: "KhachHang",
                column: "IdentityUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KhachHang_PhuongId",
                table: "KhachHang",
                column: "PhuongId");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_IdentityUserId",
                table: "NhanVien",
                column: "IdentityUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HoaDon");

            migrationBuilder.DropTable(
                name: "ChiSoDien");

            migrationBuilder.DropTable(
                name: "DonGiaDien");

            migrationBuilder.DropTable(
                name: "KhachHang");

            migrationBuilder.DropTable(
                name: "NhanVien");

            migrationBuilder.DropTable(
                name: "Phuong");

            migrationBuilder.DropColumn(
                name: "HoTen",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AspNetUsers");
        }
    }
}
