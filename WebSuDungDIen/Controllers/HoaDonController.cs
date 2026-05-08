using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using WebSuDungDIen.Data;
using WebSuDungDIen.Models;
using WebSuDungDIen.Services;
using WebSuDungDIen.ViewModels;

namespace WebSuDungDIen.Controllers
{
    public class HoaDonController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMongoCollection<HoaDon> _hoaDonCollection;
        private readonly IMongoCollection<ChiSoDien> _chiSoDienCollection;
        private readonly TaoMaService _taoMaService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMongoArchiveService _mongoService;

        public HoaDonController(ApplicationDbContext context, IMongoClient mongoClient, TaoMaService taoMaService, UserManager<ApplicationUser> userManager, IMongoArchiveService mongoService)
        {
            var database = mongoClient.GetDatabase("Cluster0");
            _chiSoDienCollection = database.GetCollection<ChiSoDien>("ChiSoDien");
            _hoaDonCollection = database.GetCollection<HoaDon>("HoaDon");
            _context = context;
            _taoMaService = taoMaService;
            _userManager = userManager;
            _mongoService = mongoService;
        }

        [HttpGet]
        public async Task<IActionResult> ExportMongoFormatToExcel()
        {
            var query = from hd in _context.HoaDon
                        join kh in _context.KhachHang on hd.KhachHangId equals kh.Id into khGroup
                        from kh in khGroup.DefaultIfEmpty()
                        join csd in _context.ChiSoDien on hd.ChiSoDienId equals csd.Id into csdGroup
                        from csd in csdGroup.DefaultIfEmpty()
                        join nv in _context.NhanVien on hd.NhanVienId equals nv.Id into nvGroup
                        from nv in nvGroup.DefaultIfEmpty()
                        select new { HoaDon = hd, KhachHang = kh, ChiSoDien = csd, NhanVien = nv };

            var sqlData = await query.ToListAsync();

            var exportData = sqlData.Select(item => new
            {
                MaHd = item.HoaDon.MaHd,
                NgayLap = item.HoaDon.NgayLap ?? DateTime.Now,

                TienDien = item.HoaDon.TienDien, // [ BỔ SUNG ] - Tiền điện trước thuế
                PhanTramVAT = item.HoaDon.PhanTramVAT,
                ThueVAT = item.HoaDon.ThueVAT,
                TongThanhToan = item.HoaDon.TongThanhToan,
                TrangThai = item.HoaDon.TrangThai,

                MaKh = item.KhachHang?.MaKh,
                TenKh = item.KhachHang?.TenKh,
                DienThoai = item.KhachHang?.DienThoai,

                ChiSoCu = item.ChiSoDien?.ChiSoCu,
                ChiSoMoi = item.ChiSoDien?.ChiSoMoi,
                TieuThu = item.HoaDon.SoDienTieuThu,

                MaNhanVien = item.NhanVien?.MaNV,
                TenNhanVien = item.NhanVien?.TenNV
            }).ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("DuLieu_ChuanHoa");

                // [ BỔ SUNG ] - Thêm "Tiền Điện" vào mảng Header (Nằm ở vị trí Cột 9)
                string[] headers = { "Mã Hóa Đơn", "Ngày Lập", "Mã Khách Hàng", "Tên Khách Hàng", "Điện Thoại",
                             "Chỉ Số Cũ", "Chỉ Số Mới", "Tiêu Thụ", "Tiền Điện", "% Thuế VAT", "Tiền Thuế",
                             "Mã Nhân Viên", "Tên Nhân Viên", "Tổng Tiền", "Trạng Thái" };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                int currentRow = 2;
                foreach (var doc in exportData)
                {
                    worksheet.Cell(currentRow, 1).Value = doc.MaHd;
                    worksheet.Cell(currentRow, 2).Value = doc.NgayLap.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cell(currentRow, 3).Value = doc.MaKh;
                    worksheet.Cell(currentRow, 4).Value = doc.TenKh;
                    worksheet.Cell(currentRow, 5).Value = doc.DienThoai;
                    worksheet.Cell(currentRow, 6).Value = doc.ChiSoCu;
                    worksheet.Cell(currentRow, 7).Value = doc.ChiSoMoi;
                    worksheet.Cell(currentRow, 8).Value = doc.TieuThu;

                    // [ BỔ SUNG ] - Ghi Tiền Điện vào Cột 9
                    worksheet.Cell(currentRow, 9).Value = doc.TienDien;
                    worksheet.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0";

                    // Dịch toàn bộ các cột sau đó đi 1 nấc (10, 11, 12...)
                    worksheet.Cell(currentRow, 10).Value = doc.PhanTramVAT;

                    worksheet.Cell(currentRow, 11).Value = doc.ThueVAT;
                    worksheet.Cell(currentRow, 11).Style.NumberFormat.Format = "#,##0";

                    worksheet.Cell(currentRow, 12).Value = doc.MaNhanVien;
                    worksheet.Cell(currentRow, 13).Value = doc.TenNhanVien;

                    worksheet.Cell(currentRow, 14).Value = doc.TongThanhToan;
                    worksheet.Cell(currentRow, 14).Style.NumberFormat.Format = "#,##0";

                    worksheet.Cell(currentRow, 15).Value = doc.TrangThai == "DaThanhToan" ? "Đã thanh toán" : "Chưa thanh toán";

                    currentRow++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "HoaDon_TongHop.xlsx");
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> ImportFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0) return RedirectToAction("Index");

            int countImport = 0;

            var currentUserId = _userManager.GetUserId(User);
            var currentNhanVien = await _context.NhanVien.FirstOrDefaultAsync(nv => nv.IdentityUserId == currentUserId);
            string defaultNhanVienId = currentNhanVien?.Id ?? string.Empty;

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                    var donGiaHienTai = await _context.DonGiaDien
                                  .OrderByDescending(d => d.Id)
                                  .FirstOrDefaultAsync();

