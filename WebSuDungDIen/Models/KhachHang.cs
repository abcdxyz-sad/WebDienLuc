using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
namespace WebSuDungDIen.Models
{
    public class KhachHang
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string? MaKh { get; set; } = null!;

        [Required(ErrorMessage = "[ LỖI ] - Vui lòng nhập tên khách hàng!")]
        public string? TenKh { get; set; } = null!;

        [Required(ErrorMessage = "[ LỖI ] - Vui lòng nhập địa chỉ của khách!")]
        public string DiaChi { get; set; } = null!;
        [Required(ErrorMessage = "[ LỖI ] - Vui lòng nhập vào số điện thoại!")]
        public string DienThoai { get; set; } = null!;

        [Required(ErrorMessage = "[ LỖI ] - Vui lòng chọn phường khách đang cư trú")]
        public string MaPhuongApi { get; set; }

        public string DiaChiDayDu { get; set; }

        public string? IdentityUserId { get; set; }
        public ApplicationUser? User { get; set; }
        public bool TrangThai { get; set; } = true;
        public List<ChiSoDien>? ChiSoDien { get; set; }
    }
}
