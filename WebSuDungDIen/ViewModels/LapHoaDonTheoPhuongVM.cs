using WebSuDungDIen.Models;

namespace WebSuDungDIen.ViewModels
{
    public class LapHoaDonTheoPhuongVM
    {
        public string? MaPhuongApi { get; set; }

        public List<KhachHang> DanhSachKhach { get; set; } = new();

        public List<DonGiaBacVM> DanhSachGia { get; set; } = new();

        public decimal PhanTramVAT { get; set; }
        public DateTime NgayLap { get; set; } = DateTime.Now;
    }
}