                    foreach (var row in rows)
                    {
                        string maHd = row.Cell(1).GetString().Trim();
                        if (string.IsNullOrEmpty(maHd)) continue;

                        var exists = await _context.HoaDon.AnyAsync(h => h.MaHd == maHd);
                        if (exists) continue;

                        string dateString = row.Cell(2).GetString().Trim();
                        DateTime ngayLap = DateTime.MinValue;

                        // Đưa ra một danh sách các định dạng chuẩn Việt Nam để hệ thống dò tìm
                        string[] expectedFormats = { "dd/MM/yyyy HH:mm", "dd/MM/yyyy", "d/M/yyyy HH:mm", "d/M/yyyy" };
                        if (!DateTime.TryParseExact(dateString, expectedFormats, new System.Globalization.CultureInfo("vi-VN"), System.Globalization.DateTimeStyles.None, out ngayLap))
                        {
                            // Nếu nó dùng định dạng Excel gốc (số Serial Date của Excel)
                            if (double.TryParse(dateString, out double excelDate))
                            {
                                ngayLap = DateTime.FromOADate(excelDate);
                            }
                        }
                        string maKh = row.Cell(3).GetString().Trim();
                        string tenKh = row.Cell(4).GetString().Trim();
                        string dienThoai = row.Cell(5).GetString().Trim();
                        int.TryParse(row.Cell(6).GetString(), out int chiSoCu);
                        int.TryParse(row.Cell(7).GetString(), out int chiSoMoi);
                        int.TryParse(row.Cell(8).GetString(), out int tieuThu);

                        // [ BỔ SUNG & CẬP NHẬT CỘT ]
                        decimal.TryParse(row.Cell(9).GetString(), out decimal tienDienTruocThue); // Cột 9: Tiền Điện
                        decimal.TryParse(row.Cell(10).GetString(), out decimal phanTramVat);      // Cột 10: % VAT
                        decimal.TryParse(row.Cell(11).GetString(), out decimal tienVat);          // Cột 11: Tiền VAT
                        string maNhanVienExcel = row.Cell(12).GetString().Trim();                 // Cột 12: Mã Nhân Viên
                                                                                                  // Bỏ qua cột 13 vì nó là Tên Nhân Viên
                        decimal.TryParse(row.Cell(14).GetString(), out decimal tongTien);         // Cột 14: Tổng Tiền
                        string trangThai = row.Cell(15).GetString().Trim() == "Đã thanh toán" ? "DaThanhToan" : "ChuaThanhToan"; // Cột 15

                        string finalNhanVienId = defaultNhanVienId;

                        if (!string.IsNullOrEmpty(maNhanVienExcel))
                        {
                            var nhanVienDb = await _context.NhanVien.FirstOrDefaultAsync(nv => nv.MaNV == maNhanVienExcel);
                            if (nhanVienDb != null)
                            {
                                finalNhanVienId = nhanVienDb.Id;
                            }
                        }

                        if (string.IsNullOrEmpty(finalNhanVienId)) continue;

                        // --- 1. Xử lý Khách Hàng ---
                        var khachHang = await _context.KhachHang.FirstOrDefaultAsync(k => k.MaKh == maKh);
                        if (khachHang == null && !string.IsNullOrEmpty(maKh))
                        {
                            khachHang = new KhachHang { Id = Guid.NewGuid().ToString(), MaKh = maKh, TenKh = tenKh, DienThoai = dienThoai };
                            _context.KhachHang.Add(khachHang);
                            await _context.SaveChangesAsync();
                        }

                        // --- 2. Xử lý Chỉ Số Điện ---
                        int thangExcel = ngayLap != DateTime.MinValue ? ngayLap.Month : DateTime.Now.Month;
                        int namExcel = ngayLap != DateTime.MinValue ? ngayLap.Year : DateTime.Now.Year;

                        // Tìm xem Chỉ số điện của khách hàng này trong tháng/năm đó đã tồn tại chưa
                        var chiSoDien = await _context.ChiSoDien.FirstOrDefaultAsync(c =>
                            c.KhachHangId == khachHang.Id &&
                            c.Thang == thangExcel &&
                            c.Nam == namExcel);

                        // Nếu chưa có, tiến hành tạo mới
                        if (chiSoDien == null)
                        {
                            chiSoDien = new ChiSoDien
                            {
                                Id = Guid.NewGuid().ToString(),
                                KhachHangId = khachHang?.Id,
                                ChiSoCu = chiSoCu,
                                ChiSoMoi = chiSoMoi,
                                NhanVienId = finalNhanVienId,
                                Thang = thangExcel,
                                Nam = namExcel
                            };
                            _context.ChiSoDien.Add(chiSoDien);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            // Nếu đã có, cập nhật lại chỉ số (phòng trường hợp người dùng sửa số trong Excel)
                            chiSoDien.ChiSoCu = chiSoCu;
                            chiSoDien.ChiSoMoi = chiSoMoi;
                            chiSoDien.NhanVienId = finalNhanVienId;
                            _context.ChiSoDien.Update(chiSoDien);
                            await _context.SaveChangesAsync();
                        }

                        var newHd = new HoaDon
                        {
                            Id = Guid.NewGuid().ToString(),
                            MaHd = maHd,
                            DonGiaId = donGiaHienTai.Id,
                            NgayLap = ngayLap == DateTime.MinValue ? DateTime.Now : ngayLap,
                            KhachHangId = khachHang?.Id,
                            ChiSoDienId = chiSoDien.Id,
                            NhanVienId = finalNhanVienId,
                            SoDienTieuThu = tieuThu,

                            TienDien = tienDienTruocThue, // [ BỔ SUNG ] - Nạp Tiền điện trước thuế vào Database

                            PhanTramVAT = phanTramVat,
                            ThueVAT = tienVat,
                            TongThanhToan = tongTien,
                            TrangThai = trangThai
                        };

                        _context.HoaDon.Add(newHd);
                        countImport++;
                    }
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = $"Đã dung nạp thành công {countImport} hóa đơn!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Index(string maTinh, string maPhuongApi, int? thang, int? nam)
        {
            // ====================================================================
            // 💥 1. LẤY MÃ PHƯỜNG VÀ TÊN TỈNH ĐÃ CÓ HÓA ĐƠN
            // ====================================================================

            // Móc dữ liệu Khách hàng ĐÃ CÓ Hóa đơn lên RAM
            var dsKhachHangCoHoaDon = await (from hd in _context.HoaDon
                                             join kh in _context.KhachHang on hd.KhachHangId equals kh.Id.ToString()
                                             where !string.IsNullOrEmpty(kh.MaPhuongApi) && !string.IsNullOrEmpty(kh.DiaChiDayDu)
                                             select new { kh.MaPhuongApi, kh.DiaChiDayDu })
                                             .Distinct()
                                             .ToListAsync();

            // Lấy mảng Mã Phường
            var phuongCoData = dsKhachHangCoHoaDon.Select(x => x.MaPhuongApi).Distinct().ToList();

            // 💥 TÀ THUẬT: Cắt phần tử cuối cùng sau dấu phẩy để lấy TÊN TỈNH
            var tinhCoData = dsKhachHangCoHoaDon
                .Select(x => x.DiaChiDayDu.Split(',').LastOrDefault()?.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .ToList();

            ViewBag.DanhSachPhuongCoData = phuongCoData;
            ViewBag.DanhSachTinhCoData = tinhCoData; // Bơm đạn mới cho Javascipt!

            ViewBag.CurrentTinh = maTinh;
            ViewBag.CurrentPhuong = maPhuongApi;

            // ====================================================================
            // 2. TẠO DROPDOWN THÁNG / NĂM (Giữ nguyên)
            // ====================================================================
            var existingDates = await (from hd in _context.HoaDon
                                       join cs in _context.ChiSoDien on hd.ChiSoDienId equals cs.Id.ToString()
                                       select new { cs.Thang, cs.Nam })
                                       .Distinct()
                                       .ToListAsync();

            var listThang = existingDates.Select(x => x.Thang).Distinct().OrderBy(x => x)
                                         .Select(x => new { Value = x, Text = "Tháng " + x }).ToList();
            var listNam = existingDates.Select(x => x.Nam).Distinct().OrderByDescending(x => x)
                                       .Select(x => new { Value = x, Text = "Năm " + x }).ToList();

            ViewBag.ThangList = new SelectList(listThang, "Value", "Text", thang);
            ViewBag.NamList = new SelectList(listNam, "Value", "Text", nam);
            ViewBag.CurrentThang = thang;
            ViewBag.CurrentNam = nam;

            // ====================================================================
            // 3. TRUY VẤN VÀ LỌC DỮ LIỆU
            // ====================================================================
            var query = from hd in _context.HoaDon
                        join cs in _context.ChiSoDien on hd.ChiSoDienId equals cs.Id.ToString()
                        join kh in _context.KhachHang on hd.KhachHangId equals kh.Id.ToString()
                        select new { hd, cs, kh };

            if (!string.IsNullOrEmpty(maTinh)) query = query.Where(x => x.kh.DiaChiDayDu.Contains(maTinh));
            if (!string.IsNullOrEmpty(maPhuongApi)) query = query.Where(x => x.kh.MaPhuongApi == maPhuongApi);
            if (thang.HasValue) query = query.Where(x => x.cs.Thang == thang.Value);
            if (nam.HasValue) query = query.Where(x => x.cs.Nam == nam.Value);

            var result = await query
                .OrderByDescending(x => x.cs.Nam)
                .ThenByDescending(x => x.cs.Thang)
                .ThenByDescending(x => x.hd.MaHd)
                .Select(x => new HoaDonIndexVM
                {
                    Id = x.hd.Id,
                    MaHd = x.hd.MaHd,
                    TenKhachHang = x.kh.TenKh,
                    DiaChi = x.kh.DiaChiDayDu ?? x.kh.DiaChi,
                    Thang = x.cs.Thang,
                    Nam = x.cs.Nam,
                    SoDienTieuThu = x.hd.SoDienTieuThu,
                    TongThanhToan = x.hd.TongThanhToan,
                    TrangThai = x.hd.TrangThai == "DaThanhToan" || x.hd.TrangThai == "Đã thanh toán",
                    NgayThanhToan = x.hd.NgayThanhToan
                }).ToListAsync();

            return View(result);
        }

        // GET: HoaDon/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var hoaDon = await _context.HoaDon.FirstOrDefaultAsync(m => m.Id == id);
            if (hoaDon == null) return NotFound();

            var kh = await _context.KhachHang.FirstOrDefaultAsync(k => k.Id == hoaDon.KhachHangId);
            var nv = await _context.NhanVien.FirstOrDefaultAsync(n => n.Id == hoaDon.NhanVienId);

            // 1. Lấy thông tin Chỉ Số Điện
            var chiSo = await _context.ChiSoDien.FirstOrDefaultAsync(c => c.Id == hoaDon.ChiSoDienId);

            // =========================================================
            // 🔥 SỬA LỖI TẠI ĐÂY: Truy vết đúng bộ 6 bậc giá của hóa đơn
            // =========================================================
            var donGiaNeo = await _context.DonGiaDien.FirstOrDefaultAsync(d => d.Id == hoaDon.DonGiaId);
            var bangGia = new List<DonGiaDien>();

            if (donGiaNeo != null)
            {
                // Chỉ lấy 6 bậc có cùng "Ngày Tạo" (Cùng 1 bộ) với cái đơn giá neo
                bangGia = await _context.DonGiaDien
                    .Where(d => d.NgayTao == donGiaNeo.NgayTao && d.Bac > 0)
                    .OrderBy(g => g.Bac)
                    .ToListAsync();
            }

            ViewBag.ThongTinKhach = kh != null ? $"{kh.TenKh} - {kh.MaKh}" : "Không tìm thấy thông tin";
            ViewBag.ThongTinNhanVien = nv != null ? nv.TenNV : "Hệ thống";

            // Đẩy dữ liệu tính toán sang View
            ViewBag.ChiSoCu = chiSo != null ? chiSo.ChiSoCu : 0;
            ViewBag.ChiSoMoi = chiSo != null ? chiSo.ChiSoMoi : 0;
            ViewBag.BangGia = bangGia;
            ViewBag.NgayLap = hoaDon.NgayLap;

            return View(hoaDon);
        }

