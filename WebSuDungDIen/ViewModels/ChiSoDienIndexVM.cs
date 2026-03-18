namespace WebSuDungDIen.Models
{
    public class ChiSoDienIndexVM
    {
        public string Id { get; set; }
        public string KhachHangId { get; set; }
        public string TenKh { get; set; } = null!;
        public string DiaChi { get; set; } = null!;
        public int ThangGanNhat { get; set; }
        public int NamGanNhat { get; set; }
        public int ChiSoCu { get; set; }
        public int ChiSoMoi { get; set; }
        public int DienTieuThu { get; set; }
    }
}
