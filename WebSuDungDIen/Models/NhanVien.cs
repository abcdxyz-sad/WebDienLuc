using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
namespace WebSuDungDIen.Models
{
    public class NhanVien
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string? IdentityUserId { get; set; }
        public ApplicationUser? User { get; set; }

        public string? MaNV { get; set; }

        [Required(ErrorMessage = "[ LỖI ] - Vui lòng nhập tên nhân viên!")]
        public string? TenNV { get; set; }

        [Required(ErrorMessage = "[ LỖI ] - Vui lòng nhập địa chỉ!")]
        public string DiaChi { get; set; } = string.Empty;

        [Required(ErrorMessage = "[ LỖI ] - Vui lòng nhập điện thoại!")]
        public string DienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "[ LỖI ] - Vui lòng chọn chức vụ cho nhân viên này!")]
        public string ChucVu { get; set; } = "NhanVien";

        public bool TrangThai { get; set; } = true;
    }
}
