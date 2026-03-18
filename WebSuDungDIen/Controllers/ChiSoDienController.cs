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

        public async Task<IActionResult> Index(string maTinh, string maPhuongApi, string searchKeyword)
        {
            // 1. Khởi tạo Query từ Context của bạn
            // MongoDB LINQ hỗ trợ rất tốt việc kết hợp Filter và Query
            var query = _context.KhachHang.AsQueryable();

            // 2. Lọc theo Tỉnh (Dựa trên chuỗi địa chỉ)
            if (!string.IsNullOrEmpty(maTinh))
            {
                query = query.Where(k => k.DiaChiDayDu.Contains(maTinh));
            }

            // 3. Lọc theo Phường
            if (!string.IsNullOrEmpty(maPhuongApi))
            {
                query = query.Where(k => k.MaPhuongApi == maPhuongApi);
            }

            // 4. LỌC THEO TỪ KHÓA (FULL-TEXT SEARCH)
            // Thay vì dùng _khachHangCollection, ta dùng cú pháp lọc trực tiếp trên Query
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                // Lưu ý: Để dòng này chạy được, bạn vẫn phải tạo Text Index trong MongoDB Compass như tôi hướng dẫn ở trên
                var filter = Builders<KhachHang>.Filter.Text(searchKeyword);

                // Chuyển đổi từ Filter sang Queryable
                // Nếu _context.KhachHang là IMongoCollection thì dùng lệnh này:
                // query = _context.KhachHang.Find(filter).AsQueryable();

                // Hoặc đơn giản hơn nếu bạn muốn tìm kiếm cơ bản (không cần Index phức tạp):
                query = query.Where(k => k.TenKh.Contains(searchKeyword) ||
                                         k.MaKh.Contains(searchKeyword) ||
                                         k.DienThoai.Contains(searchKeyword));
            }

            // --- GIỮ NGUYÊN PHẦN LOGIC ĐỔ DATA VÀO DROPDOWN CỦA BẠN ---
            var rootKhachHang = await _context.KhachHang
                .Where(k => !string.IsNullOrEmpty(k.MaPhuongApi) && !string.IsNullOrEmpty(k.DiaChiDayDu))
                .ToListAsync();

            ViewBag.DanhSachTinh = rootKhachHang
                .Select(k => k.DiaChiDayDu.Split(',').Last().Trim())
                .Distinct()
                .Select(t => new SelectListItem { Value = t, Text = t, Selected = (t == maTinh) }).ToList();

            ViewBag.DanhSachPhuong = rootKhachHang
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

            ViewBag.SearchKeyword = searchKeyword;
            ViewBag.MaPhuongApi = maPhuongApi;

            // --- ĐỔ DỮ LIỆU RA VIEWMODEL ---
            var data = await query.Select(k => new ChiSoDienIndexVM
            {
                KhachHangId = k.Id,
                TenKh = k.TenKh,
                DiaChi = k.DiaChiDayDu ?? k.DiaChi,
                ThangGanNhat = k.ChiSoDien.OrderByDescending(c => c.Nam).ThenByDescending(c => c.Thang).Select(c => c.Thang).FirstOrDefault(),
                NamGanNhat = k.ChiSoDien.OrderByDescending(c => c.Nam).ThenByDescending(c => c.Thang).Select(c => c.Nam).FirstOrDefault(),
                ChiSoCu = k.ChiSoDien.OrderByDescending(c => c.Nam).ThenByDescending(c => c.Thang).Select(c => c.ChiSoCu).FirstOrDefault(),
                ChiSoMoi = k.ChiSoDien.OrderByDescending(c => c.Nam).ThenByDescending(c => c.Thang).Select(c => c.ChiSoMoi).FirstOrDefault(),
                DienTieuThu = k.ChiSoDien.OrderByDescending(c => c.Nam).ThenByDescending(c => c.Thang).Select(c => c.ChiSoMoi - c.ChiSoCu).FirstOrDefault()
            }).ToListAsync();

            return View(data);
        }

        // GET: ChiSoDien/Details/5
        public async Task<IActionResult> Details(string id)
        {
            var kh = await _context.KhachHang
                .Include(x => x.ChiSoDien)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (kh == null)
            {
                return NotFound();
            }

            return View(kh);
        }

        // GET: ChiSoDien/Create
        public IActionResult Create(string maPhuongApi)
        {
            // Nếu chưa chọn phường (chuỗi rỗng) thì đá về Index bắt chọn lại
            if (string.IsNullOrEmpty(maPhuongApi))
            {
                TempData["ThongBao"] = "Vui lòng chọn Phường/Xã trước khi nhập chỉ số điện.";
                return RedirectToAction("Index");
            }

            var ds = _context.KhachHang
                .Where(kh => kh.MaPhuongApi == maPhuongApi)
                .Select(kh => new ChiSoDienIndexVM
                {
                    KhachHangId = kh.Id,
                    TenKh = kh.TenKh,
                    DiaChi = kh.DiaChiDayDu ?? kh.DiaChi, // Ưu tiên show địa chỉ đầy đủ

                    ChiSoCu = _context.ChiSoDien
                        .Where(cs => cs.KhachHangId == kh.Id)
                        .OrderByDescending(cs => cs.Nam)
                        .ThenByDescending(cs => cs.Thang)
                        .Select(cs => cs.ChiSoMoi) // Chỉ số mới của tháng trước = Chỉ số cũ tháng này
                        .FirstOrDefault(),

                    ThangGanNhat = _context.ChiSoDien
                        .Where(cs => cs.KhachHangId == kh.Id)
                        .OrderByDescending(cs => cs.Nam)
                        .ThenByDescending(cs => cs.Thang)
                        .Select(cs => cs.Thang)
                        .FirstOrDefault(),

                    NamGanNhat = _context.ChiSoDien
                        .Where(cs => cs.KhachHangId == kh.Id)
                        .OrderByDescending(cs => cs.Nam)
                        .ThenByDescending(cs => cs.Thang)
                        .Select(cs => cs.Nam)
                        .FirstOrDefault()
                })
                .ToList();

            if (!ds.Any())
            {
                TempData["ThongBao"] = "Phường này hiện chưa có khách hàng nào để ghi điện.";
                return RedirectToAction("Index");
            }

            // Gửi cái mã phường này sang View để lát nữa lúc bấm [LƯU], form POST còn biết đang lưu cho phường nào
            ViewBag.MaPhuongApi = maPhuongApi;

            return View(ds);
        }

        // POST: ChiSoDien/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
            // Vì Form không gửi NhanVienId lên (để bảo mật), nên Model sẽ báo lỗi thiếu trường này.
            // Ta dùng lệnh này để bảo hệ thống: "Đừng lo, tí nữa Server sẽ tự điền ID nhân viên".
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
            // Gán ID của nhân viên đang đăng nhập vào bản ghi.
            // Dù ai có táy máy F12 sửa HTML thì dòng này vẫn lấy đúng người đang đăng nhập.
            chiSoDien.NhanVienId = nhanVien.Id;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

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
            // 1. Lấy thông tin user hiện tại
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // 2. Tìm mã khách hàng tương ứng với tài khoản này
            var khachHang = await _context.KhachHang
                .FirstOrDefaultAsync(k => k.IdentityUserId == user.Id);

            if (khachHang == null) return NotFound();

            // 3. Query lấy danh sách chỉ số điện (ĐÃ FIX THEO MODEL)
            var lichSuDien = await _context.ChiSoDien
                .Where(c => c.KhachHangId == khachHang.Id)
                .OrderByDescending(c => c.Nam)    // Ưu tiên sắp xếp theo Năm mới nhất
                .ThenByDescending(c => c.Thang)  // Cùng năm thì sắp xếp theo Tháng mới nhất
                .ToListAsync();

            // Truyền thêm tên khách hàng qua ViewBag để lỡ ngoài View muốn dùng để hiển thị "Xin chào, Nguyễn Văn A"
            ViewBag.TenKhachHang = khachHang.TenKh; // Thay bằng thuộc tính tên thật trong Model KhachHang của bạn

            // 4. Trả về View dành riêng cho khách
            return View(lichSuDien);
        }
    }
}
