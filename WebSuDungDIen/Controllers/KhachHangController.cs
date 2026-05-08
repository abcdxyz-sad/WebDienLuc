using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebSuDungDien.Services;
using WebSuDungDIen.Data;
using WebSuDungDIen.Models;
using WebSuDungDIen.Services;
using WebSuDungDIen.ViewModels;

namespace WebSuDungDIen.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TaoMaService _taoMaService;
        private readonly IMongoArchiveService _mongoService;


        public KhachHangController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, TaoMaService taoMaService, IMongoArchiveService mongoService)
        {
            _context = context;
            _userManager = userManager;
            _taoMaService = taoMaService;
            _mongoService = mongoService;
        }

        // GET: KhachHang
        [Authorize(Roles = "Admin, NhanVien")]
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
        [Authorize(Roles = "Admin, NhanVien")]
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
        [Authorize(Roles = "Admin, NhanVien")]
        public async Task<IActionResult> Create()
        {
            // Lấy danh sách những cái Nick "Cô đơn" (Có Role Khách Hàng nhưng chưa có Hồ Sơ Điện)
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, NhanVien")]
        public async Task<IActionResult> Create([Bind("TenKh,DiaChi,DienThoai,IdentityUserId,MaPhuongApi,DiaChiDayDu")] KhachHang khachHang, string MaMien = "PB", int chiSoBanDau = 0)
        {
            Console.WriteLine("\n=== [DEBUG] BẮT ĐẦU TẠO KHÁCH HÀNG MỚI ===");

            try
            {
                if (khachHang == null) return BadRequest("Dữ liệu gửi lên rỗng.");

                if (string.IsNullOrEmpty(khachHang.IdentityUserId))
                {
                    khachHang.IdentityUserId = null; // Gán cứng thành NULL cho an toàn
                    ModelState.Remove("IdentityUserId");
                }

                // Bỏ qua lỗi của Navigation Property (nếu có)
                ModelState.Remove("User");
                ModelState.Remove("ChiSoDiens");
                ModelState.Remove("HoaDons");

                if (!ModelState.IsValid)
                {
                    Console.WriteLine("[CẢNH BÁO] ModelState KHÔNG hợp lệ!");
                    // Nạp lại danh sách UID nếu Form bị đá về
                    await LoadViewBagUsersAsync();
                    return View(khachHang);
                }

                // Nếu có chọn UID, kiểm tra xem thằng UID đó có bị ai hớt tay trên chưa
                if (!string.IsNullOrEmpty(khachHang.IdentityUserId))
                {
                    var existing = await _context.KhachHang.FirstOrDefaultAsync(x => x.IdentityUserId == khachHang.IdentityUserId);
                    if (existing != null)
                    {
                        ModelState.AddModelError("IdentityUserId", "Tài khoản Web này đã có hồ sơ Khách Hàng!");
                        await LoadViewBagUsersAsync();
                        return View(khachHang);
                    }
                }

                // Sinh mã API
                string maChinhThuc = await _taoMaService.TaoMaHopDongChuanAPIAsync(_context, MaMien, khachHang.MaPhuongApi);
                khachHang.MaKh = maChinhThuc;

                // Xử lý ghép địa chỉ
                if (!string.IsNullOrEmpty(khachHang.DiaChi) && !string.IsNullOrEmpty(khachHang.DiaChiDayDu))
                {
                    khachHang.DiaChi = $"{khachHang.DiaChi}, {khachHang.DiaChiDayDu}";
                }

                // Gán trạng thái (Ví dụ NV tự nhập thì Active luôn, khách tự nhập thì chờ duyệt)
                khachHang.TrangThai = true;

                // Lưu Database
                _context.Add(khachHang);
                await _context.SaveChangesAsync();
                Console.WriteLine($"[Tiến trình] Khởi tạo chỉ số công tơ ban đầu: {chiSoBanDau}");

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // 2. Móc xuống DB tìm xem gã Nhân Viên nào đang giữ cái tài khoản này
                var nhanVienThucHien = await _context.NhanVien.FirstOrDefaultAsync(nv => nv.IdentityUserId == currentUserId);

                var chiSoKyKhong = new ChiSoDien
                {
                    KhachHangId = khachHang.Id, // Nối dây xích vào cổ ông khách vừa đẻ
                    Thang = DateTime.Now.Month,
                    Nam = DateTime.Now.Year,
                    ChiSoCu = 0,               // Chả có cũ đâu, gốc bằng 0
                    ChiSoMoi = chiSoBanDau,    // Số mà sếp gõ ngoài giao diện
                    NhanVienId = nhanVienThucHien != null ? nhanVienThucHien.Id : null
                };
                _context.ChiSoDien.Add(chiSoKyKhong);
                await _context.SaveChangesAsync();
                TempData["ThongBao"] = $"Đã khởi tạo thành công khách hàng: {khachHang.MaKh}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[LỖI TẠO KHÁCH HÀNG]: " + ex.Message);
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                await LoadViewBagUsersAsync();
                return View(khachHang);
            }
        }

        // Hàm hỗ trợ nạp lại danh sách User cho View (Để code đỡ lặp lại)
        private async Task LoadViewBagUsersAsync()
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync("KhachHang");
            ViewBag.Users = usersInRole
                .Where(u => !_context.KhachHang.Any(k => k.IdentityUserId == u.Id))
                .Select(u => new { u.Id, u.Email, u.UserName, u.HoTen }).ToList();
        }

        // GET: KhachHang/Edit/5
        [Authorize(Roles = "Admin, NhanVien")]
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
        [Authorize(Roles = "Admin, NhanVien")]
        public async Task<IActionResult> Edit(string id, [Bind("Id,TenKh,DiaChi,DienThoai,MaPhuongApi,DiaChiDayDu")] KhachHang model)
        {
            var kh = await _context.KhachHang.FindAsync(id);

            if (kh == null || id != kh.Id)
                return NotFound();

            ModelState.Remove("MaKh");
            ModelState.Remove("IdentityUserId");
            // Nếu trong Model KhachHang sếp có liên kết tới bảng khác thì Remove luôn cho chắc:
            ModelState.Remove("ChiSoDienId");
            ModelState.Remove("HoaDonId");

            if (ModelState.IsValid)
            {
                bool coThayDoi = false;

                if (kh.TenKh != model.TenKh) coThayDoi = true;
                if (kh.DienThoai != model.DienThoai) coThayDoi = true;

                if (!coThayDoi)
                {
                    // Sếp có thể đổi câu chửi này cho nó thanh lịch hơn nếu muốn
                    TempData["ThongBao"] = "Hệ thống không ghi nhận có sự thay đổi nào của dữ liệu";
                    return View(model);
                }

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
        [Authorize(Roles = "Admin, NhanVien")]
        public async Task<IActionResult> Delete(String? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var khachHang = await _context.KhachHang
                .FirstOrDefaultAsync(m => m.Id == id);
            if (khachHang == null)
            {
                return NotFound();
            }

            return View(khachHang);
        }

        // POST: KhachHang/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin, NhanVien")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id, string lyDoXoa = "")
        {
            // 🚨 1. CHỐT CHẶN TỬ THẦN: "NGÓ" SANG BẢNG HÓA ĐƠN XEM CÓ DÍNH DÁNG KHÔNG
            // Dùng AnyAsync để kiểm tra cực nhanh: Chỉ cần có ít nhất 1 hóa đơn là trả về true ngay
            bool daCoHoaDon = await _context.HoaDon.AnyAsync(hd => hd.KhachHangId == id);

            if (daCoHoaDon)
            {
                TempData["Error"] = "Cảnh báo: KHÔNG THỂ XÓA! Khách hàng này đã phát sinh hóa đơn trên hệ thống. Vui lòng xử lý hóa đơn trước.";
                return RedirectToAction(nameof(Index)); // Quay xe lập tức!
            }

            string finalReason = string.IsNullOrWhiteSpace(lyDoXoa) ? "Blank" : lyDoXoa;

            // 2. NẾU AN TOÀN (Chưa có hóa đơn), LÔI KHÁCH HÀNG & CHỈ SỐ ĐIỆN LÊN
            var khachHang = await _context.KhachHang
                .Include(k => k.ChiSoDien)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (khachHang == null) return NotFound();

            try
            {
                // 3. TRỊ BỆNH VÒNG LẶP CHO CHỈ SỐ ĐIỆN (Cắt dây liên kết ngược)
                if (khachHang.ChiSoDien != null)
                {
                    foreach (var csd in khachHang.ChiSoDien)
                    {
                        csd.KhachHang = null;
                    }
                }

                // 4. DI CƯ SANG MONGODB AN TOÀN
                await _mongoService.ArchiveAsync(khachHang, User.Identity.Name ?? "Hệ thống", lyDoXoa);

                // 5. XÓA DỌN DẸP Ở SQL SERVER
                // Xóa Chỉ số điện trước (nếu có)
                if (khachHang.ChiSoDien != null && khachHang.ChiSoDien.Any())
                {
                    _context.ChiSoDien.RemoveRange(khachHang.ChiSoDien);
                }

                // Xóa Khách hàng
                _context.KhachHang.Remove(khachHang);
                await _context.SaveChangesAsync();

                TempData["ThongBao"] = $"Đã xóa thành công khách hàng {khachHang.TenKh} nhưng vẫn còn trong hệ thống !";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống trong quá trình xóa: " + ex.Message;
            }

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
        [ValidateAntiForgeryToken] // Nên thêm cái này để bảo mật form
        public async Task<IActionResult> DangKyThongTin([Bind("TenKh,DiaChi,DienThoai,MaPhuongApi,DiaChiDayDu")] KhachHang model)
        {
            Console.WriteLine("\n=== [DEBUG] BẮT ĐẦU ĐĂNG KÝ THÔNG TIN KHÁCH HÀNG ===");

            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                    return RedirectToAction("Login", "Account");

                // Nếu khách không nhập tên, lấy tạm tên từ tài khoản
                if (string.IsNullOrWhiteSpace(model.TenKh))
                {
                    model.TenKh = user.HoTen;
                }

                var khachHang = await _context.KhachHang
                    .FirstOrDefaultAsync(x => x.IdentityUserId == userId);

                if (khachHang == null)
                {
                    Console.WriteLine("[Tiến trình] Bắt đầu thêm hồ sơ mới cho User...");

                    // 1. LẤY MÃ MIỀN VÀ MÃ PHƯỜNG TỪ FORM
                    string maMien = Request.Form["MaMien"].ToString();
                    if (string.IsNullOrEmpty(maMien)) maMien = "PB"; // Fallback mặc định

                    if (string.IsNullOrEmpty(model.MaPhuongApi))
                    {
                        Console.WriteLine("[CẢNH BÁO] MaPhuongApi rỗng. Giao diện chưa gửi mã phường!");
                    }

                    // 2. SINH MÃ CHUẨN API
                    Console.WriteLine($"[Tiến trình] Đang gọi TaoMaHopDongChuanAPIAsync. Miền: {maMien}, Phường: {model.MaPhuongApi}");
                    string maChinhThuc = await _taoMaService.TaoMaHopDongChuanAPIAsync(_context, maMien, model.MaPhuongApi);
                    Console.WriteLine($"[Thành công] Mã sinh ra: {maChinhThuc}");

                    model.MaKh = maChinhThuc;
                    model.IdentityUserId = userId;
                    model.TrangThai = false; // Đăng ký xong phải chờ duyệt

                    // 3. XỬ LÝ GHÉP ĐỊA CHỈ Y NHƯ BÊN CREATE
                    Console.WriteLine($"[Tiến trình] Ghép địa chỉ. Số nhà: {model.DiaChi} | Tỉnh/Phường: {model.DiaChiDayDu}");
                    if (!string.IsNullOrEmpty(model.DiaChi) && !string.IsNullOrEmpty(model.DiaChiDayDu))
                    {
                        // Vì bên form sếp đang lưu (Số nhà) vào model.DiaChi và (Phường, Tỉnh) vào model.DiaChiDayDu
                        model.DiaChi = $"{model.DiaChi}, {model.DiaChiDayDu}";
                        Console.WriteLine($"[Thành công] Địa chỉ hoàn chỉnh lưu DB: {model.DiaChi}");
                    }

                    _context.KhachHang.Add(model);
                    await _context.SaveChangesAsync();
                    Console.WriteLine("[THÀNH CÔNG] Đã lưu thông tin đăng ký vào Database!");

                    TempData["ThongBao"] = "Thêm thông tin thành công, đang chờ duyệt";
                }
                else
                {
                    Console.WriteLine($"[Tiến trình] Cập nhật hồ sơ có sẵn của User: {userId}");

                    khachHang.TenKh = model.TenKh;
                    khachHang.DienThoai = model.DienThoai;
                    khachHang.MaPhuongApi = model.MaPhuongApi;

                    // Xử lý địa chỉ khi Cập nhật
                    if (!string.IsNullOrEmpty(model.DiaChi) && !string.IsNullOrEmpty(model.DiaChiDayDu))
                    {
                        khachHang.DiaChi = $"{model.DiaChi}, {model.DiaChiDayDu}";
                    }
                    else
                    {
                        khachHang.DiaChi = model.DiaChi;
                    }

                    await _context.SaveChangesAsync();
                    Console.WriteLine("[THÀNH CÔNG] Đã cập nhật xong!");

                    TempData["ThongBao"] = "Bạn đã hoàn tất cập nhật thông tin. Hệ thống đã ghi nhận.";
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n================ LỖI ĐĂNG KÝ THÔNG TIN ================");
                Console.WriteLine($"[Lỗi chính]: {ex.Message}");
                Console.WriteLine($"[Stack Trace]:\n{ex.StackTrace}");
                Console.WriteLine("========================================================\n");

                TempData["Loi"] = "Đã xảy ra lỗi hệ thống khi đăng ký. Vui lòng thử lại.";
                return RedirectToAction("Index", "Home");
            }
        }

        [Authorize(Roles = "KhachHang,Admin,NhanVien")]
        public async Task<IActionResult> TienDienThangNay()
        {
            // 1. Lấy khách hàng hiện tại đang đăng nhập
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var khachHang = await _context.KhachHang.FirstOrDefaultAsync(k => k.IdentityUserId == user.Id);

            // 👉 [SỬA LẠI CHỖ NÀY]: Thay NotFound() bằng thông báo cho thân thiện
            if (khachHang == null)
            {
                TempData["Error"] = "Bạn chưa đăng ký hồ sơ sử dụng điện!";
                return RedirectToAction("Index", "Home");
            }

            // 👉 [THÊM MỚI CHỖ NÀY]: Chặn luôn mấy ông đã đăng ký nhưng Admin chưa duyệt
            if (khachHang.TrangThai == false)
            {
                TempData["Error"] = "Hồ sơ của bạn đang chờ duyệt, hệ thống chưa thể cấp hóa đơn.";
                return RedirectToAction("Index", "Home");
            }

            // 2. Tự động lấy hóa đơn MỚI NHẤT của đúng khách hàng này
            var hoaDon = await _context.HoaDon
                .Where(h => h.KhachHangId == khachHang.Id)
                .OrderByDescending(h => h.NgayLap)
                .FirstOrDefaultAsync();

            // Đoạn check hóa đơn null này của sếp làm quá chuẩn rồi, giữ nguyên!
            if (hoaDon == null)
            {
                TempData["ThongBao"] = "Bạn chưa có hóa đơn tiền điện nào trong hệ thống.";
                return RedirectToAction("Index", "Home");
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
            ViewBag.BangGia = bangGia;
            ViewBag.NgayLap = hoaDon.NgayLap;

            return View(hoaDon);
        }

        // Giữ lại hàm SimulatePayment để giả lập thanh toán
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "KhachHang")]
        public async Task<IActionResult> SimulatePayment(string id)
        {
            var hoaDon = await _context.HoaDon.FindAsync(id);
            if (hoaDon != null)
            {
                hoaDon.TrangThai = "DaThanhToan";
                hoaDon.NgayThanhToan = DateTime.Now;

                // 👉 TIÊM VÀO ĐÂY: Đánh dấu thanh toán trực tiếp
                hoaDon.HinhThucThanhToan = "Trực tiếp";

                _context.Update(hoaDon);
                await _context.SaveChangesAsync();
                TempData["ThongBao"] = "Thanh toán thành công. Cảm ơn quý khách!";
            }
            return RedirectToAction(nameof(TienDienThangNay));
        }

        [Authorize(Roles = "KhachHang")]
        public async Task<IActionResult> LichSuGiaoDich()
        {
            // 1. Tóm cổ kẻ đang đăng nhập
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Tìm mã Khách Hàng tương ứng với User này
            var kh = await _context.KhachHang.FirstOrDefaultAsync(x => x.IdentityUserId == currentUserId);
            if (kh == null && !User.IsInRole("Admin"))
                return Forbid(); // Trẻ trâu xài account rác thì cút!

            // 3. Móc dữ liệu
            var lsGiaoDich = await _context.HoaDon
                .Where(h => h.KhachHangId == kh.Id &&
                           (h.TrangThai == "Đã thanh toán" ||
                            h.TrangThai == "Thành công" ||
                            h.TrangThai.Contains("thanh toán") ||
                            h.NgayThanhToan != null))
                .OrderByDescending(h => h.NgayThanhToan)
                .Select(h => new LichSuGiaoDichVM
                {
                    MaGiaoDich = "TXN-" + h.Id.Substring(0, 8).ToUpper(),
                    NgayGiaoDich = h.NgayThanhToan ?? DateTime.Now,
                    TheLoaiGiaoDich = "THANH TOÁN HÓA ĐƠN",

                    ThangNamHoaDon = h.NgayThanhToan.HasValue
                        ? $"Kỳ {h.NgayThanhToan.Value.Month}/{h.NgayThanhToan.Value.Year}"
                        : "Kỳ [KHÔNG RÕ]",

                    // LẤY DỮ LIỆU THỰC TẾ THAY VÌ HARDCODE
                    // (Đổi 'PhuongThucThanhToan' thành tên cột thực tế trong DB của sếp, ví dụ 'KieuThanhToan')
                    // Nếu cột này null, ta ngầm hiểu là họ bấm nút thanh toán trực tiếp trên web.
                    PhuongThuc = h.HinhThucThanhToan,

                    SoTien = h.TongThanhToan,
                    TrangThaiThanhCong = true
                })
                .ToListAsync();

            return View(lsGiaoDich);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restore(string archiveId)
        {
            try
            {
                // 1. Moi cục dữ liệu từ Mongo lên, tự động ép kiểu về KhachHang
                var khachHangPhucHoi = await _mongoService.GetArchivedDataAsync<KhachHang>(archiveId);

                if (khachHangPhucHoi == null)
                {
                    TempData["Error"] = "Không tìm thấy dữ liệu trong kho lưu trữ!";
                    return RedirectToAction(nameof(Index)); // Hoặc Redirect về trang Danh sách lưu trữ
                }

                _context.KhachHang.Add(khachHangPhucHoi);
                await _context.SaveChangesAsync();

                // 3. XÓA KHỎI KHO MONGO (Vì nó đã được sống lại rồi, không nằm ở cõi âm nữa)
                await _mongoService.RemoveFromArchiveAsync<KhachHang>(archiveId);

                TempData["ThongBao"] = $"Đã hoàn lại dữ liệu đã xóa của khách [{khachHangPhucHoi.TenKh}]!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi phục hồi: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
