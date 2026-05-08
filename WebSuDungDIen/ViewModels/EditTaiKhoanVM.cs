using System.ComponentModel.DataAnnotations;

namespace WebSuDungDIen.Models
{
    public class EditTaiKhoanVM
    {
        public string Id { get; set; }
        [Required(ErrorMessage = "[ LỖI ] - Không được bỏ trống Email!")]
        public string Email { get; set; }
        [Required(ErrorMessage = "[ LỖI ] - Vui lòng nhập Tên Đăng Nhập!")]
        public string UserName { get; set; }
    }
}
