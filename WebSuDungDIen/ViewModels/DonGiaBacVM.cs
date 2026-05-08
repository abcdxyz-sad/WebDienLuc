using System.ComponentModel.DataAnnotations;
namespace WebSuDungDIen.ViewModels
{
    public class DonGiaBacVM
    {
        public int Bac { get; set; }
        [Required(ErrorMessage = "[ LỖI ] - Không được bỏ trống một bậc đơn giá điện nào")]
        public decimal Gia { get; set; }
        public int GioiHan { get; set; }
    }
    
}
