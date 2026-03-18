using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebSuDungDIen.Data;
using WebSuDungDIen.Models;

namespace WebSuDungDIen.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TaoMaService _taoMaService;


        public KhachHangController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, TaoMaService taoMaService)
        {
            _context = context;
            _userManager = userManager;
            _taoMaService = taoMaService;
        }

        // GET: KhachHang
        // Thêm tham số searchKeyword vào hàm Index để hứng chữ người dùng gõ
        public async Task<IActionResult> Index(string? searchKeyword)
        {
            // 1. Khởi tạo câu truy vấn (chưa lấy dữ liệu vội)
            var query = _context.KhachHang.AsQueryable();

            // 2. Kiểm tra xem người dùng có gõ tìm kiếm không
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                string keyword = searchKeyword.Trim().ToLower();

                bool isNumberOnly = searchKeyword.All(char.IsDigit);

                if (isNumberOnly)
                {
                    // NẾU LÀ SỐ: Chỉ quất đúng cột Số điện thoại. 
                    // Dùng StartsWith (Bắt đầu bằng) hoặc == (Chính xác), cấm dùng Contains!
                    query = query.Where(k => k.DienThoai.StartsWith(searchKeyword));
                }
                else
                {
                    // NẾU LÀ CHỮ: Chỉ quét cột Tên hoặc Mã Khách Hàng.
                    // Mã KH thì bắt buộc gõ chính xác, Tên thì cho phép chứa (Contains)
                    query = query.Where(k => k.TenKh.ToLower().Contains(searchKeyword) ||
                                             k.MaKh.ToLower() == searchKeyword);
                }
            }

            // 3. Thực thi lấy dữ liệu (đã được lọc nếu có từ khóa)
            var khachHangs = await query.ToListAsync();

            // 4. Đoạn code ghép nối tài khoản Identity của bạn (GIỮ NGUYÊN)
            var userIds = khachHangs.Select(k => k.IdentityUserId).ToList();
            var users = _userManager.Users
                        .Where(u => userIds.Contains(u.Id))
                        .ToList();

            foreach (var kh in khachHangs)
            {
                kh.User = users
                    .FirstOrDefault(u => u.Id == kh.IdentityUserId);
            }

            // 5. Ném lại từ khóa ra View để cái ô input không bị mất chữ sau khi tìm
            ViewBag.SearchKeyword = searchKeyword;

            return View(khachHangs);
        }
        // GET: KhachHang/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var khachHang = await _context.KhachHang
                .Include(k => k.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (khachHang == null)
            {
                return NotFound();
            }

            return View(khachHang);
        }

        // GET: KhachHang/Create
        public async Task<IActionResult> Create()
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync("KhachHang");
            var users = usersInRole
                .Where(u => !_context.KhachHang.Any(k => k.IdentityUserId == u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.UserName,
                    u.HoTen
                })
                .ToList();

            ViewBag.Users = users;

            return View();
        }

        // POST: KhachHang/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenKh,DiaChi,DienThoai,IdentityUserId,MaPhuongApi,DiaChiDayDu")] KhachHang khachHang, string MaMien = "PB")
        {
            Console.WriteLine("\n=== [DEBUG] BẮT ĐẦU TẠO KHÁCH HÀNG MỚI ===");

            try
            {
                // 1. Kiểm tra đối tượng gửi lên
                if (khachHang == null)
                {
                    Console.WriteLine("[LỖI] Đối tượng khachHang bị null hoàn toàn từ Form gửi lên!");
                    return BadRequest("Dữ liệu gửi lên rỗng.");
                }

                Console.WriteLine($"[Data nhận được]: TenKh: {khachHang.TenKh}, DienThoai: {khachHang.DienThoai}, MaPhuongApi: {khachHang.MaPhuongApi}, UserId: {khachHang.IdentityUserId}");

                // 2. Kiểm tra ModelState
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("[CẢNH BÁO] ModelState KHÔNG hợp lệ! Chi tiết lỗi:");
                    foreach (var state in ModelState)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            Console.WriteLine($"  -> Lỗi ở trường '{state.Key}': {error.ErrorMessage}");
                        }
                    }

                    ViewData["IdentityUserId"] = new SelectList(_context.Users, "Id", "Email", khachHang.IdentityUserId);
                    return View(khachHang);
                }

                // 3. Check trùng hồ sơ
                Console.WriteLine("[Tiến trình] Đang kiểm tra trùng hồ sơ...");
                var existing = await _context.KhachHang
                                .FirstOrDefaultAsync(x => x.IdentityUserId == khachHang.IdentityUserId);

                if (existing != null)
                {
                    Console.WriteLine($"[CẢNH BÁO] Tài khoản {khachHang.IdentityUserId} đã có hồ sơ khách hàng!");
                    ModelState.AddModelError("", "Tài khoản này đã có hồ sơ.");
                    ViewData["IdentityUserId"] = new SelectList(_context.Users, "Id", "Email", khachHang.IdentityUserId);
                    return View(khachHang);
                }

                // 4. KIỂM TRA CÁC DỊCH VỤ (Kẻ khả nghi lớn nhất gây lỗi Null)
                if (_taoMaService == null)
                {
                    Console.WriteLine("[LỖI NGHIÊM TRỌNG] _taoMaService đang bị NULL! Bạn chưa Inject dịch vụ này vào Constructor của KhachHangController.");
                    throw new Exception("Chưa khởi tạo _taoMaService.");
                }

                if (string.IsNullOrEmpty(khachHang.MaPhuongApi))
                {
                    Console.WriteLine("[LỖI] MaPhuongApi bị null hoặc rỗng. Giao diện chưa gửi mã phường về Controller!");
                }

                // 5. Sinh mã
                Console.WriteLine("[Tiến trình] Đang sinh mã API...");
                string maChinhThuc = await _taoMaService.TaoMaHopDongChuanAPIAsync(_context, MaMien, khachHang.MaPhuongApi);
                khachHang.MaKh = maChinhThuc;
                Console.WriteLine($"[Thành công] Mã sinh ra: {maChinhThuc}");

                // 6. Xử lý địa chỉ
                Console.WriteLine($"[Tiến trình] Xử lý địa chỉ. Địa chỉ hiện tại (Số nhà): {khachHang.DiaChi} | Phường/Tỉnh: {khachHang.DiaChiDayDu}");

                // Nếu cả 2 đều có giá trị, ta ghép lại và GÁN VÀO CỘT DiaChi chính thức
                if (!string.IsNullOrEmpty(khachHang.DiaChi) && !string.IsNullOrEmpty(khachHang.DiaChiDayDu))
                {
                    khachHang.DiaChi = $"{khachHang.DiaChi}, {khachHang.DiaChiDayDu}";
                    Console.WriteLine($"[Thành công] Địa chỉ hoàn chỉnh lưu DB: {khachHang.DiaChi}");
                }

                khachHang.TrangThai = true;

                // 7. Lưu DB
                Console.WriteLine("[Tiến trình] Đang lưu vào Database...");
                _context.Add(khachHang);
                await _context.SaveChangesAsync();
                Console.WriteLine("[THÀNH CÔNG] Đã lưu khách hàng mới xong!");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n================ BẮT ĐƯỢC LỖI TẠO KHÁCH HÀNG ================");
                Console.WriteLine($"[Lỗi chính]: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[Inner Exception]: {ex.InnerException.Message}");
                }
                Console.WriteLine($"[Nguồn lỗi (Source)]: {ex.Source}");
                Console.WriteLine($"[Stack Trace]:\n{ex.StackTrace}");
                Console.WriteLine("=============================================================\n");

                ModelState.AddModelError("", "Có lỗi hệ thống xảy ra: " + ex.Message);

                // Trả lại View để khỏi trắng trang
                ViewData["IdentityUserId"] = new SelectList(_context.Users, "Id", "Email", khachHang?.IdentityUserId);
                return View(khachHang);
            }
        }

        // GET: KhachHang/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            if (id == null) return NotFound();

            var khachHang = await _context.KhachHang.FindAsync(id.ToString());
            if (khachHang == null) return NotFound();

            return View(khachHang);
        }

        // POST: KhachHang/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,TenKh,DiaChi,DienThoai,MaPhuongApi,DiaChiDayDu")] KhachHang model)
        {
            var kh = await _context.KhachHang.FindAsync(id);

            if (kh == null || id != kh.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                // 1. Cập nhật những thông tin cơ bản
                kh.TenKh = model.TenKh;
                kh.DienThoai = model.DienThoai;

                // ========================================================
                // 2. XỬ LÝ ĐỊA CHỈ (CHỖ ĂN TIỂM VỚI GIẢNG VIÊN Ở ĐÂY)
                // ========================================================

                // TRƯỜNG HỢP A: Khách bị sai Phường/Xã -> Phải cập nhật lại toàn bộ Phường và Số nhà
                if (!string.IsNullOrEmpty(model.MaPhuongApi) && kh.MaPhuongApi != model.MaPhuongApi)
                {
                    kh.MaPhuongApi = model.MaPhuongApi;
                    kh.DiaChi = model.DiaChi; // Cập nhật số nhà mới luôn

                    // model.DiaChiDayDu lúc này từ Frontend gửi lên chỉ chứa (Phường, Huyện, Tỉnh)
                    if (!string.IsNullOrEmpty(model.DiaChiDayDu))
                    {
                        // Ráp lại: "Số nhà mới, Phường mới..."
                        kh.DiaChiDayDu = $"{model.DiaChi}, {model.DiaChiDayDu}";
                    }
                }
                // TRƯỜNG HỢP B: GIỮ NGUYÊN Phường, CHỈ ĐỔI Số nhà/Ngõ hẻm (kh.DiaChi)
                else if (kh.DiaChi != model.DiaChi)
                {
                    // Ví dụ: kh.DiaChi cũ là "Số 12"
                    // kh.DiaChiDayDu cũ là "Số 12, Phường Bình Đức, TP Long Xuyên"
                    // model.DiaChi mới là "Số 15"

                    if (!string.IsNullOrEmpty(kh.DiaChiDayDu) && !string.IsNullOrEmpty(kh.DiaChi))
                    {
                        // Nếu chuỗi địa chỉ đầy đủ bắt đầu bằng số nhà cũ (Chuẩn form)
                        if (kh.DiaChiDayDu.StartsWith(kh.DiaChi))
                        {
                            // Cắt lấy phần đuôi (", Phường Bình Đức, TP Long Xuyên")
                            string phanDuoiPhuongXa = kh.DiaChiDayDu.Substring(kh.DiaChi.Length);

                            // Ráp số nhà mới vào: "Số 15" + ", Phường..."
                            kh.DiaChiDayDu = $"{model.DiaChi}{phanDuoiPhuongXa}";
                        }
                        else
                        {
                            // Fallback an toàn (Lỡ dữ liệu cũ bị lệch form): Tìm và thay thế
                            kh.DiaChiDayDu = kh.DiaChiDayDu.Replace(kh.DiaChi, model.DiaChi);
                        }
                    }

                    // Lưu lại số nhà mới
                    kh.DiaChi = model.DiaChi;
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: KhachHang/Delete/5
        public async Task<IActionResult> Delete(String? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var khachHang = await _context.KhachHang
                .Include(k => k.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (khachHang == null)
            {
                return NotFound();
            }

            return View(khachHang);
        }

        // POST: KhachHang/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var khachHang = await _context.KhachHang.FindAsync(id);
            if (khachHang != null)
            {
                _context.KhachHang.Remove(khachHang);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool KhachHangExists(string? id)
        {
            return _context.KhachHang.Any(e => e.Id == id);
        }

        public IActionResult SuDungDichVu()
        {
            var userId = _userManager.GetUserId(User);

            var kh = _context.KhachHang
                     .FirstOrDefault(x => x.IdentityUserId == userId);

            if (kh == null)
                return RedirectToAction("DangKyThongTin");

            if (kh.TrangThai == false)
                return RedirectToAction("ChoDuyet");

            return RedirectToAction("Dashboard");
        }

        //GET
        public async Task<IActionResult> DangKyThongTin()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            var model = new KhachHang();

            if (user != null && !string.IsNullOrEmpty(user.HoTen))
            {
                model.TenKh = user.HoTen; // tự động đổ tên xuống form
            }

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> DangKyThongTin(KhachHang model)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(model.TenKh))
            {
                model.TenKh = user.HoTen;
            }

            var khachHang = await _context.KhachHang
                .FirstOrDefaultAsync(x => x.IdentityUserId == userId);

            if (khachHang == null)
            {
                var maKH = _taoMaService.GenerateUniqueCode(
                    "KH",
                    code => _context.KhachHang.Any(x => x.MaKh == code)
                );

                model.MaKh = maKH;
                model.IdentityUserId = userId;
                model.TrangThai = false;

                _context.KhachHang.Add(model);
                await _context.SaveChangesAsync();

                TempData["ThongBao"] ="Thêm thông tin thành công, đang chờ duyệt";
            }
            else
            {
                khachHang.TenKh = model.TenKh;
                khachHang.DiaChi = model.DiaChi;
                khachHang.DienThoai = model.DienThoai;

                await _context.SaveChangesAsync();

                TempData["ThongBao"] ="Bạn đã hoàn tất cập nhật thông tin. Hệ thống đã ghi nhận.";
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "KhachHang")]
        public async Task<IActionResult> TienDienThangNay()
        {
            // 1. Lấy khách hàng hiện tại đang đăng nhập
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var khachHang = await _context.KhachHang.FirstOrDefaultAsync(k => k.IdentityUserId == user.Id);
            if (khachHang == null) return NotFound();

            // 2. Tự động lấy hóa đơn MỚI NHẤT của đúng khách hàng này
            var hoaDon = await _context.HoaDon
                .Where(h => h.KhachHangId == khachHang.Id)
                .OrderByDescending(h => h.NgayLap)
                .FirstOrDefaultAsync();

            if (hoaDon == null)
            {
                TempData["ThongBao"] = "Bạn chưa có hóa đơn tiền điện nào trong hệ thống.";
                return RedirectToAction("Index", "Home"); // Chuyển về trang chủ hoặc lịch sử
            }

            // 3. Lấy các thông tin liên quan y hệt trang Details
            var nv = await _context.NhanVien.FirstOrDefaultAsync(n => n.Id == hoaDon.NhanVienId);
            var chiSo = await _context.ChiSoDien.FirstOrDefaultAsync(c => c.Id == hoaDon.ChiSoDienId);
            var bangGia = await _context.DonGiaDien.OrderBy(g => g.Bac).ToListAsync();

            // 4. Nhồi vào ViewBag đúng chuẩn format mà View đang dùng
            ViewBag.ThongTinKhach = $"{khachHang.TenKh} - {khachHang.MaKh}";
            ViewBag.ThongTinNhanVien = nv != null ? nv.TenNV : "Hệ thống";
            ViewBag.ChiSoCu = chiSo != null ? chiSo.ChiSoCu : 0;
            ViewBag.ChiSoMoi = chiSo != null ? chiSo.ChiSoMoi : 0;

            // 👉 THÊM ĐÚNG 2 DÒNG NÀY ĐỂ LẤY THÁNG/NĂM CHUYỂN RA VIEW LÀM MÃ QR
            ViewBag.Thang = chiSo != null ? chiSo.Thang : 0;
            ViewBag.Nam = chiSo != null ? chiSo.Nam : 0;
            ViewBag.MaKh = khachHang.MaKh;
            ViewBag.BangGia = bangGia; // Đã dùng bảng giá THẬT từ DB!
            ViewBag.NgayLap = hoaDon.NgayLap;

            return View(hoaDon);
        }

        // Giữ lại hàm SimulatePayment để giả lập thanh toán
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "KhachHang")]
        public async Task<IActionResult> SimulatePayment(string id) // Lưu ý kiểu string nếu Id hóa đơn của bạn là string
        {
            var hoaDon = await _context.HoaDon.FindAsync(id);
            if (hoaDon != null)
            {
                hoaDon.TrangThai = "DaThanhToan";
                hoaDon.NgayThanhToan = DateTime.Now; // Đóng dấu ngày giờ thanh toán thật
                _context.Update(hoaDon);
                await _context.SaveChangesAsync();
                TempData["ThongBaoSuccess"] = "Thanh toán thành công. Cảm ơn quý khách!";
            }
            return RedirectToAction(nameof(TienDienThangNay));
        }
    }
}
