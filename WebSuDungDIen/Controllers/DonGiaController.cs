using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using WebSuDungDIen.Data;
using WebSuDungDIen.Models;
using WebSuDungDIen.ViewModels;
using Microsoft.EntityFrameworkCore;
namespace WebSuDungDIen.Controllers
{
    public class DonGiaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DonGiaController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin, NhanVien")]
        public async Task<IActionResult> Index()
        {
            var rawData = await _context.DonGiaDien.ToListAsync();

            var groupedData = rawData
                // 1. Gộp 6 bậc có cùng thời điểm tạo (cùng timestamp)
                .GroupBy(x => x.NgayTao.ToString("yyyy-MM-dd HH:mm"))
                .Select(g => new LichSuDonGiaVM
                {
                    // 2. Móc chính xác cái timestamp đó ra để ném lên View
                    NgayNhapLanDau = g.First().NgayTao,

                    // 3. Trải phẳng 6 bậc ra
                    Gia1 = g.FirstOrDefault(x => x.Bac == 1)?.Gia ?? 0,
                    Gia2 = g.FirstOrDefault(x => x.Bac == 2)?.Gia ?? 0,
                    Gia3 = g.FirstOrDefault(x => x.Bac == 3)?.Gia ?? 0,
                    Gia4 = g.FirstOrDefault(x => x.Bac == 4)?.Gia ?? 0,
                    Gia5 = g.FirstOrDefault(x => x.Bac == 5)?.Gia ?? 0,
                    Gia6 = g.FirstOrDefault(x => x.Bac == 6)?.Gia ?? 0
                })
                // 4. LƯỚI LỌC CUỐI CÙNG: Tháng nào/ngày nào có sự thay đổi đơn giá (có giá trị > 0) thì mới cho hiển thị!
                .Where(x => x.Gia1 > 0)
                .OrderByDescending(x => x.NgayNhapLanDau)
                .Take(3)
                .ToList();

            return View(groupedData);
        }
    }
}
