using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebSuDungDIen.Models
{
    public class ChiSoDien
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string KhachHangId { get; set; }
        public string? NhanVienId { get; set; }
        public int Thang { get; set; }
        public int Nam { get; set; }
        public int ChiSoCu { get; set; }

        [Required(ErrorMessage = "[ LỖI ] - Vui lòng nhập vào chỉ số điện!")]
        public int ChiSoMoi { get; set; }

        [ForeignKey("KhachHangId")]
        public virtual KhachHang? KhachHang { get; set; }
        [ForeignKey("NhanVienId")]
        public virtual NhanVien NhanVien { get; set; }
    }
}
