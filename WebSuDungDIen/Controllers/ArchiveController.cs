using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSuDungDIen.Data;
using WebSuDungDIen.Services;

namespace WebSuDungDIen.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ArchiveController : Controller
    {
        private readonly IMongoArchiveService _mongoService;
        private readonly ApplicationDbContext _context;

        public ArchiveController(IMongoArchiveService mongoService, ApplicationDbContext context)
        {
            _mongoService = mongoService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy danh sách từ các Collection khác nhau trong Mongo
            ViewBag.KhachHang = await _mongoService.GetArchivedListAsync("KhachHang");
            ViewBag.NhanVien = await _mongoService.GetArchivedListAsync("NhanVien");
            ViewBag.HoaDon = await _mongoService.GetArchivedListAsync("HoaDon");
            // Sếp phải có dòng này thì View nó mới có data để hiển thị
            ViewBag.TaiKhoan = await _mongoService.GetArchivedListAsync("ApplicationUser");
            return View();
        }
    }
}
