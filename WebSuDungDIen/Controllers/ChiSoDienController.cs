using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
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
using WebSuDungDIen.Data;
using WebSuDungDIen.Models;
using WebSuDungDIen.Services;

namespace WebSuDungDIen.Controllers
{
    public class ChiSoDienController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChiSoDienController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin, NhanVien")]
        public async Task<IActionResult> Index(string maTinh, string maPhuongApi, string searchKeyword, int? thang, int? nam)
        {
            var query = _context.KhachHang.AsQueryable();

            if (!string.IsNullOrEmpty(maTinh))
            {
                query = query.Where(k => k.DiaChiDayDu.Contains(maTinh));
            }

            if (!string.IsNullOrEmpty(maPhuongApi))
            {
                query = query.Where(k => k.MaPhuongApi == maPhuongApi);
            }

            if (!string.IsNullOrEmpty(searchKeyword))
            {
                string keyword = searchKeyword.Trim().ToLower();
                bool isNumberOnly = keyword.All(char.IsDigit);

                if (isNumberOnly)
                {
                    query = query.Where(k => k.DienThoai.StartsWith(keyword));
                }
                else
                {
                    query = query.Where(k => k.TenKh.ToLower().Contains(keyword) || k.MaKh.ToLower() == keyword);
                }
            }

            // --- TỐI ƯU: Lấy Data cho Dropdown ---
            var addressData = await _context.KhachHang
                .Where(k => !string.IsNullOrEmpty(k.MaPhuongApi) && !string.IsNullOrEmpty(k.DiaChiDayDu))
                .Select(k => new { k.DiaChiDayDu, k.MaPhuongApi })
                .Distinct()
                .ToListAsync();

            ViewBag.DanhSachTinh = addressData
                .Select(k => k.DiaChiDayDu.Split(',').LastOrDefault()?.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .Select(t => new SelectListItem { Value = t, Text = t, Selected = (t == maTinh) })
                .ToList();

            ViewBag.DanhSachPhuong = addressData
                .Where(k => string.IsNullOrEmpty(maTinh) || k.DiaChiDayDu.Contains(maTinh))
                .GroupBy(k => k.MaPhuongApi)
                .Select(g => {
                    var parts = g.First().DiaChiDayDu.Split(',');
                    return new SelectListItem
                    {
                        Value = g.Key,
                        Text = parts.Length >= 2 ? parts[parts.Length - 2].Trim() : "Phường " + g.Key,
                        Selected = (g.Key == maPhuongApi)
                    };
                }).ToList();

            // ==========================================
            // XỬ LÝ KỲ CHỐT (THÁNG / NĂM)
            // Nếu mới vào trang chưa chọn gì, mặc định lấy tháng/năm hiện tại
            // ==========================================
            int currentThang = thang ?? 0;
            int currentNam = nam ?? 0;

            // Nếu không có tham số truyền vào (lần đầu load trang)
            if (currentThang == 0 || currentNam == 0)
            {
                // Truy vấn tìm tháng/năm lớn nhất đã có người nhập chỉ số
                var kyChotMoiNhat = await _context.ChiSoDien
                    .OrderByDescending(c => c.Nam)
                    .ThenByDescending(c => c.Thang)
                    .Select(c => new { c.Thang, c.Nam })
                    .FirstOrDefaultAsync();

                if (kyChotMoiNhat != null)
                {
                    currentThang = kyChotMoiNhat.Thang;
                    currentNam = kyChotMoiNhat.Nam;
                }
                else
                {
                    // Nếu DB trống trơn chưa có ai nhập bao giờ thì mới xài tháng hiện tại
                    currentThang = DateTime.Now.Month;
                    currentNam = DateTime.Now.Year;
                }
            }

            ViewBag.SearchKeyword = searchKeyword;
            ViewBag.MaPhuongApi = maPhuongApi;
            ViewBag.SelectedThang = currentThang;
            ViewBag.SelectedNam = currentNam;

            // Lấy dữ liệu bảng và Filter ĐÚNG CÁI THÁNG/NĂM đang chọn
            var rawData = await query.Select(k => new
            {
                KhachHangId = k.Id,
                TenKh = k.TenKh,
                DiaChi = k.DiaChiDayDu ?? k.DiaChi,
                // SỬA Ở ĐÂY: Chỉ lấy record có Thang và Nam khớp với bộ lọc Timeline
                ChiSoKyNay = k.ChiSoDien.FirstOrDefault(c => c.Thang == currentThang && c.Nam == currentNam)
            }).ToListAsync();

            var data = rawData.Select(x => new ChiSoDienIndexVM
            {
                KhachHangId = x.KhachHangId,
                TenKh = x.TenKh,
                DiaChi = x.DiaChi,
                // Nếu ChiSoKyNay = null (tháng này chưa chốt) -> gán = 0 để UI hiện chữ "CHƯA CÓ" đỏ chót
                ThangGanNhat = x.ChiSoKyNay?.Thang ?? 0,
                NamGanNhat = x.ChiSoKyNay?.Nam ?? 0,
                ChiSoCu = x.ChiSoKyNay?.ChiSoCu ?? 0,
                ChiSoMoi = x.ChiSoKyNay?.ChiSoMoi ?? 0,
                DienTieuThu = (x.ChiSoKyNay != null && x.ChiSoKyNay.ChiSoCu > 0)
                              ? (x.ChiSoKyNay.ChiSoMoi - x.ChiSoKyNay.ChiSoCu) : 0
            }).ToList();

            return View(data);
        }

        // --- THÊM ACTION NÀY ĐỂ JS GỌI LẤY PHƯỜNG KHI ĐỔI TỈNH ---
        [HttpGet]
        public async Task<IActionResult> GetPhuongByTinh(string maTinh)
        {
            var addressData = await _context.KhachHang
                .Where(k => !string.IsNullOrEmpty(k.MaPhuongApi) && !string.IsNullOrEmpty(k.DiaChiDayDu))
                .Where(k => string.IsNullOrEmpty(maTinh) || k.DiaChiDayDu.Contains(maTinh))
                .Select(k => new { k.DiaChiDayDu, k.MaPhuongApi })
                .Distinct()
                .ToListAsync();

            var phuongList = addressData
                .GroupBy(k => k.MaPhuongApi)
                .Select(g => {
                    var parts = g.First().DiaChiDayDu.Split(',');
                    return new
                    {
                        value = g.Key,
                        text = parts.Length >= 2 ? parts[parts.Length - 2].Trim() : "Phường " + g.Key
                    };
                }).ToList();

            return Json(phuongList);
        }

        // GET: ChiSoDien/Details/5
        [Authorize(Roles = "Admin, NhanVien")]
        public async Task<IActionResult> Details(string id)
        {
            var kh = await _context.KhachHang
                .Include(x => x.ChiSoDien)
                .ThenInclude(c => c.NhanVien) // 💥 MÓC NỐI TIẾP: Kéo thằng Nhân Viên ra ánh sáng!
                .FirstOrDefaultAsync(x => x.Id == id);

            if (kh == null)
            {
                return NotFound();
            }   

            if (!string.IsNullOrEmpty(kh.IdentityUserId))
            {
                kh.User = await _context.Users.FirstOrDefaultAsync(u => u.Id == kh.IdentityUserId);
            }

            return View(kh);
        }

        // GET: ChiSoDien/Create
        [Authorize(Roles = "Admin, NhanVien")]
        [HttpGet]
        public IActionResult Create(string maPhuongApi, string startTour, int? thang, int? nam)
        {
            // Cờ báo hiệu có đang chạy Tour không
            bool isTour = !string.IsNullOrEmpty(startTour) && startTour.Trim().ToLower() == "true";

            // --- KHÚC 1: XỬ LÝ TOUR (BỐC DATA TẠI CHỖ) ---
            if (isTour && string.IsNullOrEmpty(maPhuongApi))
            {
                // Ưu tiên 1: Phường có chỉ số
                var dataMau = (from kh in _context.KhachHang
                                join cs in _context.ChiSoDien on kh.Id equals cs.KhachHangId
                                where !string.IsNullOrEmpty(kh.MaPhuongApi)
                                orderby cs.Nam descending, cs.Thang descending
                                select new { kh.MaPhuongApi, cs.Thang, cs.Nam }).FirstOrDefault();

                if (dataMau != null)
                {
                    maPhuongApi = dataMau.MaPhuongApi;
                    thang = dataMau.Thang;
                    nam = dataMau.Nam;
                }
                else
                {
                    // Ưu tiên 2: Phường chỉ có khách
                    maPhuongApi = _context.KhachHang
                        .FirstOrDefault(k => !string.IsNullOrEmpty(k.MaPhuongApi))?.MaPhuongApi;
                    thang = DateTime.Now.Month;
                    nam = DateTime.Now.Year;
                }
            }

            // --- KHÚC 2: CHẶN LỖI ---
            if (string.IsNullOrEmpty(maPhuongApi))
            {
                TempData["ThongBao"] = "Vui lòng chọn tình, phường/xã trước.";
                return RedirectToAction("Index");
            }

            // Gán lại ViewBag để View và JS sử dụng
            ViewBag.MaPhuongApi = maPhuongApi;
            ViewBag.Thang = thang;
            ViewBag.Nam = nam;
            ViewBag.StartTour = isTour ? "true" : "false";

            var ds = _context.KhachHang
                .Where(kh => kh.MaPhuongApi == maPhuongApi)
                .Select(kh => new ChiSoDienIndexVM
                {
                    MaKh = kh.MaKh,
                    KhachHangId = kh.Id,
                    TenKh = kh.TenKh,
                    DiaChi = kh.DiaChiDayDu ?? kh.DiaChi,
                    ChiSoCu = _context.ChiSoDien
                        .Where(cs => cs.KhachHangId == kh.Id)
                        .OrderByDescending(cs => cs.Nam).ThenByDescending(cs => cs.Thang)
                        .Select(cs => cs.ChiSoMoi).FirstOrDefault(),
                    ThangGanNhat = _context.ChiSoDien
                        .Where(cs => cs.KhachHangId == kh.Id)
                        .OrderByDescending(cs => cs.Nam).ThenByDescending(cs => cs.Thang)
                        .Select(cs => cs.Thang).FirstOrDefault(),
                    NamGanNhat = _context.ChiSoDien
                        .Where(cs => cs.KhachHangId == kh.Id)
                        .OrderByDescending(cs => cs.Nam).ThenByDescending(cs => cs.Thang)
                        .Select(cs => cs.Nam).FirstOrDefault()
                })
                .ToList();

            if (!ds.Any())
            {
                TempData["ThongBao"] = $"Phường {maPhuongApi} hiện chưa có khách hàng nào.";
                return RedirectToAction("Index");
            }

            return View(ds);
        }

        // POST: ChiSoDien/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin, NhanVien")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int Thang, int Nam, List<ChiSoDienIndexVM> model)
        {
            // [TRẠM QUÉT SỐ 1] Xem Form có gửi cái quần đùi gì lên không
            System.Diagnostics.Debug.WriteLine($"\n========== [SYSTEM_LOG] KHỞI ĐỘNG TIẾN TRÌNH LƯU CHỈ SỐ ==========");
            System.Diagnostics.Debug.WriteLine($"[INPUT] Tháng: {Thang} | Năm: {Nam}");
            System.Diagnostics.Debug.WriteLine($"[INPUT] Số lượng bản ghi nhận được từ View: {(model == null ? "NULL" : model.Count.ToString())}");

            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var nhanVien = await _context.NhanVien.FirstOrDefaultAsync(x => x.IdentityUserId == identityUserId);

            if (nhanVien == null)
            {
                TempData["ThongBao"] = "Không xác định được nhân viên.";
                return RedirectToAction(nameof(Index));
            }

            if (Thang < 1 || Thang > 12)
            {
                ModelState.AddModelError("", "Tháng không hợp lệ.");
                return View(model);
            }

            if (model == null || !model.Any())
            {
                TempData["ThongBao"] = "Không có dữ liệu gửi lên.";
                return RedirectToAction(nameof(Index));
            }

            // ========================================================
            // ❌ XÓA HOẶC COMMENT ĐOẠN NÀY LẠI
            // ========================================================
            // bool daCoChiSoThangNay = await _context.ChiSoDien.AnyAsync(x => x.Thang == Thang && x.Nam == Nam);
            // if (daCoChiSoThangNay)
            // {
            //     TempData["ThongBao"] = $"[ CẢNH BÁO ] Dữ liệu chỉ số điện của Tháng {Thang}/{Nam} đã tồn tại trong hệ thống. Không thể tạo đè!";
            //     return View(model);
            // }

            var danhSachLoi = new List<string>();
            int soLuongThem = 0;

            foreach (var item in model)
            {
                System.Diagnostics.Debug.WriteLine($"\n--- Đang quét Khách Hàng: {item.KhachHangId} ---");
                System.Diagnostics.Debug.WriteLine($"[DATA_CHECK] Chỉ số mới nhận được từ Form: {item.ChiSoMoi}");

                // 1. Kiểm tra xem có nhập số chưa
                if (item.ChiSoMoi <= 0)
                {
                    string err = $"Khách {item.KhachHangId} bị loại do Chỉ Số Mới = {item.ChiSoMoi}";
                    danhSachLoi.Add(err);
                    System.Diagnostics.Debug.WriteLine($"[THẤT BẠI] {err}");
                    continue;
                }

                // 2. Kiểm tra ĐÚNG KHÁCH ĐÓ đã nhập tháng này chưa
                var existMonth = await _context.ChiSoDien
                    .FirstOrDefaultAsync(x =>
                        x.KhachHangId == item.KhachHangId &&
                        x.Thang == Thang &&
                        x.Nam == Nam);

                if (existMonth != null)
                {
                    string err = $"Khách {item.KhachHangId} đã tồn tại dữ liệu tháng {Thang}/{Nam}";
                    danhSachLoi.Add(err);
                    System.Diagnostics.Debug.WriteLine($"[THẤT BẠI] {err}");
                    continue;
                }

                // 3. So sánh với tháng trước
                var last = await _context.ChiSoDien
                    .Where(x => x.KhachHangId == item.KhachHangId)
                    .OrderByDescending(x => x.Nam)
                    .ThenByDescending(x => x.Thang)
                    .FirstOrDefaultAsync();

                bool laLanNhapDauTien = (last == null);
                int chiSoCu = 0;

                if (laLanNhapDauTien)
                {
                    // NẾU LÀ LẦN ĐẦU TIÊN: 
                    // Chỉ số cũ sẽ chính bằng Chỉ số mới (để lượng tiêu thụ = 0)
                    chiSoCu = item.ChiSoMoi;
                    System.Diagnostics.Debug.WriteLine($"[INFO] Khách {item.KhachHangId} chốt mốc khởi điểm: {item.ChiSoMoi}");
                }
                else
                {
                    // NẾU KHÔNG PHẢI LẦN ĐẦU: Lấy số mới tháng trước làm số cũ tháng này
                    chiSoCu = last.ChiSoMoi;
                    System.Diagnostics.Debug.WriteLine($"[COMPARE] Cũ DB: {chiSoCu} | Mới nhập: {item.ChiSoMoi}");

                    // Chỉ check "nhập tụt lùi" nếu KHÔNG PHẢI lần đầu tiên
                    if (item.ChiSoMoi < chiSoCu)
                    {
                        string err = $"Khách {item.KhachHangId} nhập tụt (Mới: {item.ChiSoMoi} < Cũ: {chiSoCu})";
                        danhSachLoi.Add(err);
                        System.Diagnostics.Debug.WriteLine($"[THẤT BẠI] {err}");
                        continue;
                    }
                }

                // 4. Pass mọi bài test -> Chuẩn bị nạp vào Database
                System.Diagnostics.Debug.WriteLine($"[THÀNH CÔNG] Khách {item.KhachHangId} hợp lệ. Chuẩn bị Insert.");

                var chiSoDien = new ChiSoDien
                {
                    KhachHangId = item.KhachHangId,
                    Thang = Thang,
                    Nam = Nam,
                    ChiSoCu = chiSoCu,        // Đã được gán lại logic mượt mà
                    ChiSoMoi = item.ChiSoMoi,
                    NhanVienId = nhanVien.Id
                };

                _context.Add(chiSoDien);
                soLuongThem++;
            }

            System.Diagnostics.Debug.WriteLine($"\n[KẾT QUẢ] Đã Pass {soLuongThem} hồ sơ. Có {danhSachLoi.Count} hồ sơ bị loại.");
            System.Diagnostics.Debug.WriteLine($"========== [SYSTEM_LOG] KẾT THÚC TIẾN TRÌNH ==========\n");

            // Lưu một lượt cho tất cả những ông pass
            await _context.SaveChangesAsync();

            if (danhSachLoi.Any())
            {
                // Nhồi thẳng nội dung lỗi vào TempData để in ra màn hình
                TempData["ThongBao"] = $"Tạo thành công {soLuongThem} hồ sơ. Bị loại bỏ: " + string.Join(" | ", danhSachLoi);
            }
            else
            {
                TempData["ThongBao"] = $"Tạo thành công {soLuongThem} khách.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task RepopulateModelForView(List<ChiSoDienIndexVM> model)
        {
            foreach (var item in model)
            {
                // Lấy lại thông tin khách hàng từ DB dựa vào KhachHangId
                var khachHang = await _context.KhachHang.FindAsync(item.KhachHangId);
                if (khachHang != null)
                {
                    item.TenKh = khachHang.TenKh; // Thay bằng tên cột thực tế của bạn
                    item.DiaChi = khachHang.DiaChi;      // Thay bằng tên cột thực tế của bạn
                }

                // Lấy lại thông tin kỳ gần nhất và chỉ số cũ
                var last = await _context.ChiSoDien
                    .Where(x => x.KhachHangId == item.KhachHangId)
                    .OrderByDescending(x => x.Nam)
                    .ThenByDescending(x => x.Thang)
                    .FirstOrDefaultAsync();

                if (last != null)
                {
                    item.ThangGanNhat = last.Thang;
                    item.NamGanNhat = last.Nam;
                    item.ChiSoCu = last.ChiSoMoi;
                }
            }
        }

        // GET: ChiSoDien/Edit?khachHangId=....
        [Authorize(Roles = "Admin, NhanVien")]
        public async Task<IActionResult> Edit(string khachHangId)
        {
            if (string.IsNullOrEmpty(khachHangId))
            {
                return NotFound();
            }

            // Lấy kỳ mới nhất của khách hàng đó
            var chiSoDien = await _context.ChiSoDien
                .Where(x => x.KhachHangId == khachHangId)
                .OrderByDescending(x => x.Nam)
                .ThenByDescending(x => x.Thang)
                .FirstOrDefaultAsync();

            chiSoDien.KhachHang = await _context.KhachHang.FirstOrDefaultAsync(k => k.Id == chiSoDien.KhachHangId);

            if (chiSoDien == null)
            {
                return NotFound();
            }

            // Xóa hết mấy dòng Console và throw Exception đi
            // Chỉ để lại dòng này thôi:
            return View(chiSoDien);
        }

        // POST: ChiSoDien/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin, NhanVien")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("Id,KhachHangId,Thang,Nam,ChiSoCu,ChiSoMoi")] ChiSoDien model)
        {
            // 1. Lấy thông tin nhân viên ĐANG ĐĂNG NHẬP (Đây là định danh chính xác nhất)
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var nhanVien = await _context.NhanVien.FirstOrDefaultAsync(x => x.IdentityUserId == identityUserId);

            // 2. Nếu không tìm thấy nhân viên (chưa đăng nhập hoặc lỗi data), chặn luôn
            if (nhanVien == null)
            {
                ModelState.AddModelError("", "Không xác định được nhân viên đang thực hiện.");
                await LoadThongTinKhachHang();
                return View(model);
            }

            // --- QUAN TRỌNG: BỎ QUA LỖI VALIDATION CỦA NHÂN VIÊN ---
            ModelState.Remove("NhanVienId");
            ModelState.Remove("NhanVien"); // Remove cả object navigation nếu cần

            // Hàm hỗ trợ nạp lại tên khách (để view không bị lỗi hiển thị)
            async Task LoadThongTinKhachHang()
            {
                if (!string.IsNullOrEmpty(model.KhachHangId))
                {
                    model.KhachHang = await _context.KhachHang.FirstOrDefaultAsync(k => k.Id == model.KhachHangId);
                }
            }

            // 3. Kiểm tra các lỗi khác (lúc này lỗi NhanVienId đã biến mất)
            if (!ModelState.IsValid)
            {
                // --- ĐOẠN DEBUG CỦA BẠN ---
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n=== FORM ERROR DETECTED ===");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    foreach (var error in state.Errors)
                    {
                        Console.WriteLine($"Field: {key} - Error: {error.ErrorMessage}");
                    }
                }
                Console.ResetColor();
                // -------------------------

                await LoadThongTinKhachHang();
                return View(model);
            }

            // 4. Lấy dữ liệu cũ từ Database
            var chiSoDien = await _context.ChiSoDien.FindAsync(model.Id);
            if (chiSoDien == null) return NotFound();

            // =========================================================================
            // 🚨 Ổ KHÓA BẢO MẬT: KIỂM TRA HÓA ĐƠN TRƯỚC KHI CHO PHÉP CHỈNH SỬA 🚨
            // =========================================================================
            // (Lưu ý: Chữ 'HoaDon' sếp tự coi lại DbContext xem có thêm 's' không nhé)
            bool daLapHoaDon = await _context.HoaDon.AnyAsync(h => h.ChiSoDienId == model.Id);

            if (daLapHoaDon)
            {
                // Bắn bùa ngải lỗi ra ngoài TempData
                TempData["ThongBao"] = "CẢNH BÁO: Kỳ chỉ số này đã được xuất hóa đơn! Hệ thống từ chối ghi đè để bảo vệ tính toàn vẹn dữ liệu tài chính!";

                // Nạp lại tên khách hàng kẻo cái View nó báo lỗi NullReference
                await LoadThongTinKhachHang();

                // Đá văng ra lại giao diện cũ
                return View(model);
            }
            // =========================================================================

            // 5. Logic nghiệp vụ: Chỉ số mới >= cũ
            if (model.ChiSoMoi < chiSoDien.ChiSoCu)
            {
                ModelState.AddModelError("ChiSoMoi", "Chỉ số mới phải >= chỉ số cũ.");
                await LoadThongTinKhachHang();
                return View(model);
            }

            // 6. CẬP NHẬT DỮ LIỆU VÀ GHI VẾT NGƯỜI SỬA
            chiSoDien.ChiSoMoi = model.ChiSoMoi;

            // ĐÂY LÀ CHỖ QUAN TRỌNG NHẤT:
            chiSoDien.NhanVienId = nhanVien.Id;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin, NhanVien")]
        // GET: ChiSoDien/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chiSoDien = await _context.ChiSoDien
                .FirstOrDefaultAsync(m => m.Id == id);
            if (chiSoDien == null)
            {
                return NotFound();
            }

            return View(chiSoDien);
        }

        // POST: ChiSoDien/Delete/5
        [Authorize(Roles = "Admin, NhanVien")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var chiSoDien = await _context.ChiSoDien.FindAsync(id);
            if (chiSoDien != null)
            {
                _context.ChiSoDien.Remove(chiSoDien);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ChiSoDienExists(string id)
        {
            return _context.ChiSoDien.Any(e => e.Id == id);
        }

        [Authorize(Roles = "KhachHang")]
        public async Task<IActionResult> LichSuCuaToi()
        {
            // 1. Lấy thông tin user hiện tại đang đăng nhập
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // 2. Tìm thông tin Khách hàng tương ứng (Dùng Include nếu cần lấy thêm dữ liệu liên quan)
            var khachHang = await _context.KhachHang
                .FirstOrDefaultAsync(k => k.IdentityUserId == user.Id);

            if (khachHang == null) return NotFound();

            // 3. Gán luôn thông tin vào ViewBag từ biến khachHang vừa tìm được
            // Không cần dùng cái tham số userId truyền vào nữa cho rắc rối sếp nhé
            ViewBag.TenKhachHang = khachHang.TenKh;
            ViewBag.MaKhachHang = khachHang.MaKh;
            ViewBag.DiaChi = khachHang.DiaChiDayDu;

            // 4. Query lấy danh sách chỉ số điện
            var lichSuDien = await _context.ChiSoDien
                .Where(c => c.KhachHangId == khachHang.Id)
                .OrderByDescending(c => c.Nam)
                .ThenByDescending(c => c.Thang)
                .ToListAsync();

            return View(lichSuDien);
        }
    }
}