        [HttpGet]
        public async Task<IActionResult> GetChiSoDienByKhach(string khachId)
        {
            var chiSo = await _context.ChiSoDien
                .Where(x => x.KhachHangId == khachId)
                .OrderByDescending(x => x.Nam)
                .ThenByDescending(x => x.Thang)
                .FirstOrDefaultAsync();

            if (chiSo == null)
                return Json(0);

            int soDien = chiSo.ChiSoMoi - chiSo.ChiSoCu;
            if (soDien < 0)
                soDien = 0;
            return Json(soDien);
        }

        [HttpGet]
        public async Task<IActionResult> GetKhachByPhuong(string maPhuongApi)
        {
            // 1. Lấy tập Khách Hàng (Đang Active)
            var dsKhach = await _context.KhachHang
                .Where(k => k.MaPhuongApi == maPhuongApi && k.TrangThai == true)
                .Select(k => new { k.Id, k.MaKh, k.TenKh })
                .ToListAsync();

            var khachIds = dsKhach.Select(k => k.Id).ToList();
            if (!khachIds.Any()) return Json(new List<object>());

            // ==========================================================
            // 2. SỬA LỖI 1: TÌM THÁNG MỚI NHẤT (ĐỒNG BỘ VỚI HÀM CREATE)
            // ==========================================================
            var thangMoiNhat = await _context.ChiSoDien
                .Where(x => khachIds.Contains(x.KhachHangId))
                .OrderByDescending(x => x.Nam)
                .ThenByDescending(x => x.Thang)
                .Select(x => new { x.Thang, x.Nam })
                .FirstOrDefaultAsync();

            // Nếu khu vực này chưa có ai ghi điện thì trả về rỗng
            if (thangMoiNhat == null) return Json(new List<object>());

            // 3. Lấy Chỉ Số Điện của THÁNG MỚI NHẤT (thay vì DateTime.Now)
            var dsChiSoThangNay = await _context.ChiSoDien
                .Where(c => khachIds.Contains(c.KhachHangId) && c.Thang == thangMoiNhat.Thang && c.Nam == thangMoiNhat.Nam)
                .ToListAsync();

            // 4. Lấy danh sách Hóa Đơn của các khách hàng này
            var dsHoaDon = await _context.HoaDon
                .Where(hd => khachIds.Contains(hd.KhachHangId))
                .Select(hd => hd.ChiSoDienId)
                .ToListAsync();

            var chiSoDienIdsDaLap = new HashSet<string>(dsHoaDon);

            // 5. Trả kết quả về giao diện
            var result = new List<object>();
            foreach (var k in dsKhach)
            {
                var chiSo = dsChiSoThangNay.FirstOrDefault(c => c.KhachHangId == k.Id);

                // ==========================================================
                // 6. SỬA LỖI 2: ÉP KIỂU Tostring() ĐỂ HASHSET SOI ĐÚNG CHUẨN
                // ==========================================================
                bool daLapHoaDon = chiSo != null && chiSoDienIdsDaLap.Contains(chiSo.Id.ToString());

                result.Add(new
                {
                    id = k.Id,
                    ten = k.MaKh + " - " + k.TenKh,
                    chiSo = chiSo != null ? (int?)(chiSo.ChiSoMoi - chiSo.ChiSoCu) : null,
                    daLapHoaDon = daLapHoaDon
                });
            }

            return Json(result);
        }
        // GET: HoaDon/Create
        public async Task<IActionResult> Create()
        {
            // Bước 1: Lấy dữ liệu thô từ Database về bộ nhớ 
            var khachHangTho = await _context.KhachHang
                .Where(k => !string.IsNullOrEmpty(k.MaPhuongApi) && !string.IsNullOrEmpty(k.DiaChiDayDu))
                .Select(k => new { k.MaPhuongApi, k.DiaChiDayDu })
                .ToListAsync();

            // Bước 2: Xử lý chuỗi và GroupBy bằng C# trên RAM
            var danhSachPhuong = khachHangTho
                .GroupBy(k => k.MaPhuongApi)
                .Select(g => {
                    var parts = g.First().DiaChiDayDu.Split(',');
                    string tenPhuong = parts.Length >= 2 ? parts[parts.Length - 2].Trim() : "Phường " + g.Key;

                    return new SelectListItem
                    {
                        Value = g.Key,
                        Text = tenPhuong
                    };
                }).ToList();

            ViewBag.DanhSachPhuong = danhSachPhuong;

            var model = new LapHoaDonTheoPhuongVM();

            // ==========================================
            // TUYỆT CHIÊU TRỊ TRÙNG LẶP: MỖI BẬC CHỈ LẤY 1 DÒNG MỚI NHẤT
            // ==========================================
            // Lấy hết lên RAM để xử lý mượt mà, không lo EF Core báo lỗi dịch SQL
            var allGiaTho = await _context.DonGiaDien.ToListAsync();

            var bangGia = allGiaTho
                .GroupBy(x => x.Bac) // Nhóm lại: Bậc 1 ra 1 nhóm, Bậc 2 ra 1 nhóm...
                .Select(g => g.OrderByDescending(x => x.NgayTao).First()) // Trong mỗi nhóm, bốc đúng 1 thằng có ngày tạo mới nhất
                .OrderBy(x => x.Bac) // Sắp xếp lại từ Bậc 0 đến 6
                .ToList();
            // ==========================================

            // 1. Tách Thuế VAT (Bac == 0) để gán vào ô Input
            var thueVAT = bangGia.FirstOrDefault(x => x.Bac == 0);
            model.PhanTramVAT = thueVAT != null ? thueVAT.Gia : 8; // Mặc định 8 nếu chưa có

            // 2. Tách Giá Điện Bậc Thang (Bac > 0) để hiện ra bảng
            model.DanhSachGia = bangGia
                .Where(x => x.Bac > 0)
                .Select(x => new DonGiaBacVM
                {
                    Bac = x.Bac,
                    Gia = x.Gia
                }).ToList();

            // Nếu db chưa có giá điện nào thì tạo mặc định 6 bậc trống
            if (model.DanhSachGia.Count == 0)
            {
                for (int i = 1; i <= 6; i++)
                {
                    model.DanhSachGia.Add(new DonGiaBacVM { Bac = i, Gia = 0, GioiHan = 0 });
                }
            }

            return View(model);
        }

