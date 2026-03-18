namespace WebSuDungDIen.Models
{
    public class ChiTietTaiKhoanVM
    {
        public ApplicationUser User { get; set; }
        public NhanVien? NhanVien { get; set; }
        public KhachHang? KhachHang { get; set; }
    }

}
