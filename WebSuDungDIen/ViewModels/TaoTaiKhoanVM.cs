public class TaoTaiKhoanVM
{
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string LoaiTaiKhoan { get; set; }

    // Nhân viên
    public string? TenNV { get; set; }

    // Khách hàng
    public string? TenKh { get; set; }
    public string? DiaChi { get; set; }
    public string? DienThoai { get; set; }
}
