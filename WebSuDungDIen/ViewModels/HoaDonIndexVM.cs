namespace WebSuDungDIen.ViewModels
{
    public class HoaDonIndexVM
    {
        public string Id { get; set; } = null!;
        public string MaHd { get; set; } = null!;
        public string TenKhachHang { get; set; } = null!;
        public string DiaChi { get; set; } = null!;
        public int Thang { get; set; }
        public int Nam { get; set; }
        public int SoDienTieuThu { get; set; }
        public decimal TongThanhToan { get; set; }
        public bool TrangThai { get; set; }
        public DateTime? NgayThanhToan { get; set; }
    }
}
