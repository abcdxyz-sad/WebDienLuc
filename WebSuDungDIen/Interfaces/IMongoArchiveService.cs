using MongoDB.Bson;

namespace WebSuDungDIen.Services
{
    public interface IMongoArchiveService
    {
        Task ArchiveAsync<TEntity>(TEntity data, string deletedBy, string reason) where TEntity : class;

        Task<TEntity> GetArchivedDataAsync<TEntity>(string archiveId) where TEntity : class;
        Task RemoveFromArchiveAsync<TEntity>(string archiveId) where TEntity : class;
        Task<List<BsonDocument>> GetArchivedListAsync(string type);
    }
}
