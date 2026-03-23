using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebSuDungDIen.Data;
using WebSuDungDIen.Models;

namespace WebSuDungDIen.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _context = context;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> QuanLyTaiKhoan()
        {
            var khachHang = await _userManager.GetUsersInRoleAsync("KhachHang");
            var nhanVien = await _userManager.GetUsersInRoleAsync("NhanVien");
            var admin = await _userManager.GetUsersInRoleAsync("Admin");

            var tatCa = nhanVien.Concat(admin).ToList();

            var khList = _context.KhachHang
            .Include(k => k.User)
            .ToList();

            var nvList = _context.NhanVien
            .Include(n => n.User)
            .ToList();


            var model = new QuanLyTaiKhoanVM
            {
                NhanVien = nhanVien.ToList(),
                KhachHang = khachHang.ToList(),
                Admin = admin.ToList(),
                DanhSachKhach = khList,
                DanhSachNhanVien = nvList
            };
            model.NhanVien = tatCa;
            return View(model);
        }

        public IActionResult DuyetKhach(string userId)
        {
            var hoSo = _context.KhachHang
                .FirstOrDefault(k => k.IdentityUserId == userId);

            if (hoSo == null)
            {
                TempData["ThongBao"] = "Khách chưa tạo hồ sơ để duyệt!";
                return RedirectToAction("QuanLyTaiKhoan");
            }

            if (hoSo.TrangThai)
            {
                TempData["ThongBao"] = "Khách này đã được duyệt rồi.";
                return RedirectToAction("QuanLyTaiKhoan");
            }

            hoSo.TrangThai = true;
            _context.SaveChanges();

            TempData["ThongBao"] = "Duyệt khách thành công!";
            return RedirectToAction("QuanLyTaiKhoan");
        }

        public async Task<IActionResult> ResetPassword(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, "123");

            // 1. Lấy ID của tài khoản ĐANG ĐĂNG NHẬP (người đang thao tác)
            var currentUserId = _userManager.GetUserId(User);

            // 2. Kiểm tra xem có phải đang tự reset tài khoản của chính mình không?
            if (userId == currentUserId)
            {
                // Đăng xuất hoàn toàn để xóa sạch Cookie/Session cũ
                await _signInManager.SignOutAsync();

                TempData["ThongBao"] = "Mật khẩu của bạn đã thay đổi. Hệ thống tự động đăng xuất để bảo mật!";

                // Đá văng về trang chủ (Index của Home)
                return RedirectToAction("Index", "Home");
            }

            // 3. Nếu reset cho người khác thì vẫn về trang Quản Lý như bình thường
            TempData["ThongBao"] = "Đã reset mật khẩu về 123";
            return RedirectToAction("QuanLyTaiKhoan");
        }

        public async Task<IActionResult> ChiTietTaiKhoan(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return NotFound();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var nhanVien = _context.NhanVien
                .FirstOrDefault(n => n.IdentityUserId == userId);

            var khachHang = _context.KhachHang
                .FirstOrDefault(k => k.IdentityUserId == userId);

            var vm = new ChiTietTaiKhoanVM
            {
                User = user,
                NhanVien = nhanVien,
                KhachHang = khachHang
            };

            return View(vm);
        }
        // 1. HÀM GET: Đẩy danh sách khách hàng lên View
        public IActionResult TaoTaiKhoan()
        {
            // Tìm những ông Khách Hàng mà cột IdentityUserId đang rỗng (Chưa có nick)
            var khachChuaCoNick = _context.KhachHang
                .Where(k => string.IsNullOrEmpty(k.IdentityUserId))
                .Select(k => new SelectListItem
                {
                    Value = k.Id.ToString(),
                    // Hiển thị Mã KH - Tên KH - SĐT cho dễ nhìn
                    Text = $"[{k.MaKh}] {k.TenKh} - SĐT: {k.DienThoai}"
                }).ToList();

            ViewBag.DanhSachKhachCu = khachChuaCoNick;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoTaiKhoan(TaoTaiKhoanVM model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.DanhSachKhachCu = _context.KhachHang.Where(k => string.IsNullOrEmpty(k.IdentityUserId)).Select(k => new SelectListItem { Value = k.Id.ToString(), Text = $"[{k.MaKh}] {k.TenKh} - SĐT: {k.DienThoai}" }).ToList();
                return View(model);
            }

            // =======================================================
            // 🚨 BƯỚC 1: XỬ LÝ CÁI TÊN TRƯỚC KHI TẠO TÀI KHOẢN
            // =======================================================
            string tenChinhThuc = model.LoaiTaiKhoan == "NhanVien" ? model.TenNV : model.TenKh;
            KhachHang hoSoKhachCu = null;

            // Nếu là chọn Khách Hàng CŨ từ Dropdown
            if (model.LoaiTaiKhoan == "KhachHang" && !string.IsNullOrEmpty(model.KhachHangId))
            {
                // Chạy vào DB lôi hồ sơ cũ lên trước
                hoSoKhachCu = await _context.KhachHang.FindAsync(model.KhachHangId);

                // Nếu lỡ tay xóa ô Textbox Tên -> Lấy lại tên cũ trong DB bù vào!
                if (hoSoKhachCu != null && string.IsNullOrWhiteSpace(tenChinhThuc))
                {
                    tenChinhThuc = hoSoKhachCu.TenKh;
                }
            }

            // =======================================================
            // 🚨 BƯỚC 2: TẠO TÀI KHOẢN WEB (BẢO ĐẢM KHÔNG BỊ NULL TÊN)
            // =======================================================
            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                HoTen = tenChinhThuc // Đã được bọc thép, không sợ rỗng nữa!
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                ViewBag.DanhSachKhachCu = _context.KhachHang.Where(k => string.IsNullOrEmpty(k.IdentityUserId)).Select(k => new SelectListItem { Value = k.Id.ToString(), Text = $"[{k.MaKh}] {k.TenKh} - SĐT: {k.DienThoai}" }).ToList();
                return View(model);
            }

            // Gán Quyền
            await _userManager.AddToRoleAsync(user, model.LoaiTaiKhoan);

            // =============================================================
            // 🚨 BƯỚC 3: CẬP NHẬT LẠI HỒ SƠ KHÁCH HÀNG
            // =============================================================
            if (hoSoKhachCu != null)
            {
                // Nhét cái User ID vừa tạo vào hồ sơ
                hoSoKhachCu.IdentityUserId = user.Id;

                // CHỈ ĐÈ TÊN MỚI NẾU NHÂN VIÊN THỰC SỰ GÕ CÁI GÌ ĐÓ VÀO TEXTBOX
                if (!string.IsNullOrWhiteSpace(model.TenKh))
                {
                    hoSoKhachCu.TenKh = model.TenKh;
                }

                _context.KhachHang.Update(hoSoKhachCu);
                await _context.SaveChangesAsync();
            }

            TempData["ThongBao"] = "Khởi tạo tài khoản thành công!";
            return RedirectToAction("QuanLyTaiKhoan");
        }

        public async Task<IActionResult> EditTaiKhoan(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            var model = new EditTaiKhoanVM
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTaiKhoan(EditTaiKhoanVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);

            if (user == null)
                return NotFound();

            user.Email = model.Email;
            user.UserName = model.UserName;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["ThongBao"] = "Cập nhật thành công";
                return RedirectToAction("ChiTietTaiKhoan", new { userId = model.Id });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KhoaTaiKhoan(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            user.LockoutEnd = DateTimeOffset.Now.AddYears(100); // khóa 100 năm 😌

            await _userManager.UpdateAsync(user);

            TempData["ThongBao"] = "Tài khoản đã bị khóa";
            return RedirectToAction("ChiTietTaiKhoan", new { userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoKhoaTaiKhoan(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            user.LockoutEnd = null;

            await _userManager.UpdateAsync(user);

            TempData["ThongBao"] = "Tài khoản đã được mở khóa";
            return RedirectToAction("ChiTietTaiKhoan", new { userId });
        }

        public async Task<IActionResult> XoaTaiKhoan(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            await _userManager.DeleteAsync(user);

            TempData["ThongBao"] = "Đã xóa tài khoản";
            return RedirectToAction("QuanLyTaiKhoan");
        }
    }
}
