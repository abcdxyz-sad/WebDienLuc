namespace WebSuDungDIen.Models
{
    public class QuanLyTaiKhoanVM
    {
        public List<ApplicationUser> Admin { get; set; }
        public List<ApplicationUser> NhanVien { get; set; }
        public List<ApplicationUser> KhachHang { get; set; }
        public List<KhachHang> DanhSachKhach { get; set; }
        public List<NhanVien> DanhSachNhanVien { get; set; }

    }
}
