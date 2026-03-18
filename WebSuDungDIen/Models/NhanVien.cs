using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
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

        public string? TenNV { get; set; }
        public string DiaChi { get; set; } = string.Empty;
        public string DienThoai { get; set; } = string.Empty;

        public string ChucVu { get; set; } = "NhanVien";

        public bool TrangThai { get; set; } = true;
    }
}
