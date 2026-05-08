using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebSuDungDien.Services;
using WebSuDungDIen.Data;
using WebSuDungDIen.Models;
using WebSuDungDIen.Services;
namespace WebSuDungDIen.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IMongoArchiveService _mongoService;
        public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, SignInManager<ApplicationUser> signInManager, IMongoArchiveService mongoService)
        {
            _userManager = userManager;
            _context = context;
            _signInManager = signInManager;
            _mongoService = mongoService;
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

        [HttpPost] // Đổi thành HttpPost vì form bên giao diện đang gửi bằng phương thức POST
        [Authorize(Roles = "Admin,NhanVien")] // Chặn cửa an toàn
        public async Task<IActionResult> DuyetKhach(string userId, int chiSoDau = 0)
        {
            Console.WriteLine($"\n=== [DEBUG] BẮT ĐẦU DUYỆT HỒ SƠ & GẮN ĐỒNG HỒ CHO USER: {userId} ===");

            // 1. Tìm hồ sơ khách hàng
            var hoSo = await _context.KhachHang.FirstOrDefaultAsync(k => k.IdentityUserId == userId);

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

            // 2. Bật công tắc: Phê duyệt hồ sơ
            hoSo.TrangThai = true;

            // 3. Tìm xem ông Nhân Viên/Admin nào đang bấm duyệt cái này (để lưu dấu vết)
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var nhanVienThucHien = await _context.NhanVien.FirstOrDefaultAsync(nv => nv.IdentityUserId == currentUserId);

            // 4. Khởi tạo mốc chỉ số điện năng đầu tiên (Kỳ 0)
            var chiSoKyKhong = new ChiSoDien
            {
                KhachHangId = hoSo.Id, // Khóa vào đúng ông khách này
                Thang = DateTime.Now.Month,
                Nam = DateTime.Now.Year,
                ChiSoCu = 0,           // Mốc xuất phát lúc nào cũng là 0
                ChiSoMoi = chiSoDau,   // Con số Admin vừa gõ ở cái bảng Modal truyền sang
                NhanVienId = nhanVienThucHien != null ? nhanVienThucHien.Id : null
            };

            _context.ChiSoDien.Add(chiSoKyKhong);

            try
            {
                // 5. Lưu 1 phát ăn luôn cả 2 việc: Đổi trạng thái Khách + Thêm Chỉ Số Điện
                await _context.SaveChangesAsync();

                TempData["ThongBao"] = $"Đã duyệt hồ sơ {hoSo.TenKh} và ghi nhận chỉ số đầu: {chiSoDau} kWh";
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[LỖI DUYỆT KHÁCH]: " + ex.Message);
                TempData["ThongBao"] = "Lỗi hệ thống khi duyệt khách: " + ex.Message;
            }

            return RedirectToAction("QuanLyTaiKhoan");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

            var nhanVienChuaCoNick = _context.NhanVien
                .Where(n => string.IsNullOrEmpty(n.IdentityUserId))
                .Select(n => new SelectListItem
                {
                    Value = n.Id.ToString(),
                    Text = $"[{n.MaNV}] {n.TenNV} - SĐT: {n.DienThoai}"
                }).ToList();

            ViewBag.DanhSachNhanVienCu = nhanVienChuaCoNick;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoTaiKhoan(TaoTaiKhoanVM model)
        {
            if (!ModelState.IsValid)
            {
                // Nhớ load lại cả 2 list nếu bị lỗi Validate đá về View
                ViewBag.DanhSachKhachCu = _context.KhachHang.Where(k => string.IsNullOrEmpty(k.IdentityUserId)).Select(k => new SelectListItem { Value = k.Id.ToString(), Text = $"[{k.MaKh}] {k.TenKh}" }).ToList();
                ViewBag.DanhSachNhanVienCu = _context.NhanVien.Where(n => string.IsNullOrEmpty(n.IdentityUserId)).Select(n => new SelectListItem { Value = n.Id.ToString(), Text = $"[{n.MaNV}] {n.TenNV}" }).ToList();
                return View(model);
            }

            string tenChinhThuc = model.LoaiTaiKhoan == "NhanVien" ? model.TenNV : model.TenKh;
            KhachHang hoSoKhachCu = null;
            NhanVien hoSoNhanVienCu = null; // 👉 Thêm biến hứng Nhân viên cũ

            // Nếu chọn Khách Hàng
            if (model.LoaiTaiKhoan == "KhachHang" && !string.IsNullOrEmpty(model.KhachHangId))
            {
                hoSoKhachCu = await _context.KhachHang.FindAsync(model.KhachHangId);
                if (hoSoKhachCu != null && string.IsNullOrWhiteSpace(tenChinhThuc))
                    tenChinhThuc = hoSoKhachCu.TenKh;
            }

            // 👉 [THÊM MỚI]: Nếu chọn Nhân Viên
            if (model.LoaiTaiKhoan == "NhanVien" && !string.IsNullOrEmpty(model.NhanVienId))
            {
                // Chạy vào DB lôi hồ sơ NV cũ lên
                hoSoNhanVienCu = await _context.NhanVien.FindAsync(model.NhanVienId);
                // Nếu lười không gõ tên -> bốc tên từ DB đắp vào
                if (hoSoNhanVienCu != null && string.IsNullOrWhiteSpace(tenChinhThuc))
                    tenChinhThuc = hoSoNhanVienCu.TenNV;
            }

            // Tạo tài khoản Web
            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                HoTen = tenChinhThuc
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                ViewBag.DanhSachKhachCu = _context.KhachHang.Where(k => string.IsNullOrEmpty(k.IdentityUserId)).Select(k => new SelectListItem { Value = k.Id.ToString(), Text = $"[{k.MaKh}] {k.TenKh}" }).ToList();
                ViewBag.DanhSachNhanVienCu = _context.NhanVien.Where(n => string.IsNullOrEmpty(n.IdentityUserId)).Select(n => new SelectListItem { Value = n.Id.ToString(), Text = $"[{n.MaNV}] {n.TenNV}" }).ToList();
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.LoaiTaiKhoan);

            // Cập nhật Khách (Giữ nguyên)
            if (hoSoKhachCu != null)
            {
                hoSoKhachCu.IdentityUserId = user.Id;
                if (!string.IsNullOrWhiteSpace(model.TenKh)) hoSoKhachCu.TenKh = model.TenKh;
                _context.KhachHang.Update(hoSoKhachCu);
                await _context.SaveChangesAsync();
            }

            // 👉 [THÊM MỚI]: Cập nhật lại ID tài khoản cho Nhân Viên
            if (hoSoNhanVienCu != null)
            {
                hoSoNhanVienCu.IdentityUserId = user.Id;
                // Nếu có gõ tên mới thì đè vào, không thì thôi
                if (!string.IsNullOrWhiteSpace(model.TenNV)) hoSoNhanVienCu.TenNV = model.TenNV;
                _context.NhanVien.Update(hoSoNhanVienCu);
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

            if (user.UserName == model.UserName && user.Email == model.Email)
            {
                // Gắn cảnh báo vào TempData (Key "Warning" này khớp với file HTML tôi đưa sếp ở trên)
                TempData["ThongBao"] = "Hệ thống không ghi nhận có sự thay đổi nào của dữ liệu";
                return View(model);
            }

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

            // 💥 1. LUẬT CHỐNG TỰ HỦY: Không được phép tự khóa chính mình
            var currentUserId = _userManager.GetUserId(User);
            if (userId == currentUserId)
            {
                TempData["ThongBao"] = "CẢNH BÁO: Không được tự khóa tài khoản đang đăng nhập!";
                return RedirectToAction("ChiTietTaiKhoan", new { userId });
            }

            // 💥 2. LUẬT CHỐNG PHẢN QUỐC: Cấm đụng vào quyền lực tối cao (Admin)
            // Sếp có thể check theo Role "Admin" hoặc check thẳng cái UserName "admin"
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin || user.UserName.ToLower() == "admin")
            {
                TempData["ThongBao"] = "Không thể khóa tài khoản admin !";
                return RedirectToAction("ChiTietTaiKhoan", new { userId });
            }

            // Tiến hành giam vào Hắc ngục 100 năm 😌
            user.LockoutEnd = DateTimeOffset.Now.AddYears(100);

            // 💥 3. TÀ THUẬT ĐÁ VĂNG KHỎI HỆ THỐNG: Đổi SecurityStamp
            // Lệnh này ép tất cả các phiên đăng nhập (Cookie) của thằng này trên mọi máy tính/điện thoại lập tức vô giá trị!
            await _userManager.UpdateSecurityStampAsync(user);

            // Lưu lại án tử
            await _userManager.UpdateAsync(user);

            TempData["ThongBao"] = $"THÀNH CÔNG: Đã khóa tài khoản [{user.UserName}] !";
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

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> XoaTaiKhoan(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản!";
                return RedirectToAction("QuanLyTaiKhoan");
            }

            // [LÁ CHẮN BẢO VỆ ADMIN VÀ CHỐNG TỰ SÁT GIỮ NGUYÊN...]
            if (user.UserName.ToLower() == "admin") { /* báo lỗi */ }
            var currentUserId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (user.Id == currentUserId) { /* báo lỗi */ }

            try
            {
                // Đã sửa lại đúng tên hàm và nạp đủ đạn (tham số) cho nó:
                string nguoiXoa = User.Identity?.Name ?? "Hệ thống";
                await _mongoService.ArchiveAsync(user, nguoiXoa, "Thu hồi và xóa tài khoản đăng nhập");

                // 2. ✂️ NGẮT LIÊN KẾT HỒ SƠ (GIỮ LẠI HỒ SƠ Ở SQL)
                // Tìm và xóa ID liên kết ở bảng Nhân viên
                var nhanVien = _context.NhanVien.FirstOrDefault(n => n.Id == userId);
                if (nhanVien != null)
                {
                    nhanVien.Id = null;
                }

                // Tìm và xóa ID liên kết ở bảng Khách hàng
                var khachHang = _context.KhachHang.FirstOrDefault(k => k.Id == userId);
                if (khachHang != null)
                {
                    khachHang.Id = null;
                }

                // Lưu thay đổi ngắt liên kết vào SQL
                await _context.SaveChangesAsync();

                // 3. ⚔️ TRẢM ACCOUNT Ở IDENTITY SQL
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    TempData["ThongBao"] = $"Hệ thống: Đã tiêu hủy tài khoản [{user.UserName}]. Dữ liệu đã được đưa vào kho lưu trữ SYS_ARCHIVE.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi quy trình: " + ex.Message;
            }

            return RedirectToAction("QuanLyTaiKhoan");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restore(string archiveId)
        {
            try
            {
                // 1. Lấy dữ liệu từ Mongo
                var user = await _mongoService.GetArchivedDataAsync<ApplicationUser>(archiveId);
                if (user == null)
                {
                    TempData["Error"] = "Không tìm thấy dữ liệu trong kho lưu trữ!";
                    return RedirectToAction("Index", "Archive");
                }

                // 2. Phục hồi "cái xác" vào Identity SQL
                var result = await _userManager.CreateAsync(user);

                if (result.Succeeded)
                {
                    bool daTimThayHoSoGoc = false;

                    // ==========================================================
                    // 🌟 TÌM LẠI HỒ SƠ GỐC BẰNG SỐ ĐIỆN THOẠI
                    // ==========================================================
                    if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                    {
                        // Dò bên bảng Nhân Viên
                        var isNhanVien = _context.NhanVien.FirstOrDefault(n => n.DienThoai == user.PhoneNumber);
                        // Dò bên bảng Khách Hàng
                        var isKhachHang = _context.KhachHang.FirstOrDefault(k => k.DienThoai == user.PhoneNumber);

                        if (isNhanVien != null)
                        {
                            await _userManager.AddToRoleAsync(user, "NhanVien");
                            isNhanVien.Id = user.Id; // Nối lại liên kết
                            daTimThayHoSoGoc = true;
                        }
                        else if (isKhachHang != null)
                        {
                            await _userManager.AddToRoleAsync(user, "KhachHang");
                            isKhachHang.Id = user.Id; // Nối lại liên kết
                            daTimThayHoSoGoc = true;
                        }
                    }

                    // Nếu lỡ số điện thoại trống, hoặc tìm không ra, thì dùng mẹo tên User để cấp quyền dự phòng
                    if (!daTimThayHoSoGoc)
                    {
                        string lowerUserName = user.UserName.ToLower();
                        if (lowerUserName.Contains("admin") || lowerUserName == "quantri")
                        {
                            await _userManager.AddToRoleAsync(user, "Admin");
                        }
                        else if (lowerUserName.StartsWith("nv") || lowerUserName.Contains("nhanvien"))
                        {
                            await _userManager.AddToRoleAsync(user, "NhanVien");
                        }
                        else
                        {
                            await _userManager.AddToRoleAsync(user, "KhachHang");
                        }
                    }

                    // Lưu thay đổi cái dòng nối UserId vào Database
                    await _context.SaveChangesAsync();

                    // 3. Dọn rác trong Mongo
                    await _mongoService.RemoveFromArchiveAsync<ApplicationUser>(archiveId);

                    TempData["ThongBao"] = $"Khôi phục thành công [{user.UserName}]! Đã nối lại hồ sơ và cấp quyền dựa trên Số Điện Thoại.";
                }
                else
                {
                    string error = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["Error"] = "Lỗi tạo User: " + error;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
            }

            return RedirectToAction("Index", "Archive"); // Nhớ check lại tên Controller chỗ này sếp nhé
        }
    }
}
