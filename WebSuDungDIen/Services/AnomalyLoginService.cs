using MongoDB.Driver;
using WebSuDungDIen.Models;

namespace WebSuDungDIen.Services
{
    public class AnomalyLoginService
    {
        private readonly IMongoCollection<SuspiciousLoginLog> _suspiciousLogs;
        private readonly IConfiguration _config; // Công cụ đọc appsettings.json

        public AnomalyLoginService(IMongoClient mongoClient, IConfiguration config)
        {
            _config = config;
            // Nhớ đổi tên database cho chuẩn của sếp nhé
            var database = mongoClient.GetDatabase("WebSuDungDienLogs");
            _suspiciousLogs = database.GetCollection<SuspiciousLoginLog>("SuspiciousLoginLogs");
        }

        public async Task CheckAndLogAnomalyAsync(string username, string currentIp, string currentDevice)
        {
            // 1. Kéo thông tin "Máy chính chủ" từ file config ra
            string masterIp = _config["MasterDeviceConfig:MasterIp"] ?? "";
            string masterUserAgent = _config["MasterDeviceConfig:MasterUserAgent"] ?? "";

            // 2. Đối chiếu với thông tin của người đang đăng nhập
            bool isNewIp = currentIp != masterIp;
            bool isNewDevice = currentDevice != masterUserAgent;

            // 3. Nếu lệch IP HOẶC lệch thiết bị -> Chốt đơn đưa vào diện tình nghi
            if (isNewIp || isNewDevice)
            {
                var warningTokens = new List<string>();
                if (isNewIp) warningTokens.Add("IP lạ (Khác Master IP)");
                if (isNewDevice) warningTokens.Add("Thiết bị lạ (Khác Master Device)");

                var anomalyLog = new SuspiciousLoginLog
                {
                    Username = username,
                    IpAddress = currentIp,
                    DeviceInfo = currentDevice,
                    WarningType = string.Join(" & ", warningTokens)
                };

                // Đẩy thẳng xuống MongoDB
                await _suspiciousLogs.InsertOneAsync(anomalyLog);
            }
        }
    }
}
