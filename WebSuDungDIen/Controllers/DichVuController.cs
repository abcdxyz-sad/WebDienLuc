using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebSuDungDIen.Data;
using WebSuDungDIen.Models;

namespace WebSuDungDIen.Controllers
{
    public class DichVuController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DichVuController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> SuDungDien()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("DangNhap", "Account");

            // Admin hoặc Nhân viên
            if (User.IsInRole("Admin") || User.IsInRole("NhanVien"))
            {
                return RedirectToAction("DuyetKhach", "Admin");
            }

            // Khách hàng
            if (User.IsInRole("KhachHang"))
            {
                var hoSo = _context.KhachHang
                    .FirstOrDefault(k => k.IdentityUserId == user.Id);

                // ❌ Chưa có hồ sơ
                if (hoSo == null)
                {
                    return RedirectToAction("DangKyThongTin", "KhachHang");
                }

                // ⏳ Có hồ sơ nhưng chưa duyệt
                if (!hoSo.TrangThai)
                {
                    TempData["ThongBao"] ="Thông tin của bạn đang chờ duyệt. Vui lòng chờ xác nhận.";

                    return RedirectToAction("Index", "Home");
                }

                // ✅ Đã duyệt
                return RedirectToAction("LichSuCuaToi", "ChiSoDien");
            }

            return RedirectToAction("AccessDenied", "Account");
        }
    }
}
