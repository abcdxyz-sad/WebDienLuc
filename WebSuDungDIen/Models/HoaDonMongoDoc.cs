namespace WebSuDungDIen.Models
{
    public class HoaDonMongoDoc
    {
        public string MaHd { get; set; }
        public DateTime NgayLap { get; set; }
        public decimal PhanTramVAT { get; set; }

        public decimal ThueVAT { get; set; }
        public decimal TongThanhToan { get; set; }
        public string TrangThai { get; set; }

        // Dữ liệu nhúng (Embedded Document) - Không cần tách bảng
        public KhachHangDoc KhachHang { get; set; }
        public ChiSoDienDoc ChiSoDien { get; set; }
    }

    public class KhachHangDoc
    {
        public string MaKh { get; set; }
        public string TenKh { get; set; }
        public string DienThoai { get; set; }
    }

    public class ChiSoDienDoc
    {
        public int ChiSoCu { get; set; }
        public int ChiSoMoi { get; set; }
        public int TieuThu { get; set; }
    }
}