        private decimal TinhTienDienBacThang(int soDien, List<DonGiaBacVM> danhSachGia)
        {
            int[] mucBac = { 50, 50, 100, 100, 100, int.MaxValue };

            decimal tongTien = 0;
            int dienConLai = soDien;

            for (int i = 0; i < danhSachGia.Count; i++)
            {
                if (dienConLai <= 0)
                    break;

                int dienTinhTrongBac = Math.Min(dienConLai, mucBac[i]);

                tongTien += dienTinhTrongBac * danhSachGia[i].Gia;

                dienConLai -= dienTinhTrongBac;
            }

            return tongTien;
        }

        [HttpPost]
        public IActionResult TinhTienTest([FromBody] TinhTienRequest model)
        {
            var tong = TinhTienDienBacThang(model.SoDien, model.DsGia);
            return Json(tong);
        }

        public class TinhTienRequest
        {
            public int SoDien { get; set; }
            public List<DonGiaBacVM> DsGia { get; set; }
        }
        private async Task<int> LuuBangGiaAsync(List<DonGiaBacVM> dsGia, decimal phanTramVAT)
        {
            var now = DateTime.Now;

            // 1. Lấy bộ đơn giá mới nhất hiện có trong DB để so sánh
            var latestGroup = await _context.DonGiaDien
                .OrderByDescending(x => x.NgayTao)
                .ToListAsync();

            // Lấy mốc thời gian của lần nhập gần nhất
            var latestTimestamp = latestGroup.FirstOrDefault()?.NgayTao;
            var lastSet = latestGroup.Where(x => x.NgayTao == latestTimestamp).ToList();

            // 2. Kiểm tra xem giá người dùng vừa nhập có khác gì so với giá cũ không
            bool coThayDoi = false;

            // So sánh 6 bậc giá
            foreach (var bac in dsGia)
            {
                var giaCu = lastSet.FirstOrDefault(x => x.Bac == bac.Bac)?.Gia ?? -1;
                if (bac.Gia != giaCu) { coThayDoi = true; break; }
            }

            // So sánh thuế VAT (Bậc 0)
            var vatCu = lastSet.FirstOrDefault(x => x.Bac == 0)?.Gia ?? -1;
            if (phanTramVAT != vatCu) coThayDoi = true;

            // 3. XỬ LÝ LƯU
            if (coThayDoi || !lastSet.Any())
            {
                // Nếu có thay đổi hoặc DB đang trống -> ĐẺ THÊM BỘ MỚI (Timestamp mới)
                var listMoi = new List<DonGiaDien>();

                // Thêm 6 bậc điện
                foreach (var bac in dsGia)
                {
                    listMoi.Add(new DonGiaDien
                    {
                        Bac = bac.Bac,
                        Gia = bac.Gia,
                        NgayTao = now
                    });
                }

                // Thêm dòng Thuế VAT (Bậc 0)
                listMoi.Add(new DonGiaDien
                {
                    Bac = 0,
                    Gia = phanTramVAT,
                    NgayTao = now
                });

                _context.DonGiaDien.AddRange(listMoi);
                await _context.SaveChangesAsync();

                // Trả về ID của dòng Bậc 1 mới tạo
                return listMoi.First(x => x.Bac == 1).Id;
            }
            else
            {
                // Nếu giá y hệt cũ -> Không lưu gì cả, chỉ lấy ID của Bậc 1 hiện tại trả về
                return lastSet.First(x => x.Bac == 1).Id;
            }
        }

