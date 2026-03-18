using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebSuDungDIen.Models { 
    public class SystemLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Action { get; set; }
        public string User { get; set; }
        public string Role { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
