using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebSuDungDIen.Models
{
    public class SuspiciousLoginLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Username { get; set; }

        public string IpAddress { get; set; }

        public string DeviceInfo { get; set; }

        public string WarningType { get; set; } // Ghi chú: "Địa chỉ IP lạ", "Thiết bị mới"...

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    }
}
