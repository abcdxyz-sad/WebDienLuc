using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace WebSuDungDIen.Models
{
    public class KhachHang
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string? MaKh { get; set; } = null!;
        public string? TenKh { get; set; } = null!;

        public string DiaChi { get; set; } = null!;
        public string DienThoai { get; set; } = null!;

        public string MaPhuongApi { get; set; }

        public string DiaChiDayDu { get; set; }

        public string? IdentityUserId { get; set; } = null!;
        public ApplicationUser? User { get; set; } = null!;
        public bool TrangThai { get; set; } = true;
        public List<ChiSoDien>? ChiSoDien { get; set; }
    }
}
