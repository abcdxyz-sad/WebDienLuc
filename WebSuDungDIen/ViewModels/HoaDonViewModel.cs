using System.ComponentModel.DataAnnotations;
namespace WebSuDungDIen.Models
{
    public class HoaDonViewModel
    {
        public int ChiSoCu { get; set; }
        public int ChiSoMoi { get; set; }

        [Required(ErrorMessage = "[ LỖI ] - Không được bỏ trống một bậc đơn giá điện nào")]
        public decimal Gia1 { get; set; }
        [Required(ErrorMessage = "[ LỖI ] - Không được bỏ trống một bậc đơn giá điện nào")]

        public decimal Gia2 { get; set; }
        [Required(ErrorMessage = "[ LỖI ] - Không được bỏ trống một bậc đơn giá điện nào")]

        public decimal Gia3 { get; set; }
        [Required(ErrorMessage = "[ LỖI ] - Không được bỏ trống một bậc đơn giá điện nào")]

        public decimal Gia4 { get; set; }
        [Required(ErrorMessage = "[ LỖI ] - Không được bỏ trống một bậc đơn giá điện nào")]

        public decimal Gia5 { get; set; }
        [Required(ErrorMessage = "[ LỖI ] - Không được bỏ trống một bậc đơn giá điện nào")]

        public decimal Gia6 { get; set; }

        [Required(ErrorMessage = "[ LỖI ] - Vui lòng nhập vào thuế!")]
        public decimal PhanTramVAT { get; set; } // nhập 8 hoặc 10
    }

}
