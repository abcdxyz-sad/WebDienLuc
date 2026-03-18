using MongoDB.Driver;
using WebSuDungDIen.Models;
namespace WebSuDungDien.Services
{
    public class MongoService
    {
        private readonly IMongoDatabase _database;

        public MongoService(IConfiguration config)
        {
            var client = new MongoClient(config["MongoDB:ConnectionString"]);
            _database = client.GetDatabase(config["MongoDB:DatabaseName"]);
        }

        public IMongoCollection<SystemLog> Logs =>
            _database.GetCollection<SystemLog>("SystemLogs");
    }
}
