using MongoDB.Driver;
using WebSuDungDIen.Models;

namespace WebSuDungDIen.Services
{
    public class KhachHangRepository
    {
        private readonly IMongoCollection<KhachHang> _khachHangCollection;

        public KhachHangRepository(IMongoClient mongoClient)
        {
            // 1. Kết nối tới database và collection
            var database = mongoClient.GetDatabase("TenDatabaseCuaBan");
            _khachHangCollection = database.GetCollection<KhachHang>("KhachHang");

            // 2. Định nghĩa các cột muốn đưa vào bộ lọc tìm kiếm văn bản
            // Ở đây ta chọn Tên, Số điện thoại và Mã khách hàng
            var indexKeys = Builders<KhachHang>.IndexKeys
                .Text(x => x.TenKh)
                .Text(x => x.DienThoai)
                .Text(x => x.MaKh);

            // 3. Cấu hình Index (đặt tên cho dễ quản lý)
            var indexOptions = new CreateIndexOptions { Name = "FullTextSearchIndex" };
            var indexModel = new CreateIndexModel<KhachHang>(indexKeys, indexOptions);

            // 4. Chạy lệnh tạo Index
            // Dùng Try-Catch để nếu Index đã tồn tại thì app vẫn chạy bình thường, không bị lỗi sập
            try
            {
                _khachHangCollection.Indexes.CreateOne(indexModel);
                // Bạn có thể dùng Console.WriteLine("Tạo Text Index thành công!"); để kiểm tra lúc debug
            }
            catch (MongoCommandException ex)
            {
                // Nếu lỗi là do Index đã tồn tại thì bỏ qua
                if (ex.CodeName != "IndexKeySpecsConflict") throw;
            }
        }
    }
}