        // POST: HoaDon/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LapHoaDonTheoPhuongVM model)
        {
            // === 1. LẤY THÔNG TIN NHÂN VIÊN ĐANG ĐĂNG NHẬP ===
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var nhanVien = await _context.NhanVien
                .FirstOrDefaultAsync(x => x.IdentityUserId == identityUserId);

            if (nhanVien == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin nhân viên thực hiện. Vui lòng đăng nhập lại.";
                return RedirectToAction(nameof(Index));
            }
            // =================================================

            // Kiểm tra an toàn: Nếu user chưa chọn phường
            if (string.IsNullOrEmpty(model.MaPhuongApi))
            {
                TempData["Error"] = "Vui lòng chọn Phường/Xã trước khi lập hóa đơn.";
                return RedirectToAction(nameof(Index));
            }

            model.DanhSachGia ??= new List<DonGiaBacVM>();
            var now = DateTime.Now;
            var danhSachHoaDonMoi = new List<HoaDon>();

            try
            {
                Console.WriteLine($"\n--- BẮT ĐẦU QUÁ TRÌNH LẬP HÓA ĐƠN CHO PHƯỜNG MÃ: {model.MaPhuongApi} ---");

                // 1. Lấy ra TẤT CẢ mã khách hàng thuộc Phường này (Dùng MaPhuongApi)
                var khachHangIds = await _context.KhachHang
                    .Where(k => k.MaPhuongApi == model.MaPhuongApi)
                    .Select(k => k.Id)
                    .ToListAsync();

                if (!khachHangIds.Any())
                {
                    TempData["Error"] = "Phường này chưa có khách hàng nào.";
                    return RedirectToAction(nameof(Index));
                }

                // 2. Tìm tháng/năm mới nhất của nhóm khách hàng này
                var thangMoiNhat = await _context.ChiSoDien
                    .Where(x => khachHangIds.Contains(x.KhachHangId))
                    .OrderByDescending(x => x.Nam)
                    .ThenByDescending(x => x.Thang)
                    .Select(x => new { x.Thang, x.Nam })
                    .FirstOrDefaultAsync();

                if (thangMoiNhat == null)
                {
                    TempData["Error"] = "Chưa có chỉ số điện nào cho khách hàng thuộc phường này.";
                    return RedirectToAction(nameof(Index));
                }

                // 3. Lọc danh sách chỉ số điện chuẩn (Đã loại bỏ trùng lặp nếu có)
                var danhSachChiSoRaw = await _context.ChiSoDien
                    .Where(x =>
                        x.Thang == thangMoiNhat.Thang &&
                        x.Nam == thangMoiNhat.Nam &&
                        khachHangIds.Contains(x.KhachHangId)
                    )
                    .ToListAsync();

                var danhSachChiSo = danhSachChiSoRaw
                    .GroupBy(x => x.KhachHangId)
                    .Select(g => g.OrderByDescending(c => c.Id).First())
                    .ToList();

                if (!danhSachChiSo.Any())
                {
                    TempData["Error"] = "Không tìm thấy chỉ số điện tháng này.";
                    return RedirectToAction(nameof(Index));
                }

                // 4. Cập nhật bảng giá SQL
                int donGiaIdHopLe = await LuuBangGiaAsync(model.DanhSachGia, model.PhanTramVAT);

                // [ BƯỚC ĐỆM QUAN TRỌNG ]: Hút toàn bộ Mã Khách Hàng (MaKh) lên RAM...
                var dictKhachHang = await _context.KhachHang
                    .Where(k => khachHangIds.Contains(k.Id))
                    .ToDictionaryAsync(k => k.Id, k => k.MaKh);

                // =======================================================
                // [ CHỐT CHẶN MỚI LẮP ĐẶT ]: Tìm ra các khách hàng CHỈ CÓ ĐÚNG 1 LẦN GHI CHỈ SỐ
                // (Tức là mới chỉ có chỉ số đầu vào lúc lắp đồng hồ, chưa có tháng tiếp theo để đối chiếu tiêu thụ)
                var danhSachKhachMoiIds = await _context.ChiSoDien
                    .Where(x => khachHangIds.Contains(x.KhachHangId))
                    .GroupBy(x => x.KhachHangId)
                    .Where(g => g.Count() == 1) // Lọc ra ông nào mới có 1 record
                    .Select(g => g.Key)
                    .ToListAsync();
                // =======================================================

                // THÊM 2 BIẾN ĐẾM NÀY TRƯỚC VÒNG LẶP:
                int soKhachMoiBiBoQua = 0;
                int soHoaDonDaTonTai = 0;

                // 5. Xử lý logic tạo hóa đơn
                Console.WriteLine("[Debug 5] Bắt đầu tính tiền và tạo hóa đơn...");
                foreach (var chiSo in danhSachChiSo)
                {
                    var chiSoIdStr = chiSo.Id.ToString();
                    if (chiSo.KhachHangId == null) continue;

                    // === CHUYỂN PHẦN TẠO MÃ HÓA ĐƠN LÊN ĐÂY ===
                    string maKhachHang = dictKhachHang.ContainsKey(chiSo.KhachHangId) ? dictKhachHang[chiSo.KhachHangId] : "UNKNOWN";
                    string namNgan = (thangMoiNhat.Nam % 100).ToString("D2");
                    string kyHoaDon = $"{thangMoiNhat.Thang:D2}{namNgan}";
                    string maHdChinhThuc = $"HD-{maKhachHang}-{kyHoaDon}";

                    // 👉 [SỬA LẠI CHỖ NÀY]: Check tồn tại bằng cả ChiSoDienId HOẶC MaHd
                    var daTonTai = await _context.HoaDon.AnyAsync(x => x.ChiSoDienId == chiSoIdStr || x.MaHd == maHdChinhThuc);
                    if (daTonTai)
                    {
                        soHoaDonDaTonTai++; // Ghi nhận 1 ca đã có hóa đơn
                        continue;
                    }

                    if (danhSachKhachMoiIds.Contains(chiSo.KhachHangId))
                    {
                        soKhachMoiBiBoQua++; // Ghi nhận 1 ca mới lắp đồng hồ
                        continue;
                    }

                    int soDien = chiSo.ChiSoMoi - chiSo.ChiSoCu;

                    if (soDien <= 0)
                    {
                        soKhachMoiBiBoQua++; // Cũng tính là chưa phát sinh tiêu thụ
                        continue;
                    }
                    // =======================================================

                    // Vượt qua được trạm kiểm lâm thì mới tiến hành chém đẹp
                    decimal tienDien = TinhTienDienBacThang(soDien, model.DanhSachGia);
                    decimal thue = tienDien * (model.PhanTramVAT / 100m);

                    danhSachHoaDonMoi.Add(new HoaDon
                    {
                        Id = Guid.NewGuid().ToString(),
                        MaHd = maHdChinhThuc, // Dùng luôn cái biến vừa tạo ở trên
                        KhachHangId = chiSo.KhachHangId.ToString(),
                        ChiSoDienId = chiSoIdStr,
                        SoDienTieuThu = soDien,
                        TienDien = tienDien,
                        ThueVAT = thue,
                        TongThanhToan = tienDien + thue,
                        TrangThai = "ChuaThanhToan",
                        DonGiaId = donGiaIdHopLe,
                        NhanVienId = nhanVien.Id,
                        PhanTramVAT = model.PhanTramVAT,
                        NgayLap = now
                    });
                }

                Console.WriteLine($"[Debug 6] Số lượng hóa đơn mới chuẩn bị lưu: {danhSachHoaDonMoi.Count}");

                if (danhSachHoaDonMoi.Any())
                {
                    // === BƯỚC 1: LƯU SQL SERVER ===
                    _context.HoaDon.AddRange(danhSachHoaDonMoi);
                    await _context.SaveChangesAsync();

                    // === BƯỚC 2: LƯU MONGODB ===
                    try
                    {
                        if (_hoaDonCollection == null)
                            throw new Exception("Chưa khởi tạo kết nối MongoDB (_hoaDonCollection is null)");

                        var mongoData = danhSachHoaDonMoi.Select(hd => new HoaDon
                        {
                            Id = hd.Id,
                            MaHd = hd.MaHd, // MongoDB cũng sẽ nhận được mã chuẩn
                            KhachHangId = hd.KhachHangId,
                            ChiSoDienId = hd.ChiSoDienId,
                            SoDienTieuThu = hd.SoDienTieuThu,
                            TienDien = hd.TienDien,
                            ThueVAT = hd.ThueVAT,
                            TongThanhToan = hd.TongThanhToan,
                            TrangThai = hd.TrangThai,
                            NhanVienId = hd.NhanVienId,
                            PhanTramVAT = hd.PhanTramVAT,
                            NgayLap = now
                        }).ToList();

                        await _hoaDonCollection.InsertManyAsync(mongoData);
                        TempData["Success"] = $"Đã phát hành {danhSachHoaDonMoi.Count} hóa đơn thành công!";
                    }
                    catch (Exception exMongo)
                    {
                        Console.WriteLine("LỖI MONGO: " + exMongo.Message);
                        TempData["Success"] = $"Đã lưu {danhSachHoaDonMoi.Count} hóa đơn vào SQL. (MongoDB chưa đồng bộ)";
                    }
                }
                else
                {
                    if (soKhachMoiBiBoQua > 0 && soHoaDonDaTonTai == 0)
                    {
                        TempData["Error"] = $"Hệ thống đã bỏ qua {soKhachMoiBiBoQua} khách hàng do đây là tháng đầu lắp đồng hồ (Chưa phát sinh tiêu thụ). Không có hóa đơn nào được lập!";
                    }
                    else if (soHoaDonDaTonTai > 0 && soKhachMoiBiBoQua == 0)
                    {
                        TempData["Error"] = "Có khách hàng trong phường này đều đã được lập hóa đơn cho tháng này từ trước.";
                    }
                    else
                    {
                        TempData["Error"] = $"Đã bỏ qua: {soHoaDonDaTonTai} khách cũ (đã có HĐ) và {soKhachMoiBiBoQua} khách mới (chưa dùng điện). Không có hóa đơn mới nào!";
                    }
                }
            }
            catch (Exception ex)
            {
                string msgUi = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["Error"] = "Lỗi hệ thống: " + msgUi;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: HoaDon/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hoaDon = await _context.HoaDon.FindAsync(id);
            if (hoaDon == null)
            {
                return NotFound();
            }
            return View(hoaDon);
        }

        // POST: HoaDon/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,MaHd,KhachHangId,ChiSoDienId,Thang,Nam,TongTien,TrangThai,NgayThanhToan")] HoaDon hoaDon)
        {
            if (id != hoaDon.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(hoaDon);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HoaDonExists(hoaDon.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(hoaDon);
        }

        // GET: HoaDon/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 1. Lôi Hóa Đơn lên (Bỏ mẹ cái Include đi)
            var hoaDon = await _context.HoaDon.FirstOrDefaultAsync(m => m.Id == id);

            if (hoaDon == null)
            {
                return NotFound();
            }

            // 2. 💥 TÀ THUẬT MÓC TAY: Dùng KhachHangId đi tóm cổ thằng Khách
            var khach = await _context.KhachHang.FindAsync(hoaDon.KhachHangId);
            ViewBag.ChiSoMucTieu = await _context.ChiSoDien.FindAsync(hoaDon.ChiSoDienId);

            // 3. Nhét nó vào bao bố (ViewBag) để gửi sang View
            ViewBag.KhachMucTieu = khach;

            return View(hoaDon);
        }

        // POST: HoaDon/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id, string lyDoXoa = "")
        {
            var hoaDon = await _context.HoaDon.FindAsync(id);
            if (hoaDon == null) return NotFound();

            // 🛑 CHẶN ĐỨNG: Hóa đơn đã thu tiền thì cấm tuyệt đối việc xóa!
            // (Bảo vệ dữ liệu tài chính, muốn xóa thì phải làm nghiệp vụ Hoàn Tiền / Hủy thanh toán trước)
            if (hoaDon.TrangThai == "DaThanhToan")
            {
                TempData["Error"] = "TỪ CHỐI TIÊU HỦY: Hóa đơn này đã được khách hàng thanh toán. Lịch sử tài chính không cho phép xóa!";
                return RedirectToAction(nameof(Index));
            }

            // Chuẩn bị lý do để ghi vào sổ Nam Tào (MongoDB)
            string finalReason = string.IsNullOrWhiteSpace(lyDoXoa) ? "Không có lý do" : lyDoXoa;

            try
            {
                // 1. Đóng gói và bưng qua MongoDB (Archive)
                await _mongoService.ArchiveAsync(hoaDon, User.Identity.Name ?? "Hệ thống_Admin", finalReason);

                // 2. Trảm khỏi SQL Server
                _context.HoaDon.Remove(hoaDon);
                await _context.SaveChangesAsync();

                TempData["ThongBao"] = $"Đã tiêu hủy hóa đơn [{hoaDon.MaHd}] và đẩy vào phân vùng Archive thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống trong quá trình tiêu hủy hóa đơn: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: HoaDon/Restore
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // Tùy sếp phân quyền ai được phục hồi
        public async Task<IActionResult> Restore(string archiveId)
        {
            if (string.IsNullOrEmpty(archiveId))
            {
                TempData["Error"] = "LỖI: Thiếu mã định danh Archive ID!";
                return Redirect(Request.Headers["Referer"].ToString() ?? "/");
            }

            try
            {
                // 1. Lôi cái xác Hóa Đơn từ MongoDB lên
                var hoaDonPhucHoi = await _mongoService.GetArchivedDataAsync<HoaDon>(archiveId);

                if (hoaDonPhucHoi == null)
                {
                    TempData["Error"] = "THẤT BẠI: Không tìm thấy hóa đơn này trong Archive!";
                    return Redirect(Request.Headers["Referer"].ToString() ?? "/");
                }

                // ========================================================================
                // 🛡️ 4 LỚP GIÁP KIỂM TRA KHÓA NGOẠI (FOREIGN KEY CHECKS)
                // Nếu 1 trong 4 trụ cột này đã biến mất khỏi SQL, tuyệt đối không cho Restore!
                // ========================================================================
                bool khachTonTai = await _context.KhachHang.AnyAsync(k => k.Id == hoaDonPhucHoi.KhachHangId);
                bool chiSoTonTai = await _context.ChiSoDien.AnyAsync(c => c.Id == hoaDonPhucHoi.ChiSoDienId);
                bool nhanVienTonTai = await _context.NhanVien.AnyAsync(nv => nv.Id == hoaDonPhucHoi.NhanVienId);
                bool donGiaTonTai = await _context.DonGiaDien.AnyAsync(d => d.Id == hoaDonPhucHoi.DonGiaId);

                if (!khachTonTai || !chiSoTonTai || !nhanVienTonTai || !donGiaTonTai)
                {
                    TempData["Error"] = "Lỗi: Các dữ liệu gốc (Khách Hàng / Nhân Viên / Chỉ Số / Đơn Giá) liên kết với hóa đơn này đã bị mất khỏi hệ thống. Yêu cầu phục hồi dữ liệu gốc trước!";
                    return Redirect(Request.Headers["Referer"].ToString() ?? "/");
                }
                // ========================================================================

                // 2. An toàn rồi, bơm lại vào SQL
                _context.HoaDon.Add(hoaDonPhucHoi);
                await _context.SaveChangesAsync();

                // 3. Phục hồi xong thì xóa rác trong MongoDB
                await _mongoService.RemoveFromArchiveAsync<HoaDon>(archiveId);

                TempData["ThongBao"] = $"[ PHỤC HỒI THÀNH CÔNG ] Đã đưa hóa đơn [{hoaDonPhucHoi.MaHd}] quay lại hệ thống!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi phục hồi hóa đơn: " + ex.Message;
            }

            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }

        private bool HoaDonExists(string id)
        {
            return _context.HoaDon.Any(e => e.Id == id);
        }

        [HttpGet]
        public async Task<IActionResult> PrintInvoice(string maHd)
        {
            if (string.IsNullOrEmpty(maHd)) return BadRequest("Mã hóa đơn không hợp lệ.");

            // === BƯỚC 1: KÉO HÓA ĐƠN TỪ DB LÊN TRƯỚC ===
            var invoice = await (from hd in _context.HoaDon
                                 join kh in _context.KhachHang on hd.KhachHangId equals kh.Id into khGroup
                                 from kh in khGroup.DefaultIfEmpty()
                                 join csd in _context.ChiSoDien on hd.ChiSoDienId equals csd.Id into csdGroup
                                 from csd in csdGroup.DefaultIfEmpty()
                                 join nv in _context.NhanVien on hd.NhanVienId equals nv.Id into nvGroup
                                 from nv in nvGroup.DefaultIfEmpty()
                                 where hd.MaHd == maHd
                                 select new
                                 {
                                     HoaDon = hd,
                                     KhachHang = kh,
                                     ChiSoDien = csd,
                                     NhanVien = nv
                                 }).FirstOrDefaultAsync();

            if (invoice == null) return NotFound("Không tìm thấy hóa đơn này!");

            // === BƯỚC 2: TRUY VẤN 6 BẬC ĐƠN GIÁ (THEO CHIỀU DỌC) ===
            // 2.1. Tìm cái dòng Đơn Giá gốc mà Hóa Đơn đang neo vào
            var donGiaNeo = await _context.DonGiaDien.FirstOrDefaultAsync(d => d.Id == invoice.HoaDon.DonGiaId);

            if (donGiaNeo != null)
            {
                // 2.2. Lấy toàn bộ anh em của nó (Các bậc khác có cùng Ngày Tạo) và sắp xếp từ Bậc 1 -> Bậc 6
                var mang6Bac = await _context.DonGiaDien
                                             .Where(d => d.NgayTao == donGiaNeo.NgayTao)
                                             .OrderBy(d => d.Bac)
                                             .Select(d => d.Gia)
                                             .ToArrayAsync();


                // Quăng mảng số thực này sang cho HTML tự xào nấu
                ViewBag.GiaBacThang = mang6Bac;
            }

            // === BƯỚC 3: ĐÓNG GÓI DỮ LIỆU ĐẨY SANG GIAO DIỆN ===
            dynamic model = new System.Dynamic.ExpandoObject();
            model.HoaDon = invoice.HoaDon;
            model.KhachHang = invoice.KhachHang;
            model.ChiSoDien = invoice.ChiSoDien;
            model.NhanVien = invoice.NhanVien;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetAllTrangThai()
        {
            try
            {
                Console.WriteLine("\n[DEBUG] BẮT ĐẦU ĐẢO NGƯỢC TRẠNG THÁI TOÀN BỘ HÓA ĐƠN...");

                // 1. Kéo toàn bộ hóa đơn lên
                var danhSachHoaDon = await _context.HoaDon.ToListAsync();

                if (!danhSachHoaDon.Any())
                {
                    TempData["ThongBao"] = "Hệ thống chưa có hóa đơn nào để reset!";
                    return RedirectToAction(nameof(Index));
                }

                // 2. Vẩy bùa: Đưa tất cả về Chưa Thanh Toán và Xóa ngày thu
                foreach (var hd in danhSachHoaDon)
                {
                    hd.TrangThai = "ChuaThanhToan"; // Set lại đúng cái chữ gốc trong Model của sếp
                    hd.NgayThanhToan = null;        // Xóa ngày giờ đã nộp tiền
                }

                // 3. Lưu xuống SQL
                _context.HoaDon.UpdateRange(danhSachHoaDon);
                await _context.SaveChangesAsync();

                // 4. (Tùy chọn) Lưu xuống MongoDB nếu sếp đang sync 2 bên
                try
                {
                    if (_hoaDonCollection != null)
                    {
                        var filter = Builders<HoaDon>.Filter.Empty; // Chọc tất cả
                        var update = Builders<HoaDon>.Update
                            .Set(x => x.TrangThai, "ChuaThanhToan")
                            .Set(x => x.NgayThanhToan, null);

                        await _hoaDonCollection.UpdateManyAsync(filter, update);
                    }
                }
                catch (Exception exMongo)
                {
                    Console.WriteLine("[CẢNH BÁO MONGO] Lỗi khi reset mongo: " + exMongo.Message);
                }

                TempData["ThongBao"] = $"Đã reset thành công {danhSachHoaDon.Count} hóa đơn về trạng thái CHƯA THANH TOÁN!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống khi reset: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
