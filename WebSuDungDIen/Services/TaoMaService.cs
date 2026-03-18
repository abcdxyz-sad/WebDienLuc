using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using WebSuDungDIen.Data;
public class TaoMaService
{
    private static readonly Random _random = new Random();
    private readonly ApplicationDbContext _context; // Đừng quên inject DbContext vào nhé!
    private static readonly object _lockTaoMa = new object();

    // Inject DbContext để hàm bên dưới có thể query vào Database
    public TaoMaService(ApplicationDbContext context)
    {
        _context = context;
    }
    public string GenerateUniqueCode(
        string prefix,
        Func<string, bool> isExistFunc,
        int maxRetry = 50
    )
    {
        for (int i = 0; i < maxRetry; i++)
        {
            var letters = new string(
                Enumerable.Range(0, 3)
                .Select(_ => (char)_random.Next('A', 'Z' + 1))
                .ToArray()
            );

            var numbers = _random.Next(0, 1000).ToString("D3");

            var code = $"{prefix}{letters}{numbers}";

            if (!isExistFunc(code))
            {
                return code;
            }
        }

        throw new Exception("Không thể tạo mã không trùng sau nhiều lần thử ");
    }

    public async Task<string> TaoMaHopDongChuanAPIAsync(ApplicationDbContext context, string maMien, string maPhuongApi)
    {
        // Tiền tố: "PB-28864-"
        string prefix = $"{maMien}{maPhuongApi}";

        // KHÓA LUỒNG: Chỉ 1 người được vào sinh mã tại 1 thời điểm
        lock (_lockTaoMa)
        {
            // Dùng context truy vấn đồng bộ (Bỏ async/await đi)
            var lastCode = context.KhachHang
                .Where(k => k.MaKh.StartsWith(prefix))
                .OrderByDescending(k => k.MaKh)
                .Select(k => k.MaKh) // Chỉ lấy đúng cột MaKh cho nhẹ RAM
                .FirstOrDefault();

            int stt = 1;
            if (lastCode != null)
            {
                // Cắt chuỗi lấy phần đuôi
                string lastNumberStr = lastCode.Substring(prefix.Length);
                if (int.TryParse(lastNumberStr, out int lastNumber))
                {
                    stt = lastNumber + 1; // Tăng lên 1
                }
            }

            // Trả về kết quả 5 số cực đẹp: PB-28864-00001
            return $"{prefix}{stt:D5}";
        }
    }

    public string TaoMaHoaDon(string maKhachHang, int thang, int nam)
    {
        // Lấy 2 số cuối của năm. VD: 2026 -> "26"
        string namNgan = nam.ToString().Substring(2, 2);

        // Format tháng luôn có 2 số (D2). VD: tháng 3 -> "03"
        string kyHoaDon = $"{thang:D2}{namNgan}";

        // Kết quả: "HD-PB-28864-00001-0326"
        return $"HD-{maKhachHang}-{kyHoaDon}";
    }
}
