using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
namespace WebSuDungDIen.Services
{
    public class MongoArchiveService : IMongoArchiveService
    {
        private readonly IMongoDatabase _database;

        public MongoArchiveService(IConfiguration config)
        {
            var client = new MongoClient(config["MongoDB:ConnectionString"]);
            _database = client.GetDatabase(config["MongoDB:DatabaseName"]);
        }

        public async Task ArchiveAsync<TEntity>(TEntity data, string deletedBy, string reason) where TEntity : class
        {
            // Tự động chọn Collection dựa trên tên của Class (VD: KhachHang -> Archived_KhachHang)
            string collectionName = "Archived_" + typeof(TEntity).Name;
            var collection = _database.GetCollection<BsonDocument>(collectionName);

            var archiveEntry = new BsonDocument
            {
                { "ArchiveId", Guid.NewGuid().ToString() },
                { "EntityName", typeof(TEntity).Name },
                { "Data", data.ToBsonDocument() }, // Đóng gói toàn bộ object
                { "DeletedAt", DateTime.Now },
                { "DeletedBy", deletedBy },
                { "Reason", reason }
            };

            await collection.InsertOneAsync(archiveEntry);
        }

        public async Task<TEntity> GetArchivedDataAsync<TEntity>(string archiveId) where TEntity : class
        {
            string collectionName = "Archived_" + typeof(TEntity).Name;
            var collection = _database.GetCollection<BsonDocument>(collectionName);

            var filter = Builders<BsonDocument>.Filter.Eq("ArchiveId", archiveId);
            var doc = await collection.Find(filter).FirstOrDefaultAsync();

            if (doc == null) return null;

            // Moi cái cục "Data" ra và dịch ngược nó lại thành Object KhachHang
            var originalDataBson = doc["Data"].AsBsonDocument;
            return BsonSerializer.Deserialize<TEntity>(originalDataBson);
        }

        // 2. Hàm dọn dẹp (đã phục hồi thì phải xóa khỏi kho lưu trữ)
        public async Task RemoveFromArchiveAsync<TEntity>(string archiveId) where TEntity : class
        {
            string collectionName = "Archived_" + typeof(TEntity).Name;
            var collection = _database.GetCollection<BsonDocument>(collectionName);

            var filter = Builders<BsonDocument>.Filter.Eq("ArchiveId", archiveId);
            await collection.DeleteOneAsync(filter);
        }

        // Hàm lấy danh sách đã xóa (kèm cả Metadata như ngày xóa, lý do)
        public async Task<List<BsonDocument>> GetArchivedListAsync(string type)
        {
            string collectionName = "Archived_" + type;
            var collection = _database.GetCollection<BsonDocument>(collectionName);

            // Sắp xếp theo ngày xóa mới nhất lên đầu
            return await collection.Find(new BsonDocument())
                                   .Sort(Builders<BsonDocument>.Sort.Descending("DeletedAt"))
                                   .ToListAsync();
        }
    }
}
