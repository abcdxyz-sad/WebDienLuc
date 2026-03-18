using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebSuDungDIen.Models
{
    public class DonGiaDien
    {
        public int Id { get; set; }
        public int Bac { get; set; } 
        public decimal Gia { get; set; }
        public DateTime NgayTao { get; set; }
    }
}
