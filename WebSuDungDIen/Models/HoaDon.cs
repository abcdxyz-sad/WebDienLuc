using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace WebSuDungDIen.Models
{
    public class HoaDon
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string MaHd { get; set; } = null!;
        public string ChiSoDienId { get; set; } = null!;

        public string KhachHangId { get; set; } = null!;

        public string NhanVienId { get; set; } = null!;

        public int DonGiaId { get; set; }

        public int SoDienTieuThu { get; set; }

        public decimal TienDien { get; set; }

        public decimal PhanTramVAT { get; set; }

        public decimal ThueVAT { get; set; }

        public decimal TongThanhToan { get; set; }

        public string TrangThai { get; set; } = "ChuaThanhToan";
        public string? HinhThucThanhToan { get; set; }
        public DateTime? NgayThanhToan { get; set; }
        public DateTime? NgayLap { get; set; }
    }

}
