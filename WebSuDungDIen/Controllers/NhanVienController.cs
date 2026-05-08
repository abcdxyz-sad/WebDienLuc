using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebSuDungDien.Services;
using WebSuDungDIen.Data;
using WebSuDungDIen.Models;
using WebSuDungDIen.Services;

namespace WebSuDungDIen.Controllers
{
    [Authorize(Roles = "Admin")]
    public class NhanVienController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TaoMaService _taoMaService;
        private readonly IMongoArchiveService _mongoService;

        public NhanVienController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, TaoMaService taoMaService, IMongoArchiveService mongoService)
        {
            _context = context;
            _userManager = userManager;
            _taoMaService = taoMaService;
            _mongoService = mongoService;
        }

        // GET: NhanVien
        public async Task<IActionResult> Index()
        {
            var nhanViens = await _context.NhanVien.ToListAsync();

            var userIds = nhanViens
                .Select(nv => nv.IdentityUserId)
                .ToList();

            var users = _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToList();

            foreach (var nv in nhanViens)
            {
                nv.User = users
                    .FirstOrDefault(u => u.Id == nv.IdentityUserId);
            }

            return View(nhanViens);
        }

        // GET: NhanVien/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nhanVien = await _context.NhanVien
                .Include(n => n.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (nhanVien == null)
            {
                return NotFound();
            }

            return View(nhanVien);
        }

        public async Task<JsonResult> GetHoTen(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return Json("");

            return Json(user.HoTen);
        }

        // GET: NhanVien/Create
        [Authorize(Roles = "Admin")] // Thường tạo NV mới thì Admin mới được làm
        public async Task<IActionResult> Create()
        {
            // 1. Lấy danh sách Nick "Cô đơn" (Có Role Nhân Viên nhưng chưa có Hồ sơ)
            var usersInRole = await _userManager.GetUsersInRoleAsync("NhanVien");
            var users = usersInRole
                .Where(u => !_context.NhanVien.Any(n => n.IdentityUserId == u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.UserName,
                    u.HoTen // Lấy đúng trường HoTen để đẩy sang View
                })
                .ToList();

            ViewBag.Users = users;

            // 2. Giữ lại cái list Chức vụ của sếp
            ViewBag.ChucVuList = new SelectList(new List<object>
            {
                new { Value = "NhanVien", Text = "Nhân viên" },
                new { Value = "Admin", Text = "Admin" }
            }, "Value", "Text");

            return View();
        }

        // POST: NhanVien/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        // [Authorize(Roles = "Admin")] // Mở comment nếu bạn muốn giới hạn chỉ Admin được tạo Nhân viên
        public async Task<IActionResult> Create([Bind("IdentityUserId,TenNV,DiaChi,DienThoai,ChucVu")] NhanVien nhanVien)
        {
            Console.WriteLine("\n=== [DEBUG] BẮT ĐẦU TẠO NHÂN VIÊN MỚI ===");

            try
            {
                if (nhanVien == null) return BadRequest("Dữ liệu gửi lên rỗng.");

                // === 1. XỬ LÝ IDENTITYUSERID RỖNG ===
                if (string.IsNullOrEmpty(nhanVien.IdentityUserId))
                {
                    nhanVien.IdentityUserId = null; // Gán cứng thành NULL cho an toàn
                    ModelState.Remove("IdentityUserId");
                }

                // === 2. DỌN DẸP VALIDATE LIÊN KẾT ===
                // Gỡ các Navigation Properties (tuỳ theo tên khai báo trong Model NhanVien của bạn)
                ModelState.Remove("AppUser");
                ModelState.Remove("HoaDons");
                ModelState.Remove("ChiSoDiens");
                ModelState.Remove("MaNV"); // Gỡ MaNV vì lát nữa hệ thống tự sinh

                // === 3. KIỂM TRA VALIDATE FORM ===
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("[CẢNH BÁO] ModelState KHÔNG hợp lệ!");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine("LỖI VALIDATE: " + error.ErrorMessage);
                    }
                    // Nạp lại danh sách User để load dropdown khi bị đá lại View
                    await LoadNhanVienDropdown(nhanVien.IdentityUserId);
                    return View(nhanVien);
                }

                // === 4. CHỐNG HỚT TAY TRÊN (TRÙNG TÀI KHOẢN) ===
                if (!string.IsNullOrEmpty(nhanVien.IdentityUserId))
                {
                    var existing = await _context.NhanVien.FirstOrDefaultAsync(x => x.IdentityUserId == nhanVien.IdentityUserId);
                    if (existing != null)
                    {
                        ModelState.AddModelError("IdentityUserId", "Tài khoản Web này đã được phân công cho một Nhân viên khác!");
                        await LoadNhanVienDropdown(nhanVien.IdentityUserId);
                        return View(nhanVien);
                    }
                }

                // === 5. SINH MÃ NHÂN VIÊN TỰ ĐỘNG ===
                var maNV = _taoMaService.GenerateUniqueCode(
                    "NV",
                    code => _context.NhanVien.Any(x => x.MaNV == code)
                );
                nhanVien.MaNV = maNV;

                // === 6. ĐỒNG BỘ VỚI BẢNG TÀI KHOẢN (NẾU CÓ CHỌN TÀI KHOẢN) ===
                if (!string.IsNullOrEmpty(nhanVien.IdentityUserId))
                {
                    var user = await _userManager.FindByIdAsync(nhanVien.IdentityUserId);
                    if (user != null)
                    {
                        if (!string.IsNullOrWhiteSpace(nhanVien.TenNV))
                        {
                            // Cập nhật ngược tên mới từ Form NV sang bảng Tài khoản
                            user.HoTen = nhanVien.TenNV;
                            await _userManager.UpdateAsync(user);
                        }
                        else
                        {
                            // Nếu lười gõ tên, lấy luôn tên từ Tài khoản đắp qua
                            nhanVien.TenNV = user.HoTen;
                        }

                        // Cập nhật Role cho tài khoản theo Chức Vụ
                        var currentRoles = await _userManager.GetRolesAsync(user);
                        if (currentRoles.Any())
                        {
                            await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        }
                        await _userManager.AddToRoleAsync(user, nhanVien.ChucVu);
                    }
                }

                // === 7. LƯU VÀO DATABASE ===
                _context.NhanVien.Add(nhanVien);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[Tiến trình] Đã lưu thành công Nhân viên: {nhanVien.MaNV}");

                TempData["ThongBao"] = $"Thêm nhân viên mới thành công: {nhanVien.MaNV}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[LỖI TẠO NHÂN VIÊN]: " + ex.Message);
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);

                // Nhớ load lại dropdown nếu có lỗi văng ra catch
                await LoadNhanVienDropdown(nhanVien.IdentityUserId);
                return View(nhanVien);
            }
        }

        private async Task LoadNhanVienDropdown(string? selectedId)
        {
            var users = await _userManager.GetUsersInRoleAsync("NhanVien");

            var daCoHoSo = _context.NhanVien
                                .Select(n => n.IdentityUserId)
                                .ToList();

            var usersChuaCoHoSo = users
                .Where(u => !daCoHoSo.Contains(u.Id))
                .ToList();

            ViewData["IdentityUserId"] =
                new SelectList(usersChuaCoHoSo, "Id", "UserName", selectedId);
        }

        // GET: NhanVien/Edit/5
        public async Task<IActionResult> Edit(string id) // BƯỚC SỬA QUAN TRỌNG NHẤT: Đổi int? thành string
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var nhanVien = await _context.NhanVien.FindAsync(id);
            if (nhanVien == null)
                return NotFound();

            // Dropdown chức vụ
            ViewBag.ChucVuList = new SelectList(new List<object>
                {
                    new { Value = "NhanVien", Text = "Nhân viên" },
                    new { Value = "Admin", Text = "Admin" }
                }, "Value", "Text", nhanVien?.ChucVu);

            return View(nhanVien);
        }

        // POST: NhanVien/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,TenNV,DiaChi,DienThoai,ChucVu")] NhanVien model) // Đã xóa TrangThai ở đây
        {
            if (id != model.Id)
                return NotFound();

            var nhanVien = await _context.NhanVien.FindAsync(id);
            if (nhanVien == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                // Chỉ update field cho phép
                nhanVien.TenNV = model.TenNV;
                nhanVien.DiaChi = model.DiaChi;
                nhanVien.DienThoai = model.DienThoai;
                nhanVien.ChucVu = model.ChucVu;
                // ĐÃ XÓA dòng: nhanVien.TrangThai = model.TrangThai;

                await _context.SaveChangesAsync();

                // 🔥 Update role Identity theo ChucVu
                var user = await _userManager.FindByIdAsync(nhanVien.IdentityUserId);

                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, roles);
                    await _userManager.AddToRoleAsync(user, nhanVien.ChucVu);
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.ChucVuList = new SelectList(new List<string>
            {
                "NhanVien",
                "Admin"
            }, model.ChucVu);

            return View(model);
        }

        // GET: NhanVien/Delete/5
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nhanVien = await _context.NhanVien
                .Include(n => n.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (nhanVien == null)
            {
                return NotFound();
            }

            // ==========================================================
            // 🛡️ LÁ CHẮN 1: BẢO VỆ GIAO DIỆN (CHỐNG TỰ SÁT)
            // ==========================================================
            var currentUserId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            // So sánh IdentityUserId của hồ sơ nhân viên với ID của kẻ đang online
            if (nhanVien.IdentityUserId == currentUserId)
            {
                TempData["Error"] = "CẢNH BÁO: Không được tự xóa tài khoản của chính mình!";
                return RedirectToAction(nameof(Index));
            }

            return View(nhanVien);
        }

        // POST: NhanVien/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id, string lyDoXoa = "")
        {
            var nhanVien = await _context.NhanVien.FindAsync(id);
            if (nhanVien == null) return NotFound();
            var currentUserId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (nhanVien.IdentityUserId == currentUserId)
            {
                TempData["Error"] = "LỖI: Không được tự xóa tài khoản của chính mình!";
                return RedirectToAction(nameof(Index));
            }

            bool daNhapChiSo = await _context.ChiSoDien.AnyAsync(c => c.NhanVienId == id);
            bool daLapHoaDon = await _context.HoaDon.AnyAsync(h => h.NhanVienId == id);

            if (daNhapChiSo || daLapHoaDon)
            {
                TempData["Error"] = "LỖI: Nhân viên này đã từng tham gia ghi chỉ số điện hoặc lập hóa đơn. Lịch sử hệ thống không cho phép xóa!";
                return RedirectToAction(nameof(Index));
            }

            // Chuẩn bị lý do để ghi vào sổ Nam Tào (MongoDB)
            string finalReason = string.IsNullOrWhiteSpace(lyDoXoa) ? "Không có lý do" : lyDoXoa;

            try
            {
                await _mongoService.ArchiveAsync(nhanVien, User.Identity.Name ?? "Hệ thống_Admin", finalReason);
                _context.NhanVien.Remove(nhanVien);
                await _context.SaveChangesAsync();

                TempData["ThongBao"] = $"Đã xóa hồ sơ nhân sự [{nhanVien.TenNV}] thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống trong quá trình tiêu hủy: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: NhanVien/Restore
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // ⚠️ Tuyệt đối cấm Nhân viên tự gọi hồn nhau!
        public async Task<IActionResult> Restore(string archiveId)
        {
            // Ngăn chặn bọn trẻ trâu phá bùa chú
            if (string.IsNullOrEmpty(archiveId))
            {
                TempData["Error"] = "LỖI LÔ-GÍC: Thiếu mã định danh linh hồn (Archive ID)!";
                return Redirect(Request.Headers["Referer"].ToString() ?? "/");
            }

            try
            {
                var nhanVienPhucHoi = await _mongoService.GetArchivedDataAsync<NhanVien>(archiveId);

                if (nhanVienPhucHoi == null)
                {
                    TempData["Error"] = "THẤT BẠI: Không tìm thấy nhân sự này trong Archive!";
                    return Redirect(Request.Headers["Referer"].ToString() ?? "/");
                }

                _context.NhanVien.Add(nhanVienPhucHoi);
                await _context.SaveChangesAsync();

                await _mongoService.RemoveFromArchiveAsync<NhanVien>(archiveId);

                TempData["ThongBao"] = $"Đã khôi phục nhân sự [{nhanVienPhucHoi.TenNV}] về lại hệ thống!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi phục hồi dữ liệu: " + ex.Message;
            }
            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }

        private bool NhanVienExists(string id)
        {
            return _context.NhanVien.Any(e => e.Id == id);
        }
    }
}
