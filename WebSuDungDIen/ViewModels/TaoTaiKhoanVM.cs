using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// Kế thừa IValidatableObject để mở khóa tính năng Check Validation Động
public class TaoTaiKhoanVM : IValidatableObject
{
    [Required(ErrorMessage = "[ LỖI ] - Vui lòng nhập Tên Đăng Nhập!")]
    public string UserName { get; set; }

    [Required(ErrorMessage = "[ LỖI ] - Không được bỏ trống Email!")]
    [EmailAddress(ErrorMessage = "[ LỖI ] - Email không đúng định dạng (vd: khachhang@domain.com)")]
    public string Email { get; set; }

    [Required(ErrorMessage = "[ LỖI ] - Vui lòng nhập Mật Khẩu!")]
    public string Password { get; set; }

    [Required(ErrorMessage = "[ LỖI ] - Vui lòng xác nhận lại Mật Khẩu!")]
    [Compare("Password", ErrorMessage = "[ LỖI ] - Mật khẩu xác nhận không khớp!")]
    public string ConfirmPassword { get; set; }

    [Required(ErrorMessage = "[ LỖI ] - Vui lòng chọn loại tài khoản")]
    public string LoaiTaiKhoan { get; set; }

    public string? KhachHangId { get; set; }

    public string? NhanVienId { get; set; }

    // ==========================================
    // ĐÃ GỠ BỎ [Required] Ở CÁC TRƯỜNG DƯỚI ĐÂY
    // ==========================================
    public string? TenNV { get; set; }
    public string? TenKh { get; set; }
    public string? DiaChi { get; set; }
    public string? DienThoai { get; set; }

    // ==========================================
    // HỆ THỐNG QUÉT LỖI ĐỘNG (Chạy ngầm khi Submit)
    // ==========================================
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // 1. NẾU LÀ NHÂN VIÊN -> Chỉ check Tên NV
        if (LoaiTaiKhoan == "NhanVien")
        {
            if (string.IsNullOrWhiteSpace(NhanVienId))
            {

                if (string.IsNullOrWhiteSpace(TenNV))
                {
                    yield return new ValidationResult("[ LỖI ] - Vui lòng nhập tên nhân viên!", new[] { nameof(TenNV) });
                }
            }
        }
        // 2. NẾU LÀ KHÁCH HÀNG -> Chỉ check Tên KH (và các thông tin khác)
        else if (LoaiTaiKhoan == "KhachHang")
        {
            // Bắt buộc nhập tên Khách nếu KHÔNG chọn Khách cũ (KhachHangId rỗng)
            if (string.IsNullOrWhiteSpace(KhachHangId))
            {
                if (string.IsNullOrWhiteSpace(TenKh))
                {
                    yield return new ValidationResult("[ LỖI ] - Vui lòng nhập tên khách hàng/đại diện!", new[] { nameof(TenKh) });
                }

                // Chú ý: Trong HTML của sếp chưa có ô input cho Địa Chỉ & Điện Thoại. 
                // Khi nào sếp vẽ thêm 2 ô đó vào HTML thì BỎ COMMENT 4 dòng dưới đây ra nhé!

                // if (string.IsNullOrWhiteSpace(DiaChi))
                //     yield return new ValidationResult("[ LỖI ] - Vui lòng nhập địa chỉ!", new[] { nameof(DiaChi) });

                // if (string.IsNullOrWhiteSpace(DienThoai))
                //     yield return new ValidationResult("[ LỖI ] - Vui lòng nhập số điện thoại!", new[] { nameof(DienThoai) });
            }
        }
    }
}