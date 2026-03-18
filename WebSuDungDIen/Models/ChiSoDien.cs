using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace WebSuDungDIen.Models
{
    public class ChiSoDien
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string KhachHangId { get; set; }
        public string NhanVienId { get; set; }

        public int Thang { get; set; }
        public int Nam { get; set; }

        public int ChiSoCu { get; set; }
        public int ChiSoMoi { get; set; }
        public KhachHang? KhachHang { get; set; }
    }
}
