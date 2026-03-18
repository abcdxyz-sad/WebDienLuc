using ClosedXML.Excel;
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

        public HoaDonController(ApplicationDbContext context, IMongoClient mongoClient, TaoMaService taoMaService, UserManager<ApplicationUser> userManager)
        {
            var database = mongoClient.GetDatabase("Cluster0");
            _chiSoDienCollection = database.GetCollection<ChiSoDien>("ChiSoDien");
            _hoaDonCollection = database.GetCollection<HoaDon>("HoaDon");
            _context = context;
            _taoMaService = taoMaService;
            _userManager = userManager;
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

        public async Task<IActionResult> Index(string maPhuongApi, int? thang, int? nam)
        {
            // 1. Chuẩn bị dữ liệu cho Dropdown (ViewBag)
            // ĐÃ XÓA TẠO SELECTLIST PHƯỜNG Ở ĐÂY VÌ ĐÃ GIAO CHO JAVASCRIPT XỬ LÝ

            // Tạo danh sách Tháng (1 -> 12)
            var listThang = Enumerable.Range(1, 12).Select(x => new { Value = x, Text = "Tháng " + x }).ToList();
            ViewBag.ThangList = new SelectList(listThang, "Value", "Text", thang);

            // Tạo danh sách Năm (5 năm gần nhất)
            int currentYear = DateTime.Now.Year;
            var listNam = Enumerable.Range(currentYear - 4, 5).OrderByDescending(x => x).Select(x => new { Value = x, Text = "Năm " + x }).ToList();
            ViewBag.NamList = new SelectList(listNam, "Value", "Text", nam);

            // Lưu lại giá trị đang chọn để hiển thị lại trên View
            ViewBag.CurrentPhuong = maPhuongApi;
            ViewBag.CurrentThang = thang;
            ViewBag.CurrentNam = nam;

            // 2. Truy vấn dữ liệu (LINQ) - ĐÃ XÓA BẢNG PHƯỜNG
            var query = from hd in _context.HoaDon
                        join cs in _context.ChiSoDien on hd.ChiSoDienId equals cs.Id.ToString()
                        join kh in _context.KhachHang on hd.KhachHangId equals kh.Id.ToString()
                        select new
                        {
                            hd,
                            cs,
                            kh
                        };

            // 3. Áp dụng bộ lọc (Filter)
            if (!string.IsNullOrEmpty(maPhuongApi))
            {
                query = query.Where(x => x.kh.MaPhuongApi == maPhuongApi);
            }

            if (thang.HasValue)
            {
                query = query.Where(x => x.cs.Thang == thang.Value);
            }

            if (nam.HasValue)
            {
                query = query.Where(x => x.cs.Nam == nam.Value);
            }

            // 4. Sắp xếp và Select ra ViewModel
            var result = await query
                .OrderByDescending(x => x.cs.Nam)
                .ThenByDescending(x => x.cs.Thang)
                .ThenByDescending(x => x.hd.MaHd)
                .Select(x => new HoaDonIndexVM
                {
                    Id = x.hd.Id,
                    MaHd = x.hd.MaHd,
                    TenKhachHang = x.kh.TenKh,
                    // Thay vì TenPhuong (bảng cũ), ta dùng luôn địa chỉ đầy đủ để hiển thị
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

        [HttpGet]
        public async Task<IActionResult> DuyetThanhToan(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // 1. Tìm hóa đơn trong Database
            var hoaDon = await _context.HoaDon.FindAsync(id);
            if (hoaDon == null)
            {
                TempData["Error"] = "Không tìm thấy hóa đơn này!";
                return RedirectToAction(nameof(Index));
            }

            bool khachDaTraTien = false; // Thay bằng: hoaDon.KhachDaChuyenKhoan == true; hoặc tương tự

            // 3. Xử lý kết quả duyệt
            if (khachDaTraTien)
            {
                // Nếu khách đã trả: Cập nhật hóa đơn thành ĐÃ THU
                hoaDon.TrangThai = "DaThanhToan";
                hoaDon.NgayThanhToan = DateTime.Now;

                _context.Update(hoaDon);
                await _context.SaveChangesAsync();

                TempData["ThongBao"] = $"Tuyệt vời! Đã duyệt thanh toán thành công cho hóa đơn #{hoaDon.MaHd}.";
            }
            else
            {
                // Nếu khách chưa trả: Bật cảnh báo (Cái này sẽ ăn vào TempData["Error"] màu đỏ của bạn ở View)
                TempData["Error"] = $"Khách hàng chưa thanh toán cho hóa đơn #{hoaDon.MaHd}. Không thể duyệt!";
            }

            // Quay lại trang danh sách hóa đơn
            return RedirectToAction(nameof(Index));
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

            // 2. Lấy Bảng giá điện hiện hành
            var bangGia = await _context.DonGiaDien.OrderBy(g => g.Bac).ToListAsync();

            ViewBag.ThongTinKhach = kh != null ? $"{kh.TenKh} - {kh.MaKh}" : "Không tìm thấy thông tin";
            ViewBag.ThongTinNhanVien = nv != null ? nv.TenNV : "Không tìm thấy nhân viên";

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
            var ds = await _context.KhachHang
                .Where(k => k.MaPhuongApi == maPhuongApi)
                .Select(k => new
                {
                    k.Id,
                    Ten = k.MaKh + " - " + k.TenKh,
                    // Lấy tháng mới nhất luôn!
                    ChiSo = _context.ChiSoDien
                        .Where(c => c.KhachHangId == k.Id)
                        .OrderByDescending(c => c.Nam)     // Năm mới nhất
                        .ThenByDescending(c => c.Thang)    // Tháng mới nhất
                        .Select(c => (int?)(c.ChiSoMoi - c.ChiSoCu))
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Json(ds);
        }

        // GET: HoaDon/Create
        public async Task<IActionResult> Create()
        {
            // Bước 1: Lấy dữ liệu thô từ Database về bộ nhớ (chỉ lấy đúng 2 cột cần dùng cho nhẹ)
            var khachHangTho = await _context.KhachHang
                .Where(k => !string.IsNullOrEmpty(k.MaPhuongApi) && !string.IsNullOrEmpty(k.DiaChiDayDu))
                .Select(k => new { k.MaPhuongApi, k.DiaChiDayDu })
                .ToListAsync(); // <--- CHỐT CHẶN Ở ĐÂY: Ép chạy câu lệnh SQL và lưu vào RAM

            // Bước 2: Xử lý chuỗi và GroupBy bằng C# trên RAM (Không lo lỗi Expression Tree nữa)
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

            // Load bảng giá như cũ
            var bangGia = await _context.DonGiaDien
                .OrderBy(x => x.Bac)
                .ToListAsync();

            model.DanhSachGia = bangGia.Select(x => new DonGiaBacVM
            {
                Bac = x.Bac,
                Gia = x.Gia
            }).ToList();

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
        private async Task<int> LuuBangGiaAsync(List<DonGiaBacVM> dsGia)
        {
            var now = DateTime.Now;

            // Lấy danh sách bảng giá hiện tại trong Database
            var allGia = await _context.DonGiaDien.ToListAsync();

            if (allGia.Any())
            {
                // === CÁCH 1: NẾU ĐÃ CÓ BẢNG GIÁ -> CHỈ CẬP NHẬT TIỀN (KHÔNG XÓA) ===
                foreach (var bac in dsGia)
                {
                    var giaDb = allGia.FirstOrDefault(g => g.Bac == bac.Bac);
                    if (giaDb != null)
                    {
                        giaDb.Gia = bac.Gia; // Cập nhật lại giá tiền mới
                    }
                    else
                    {
                        // Phòng hờ trường hợp bạn thêm bậc 7, bậc 8...
                        _context.DonGiaDien.Add(new DonGiaDien
                        {
                            Bac = bac.Bac,
                            Gia = bac.Gia,
                            NgayTao = now
                        });
                    }
                }
            }
            else
            {
                // === CÁCH 2: NẾU DB TRỐNG TRƠN -> THÊM MỚI HOÀN TOÀN ===
                var listGiaMoi = new List<DonGiaDien>();
                foreach (var bac in dsGia)
                {
                    listGiaMoi.Add(new DonGiaDien
                    {
                        Bac = bac.Bac,
                        Gia = bac.Gia,
                        NgayTao = now
                    });
                }
                _context.DonGiaDien.AddRange(listGiaMoi);
            }

            // Lưu mọi thay đổi (Update hoặc Insert) xuống Database
            await _context.SaveChangesAsync();

            // Lấy ID của Bậc 1 trả về để gán cho Hóa Đơn (tránh lỗi khóa ngoại)
            var bac1 = await _context.DonGiaDien.FirstOrDefaultAsync(g => g.Bac == 1);
            return bac1?.Id ?? 1;
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
                int donGiaIdHopLe = await LuuBangGiaAsync(model.DanhSachGia);

                // [ BƯỚC ĐỆM QUAN TRỌNG ]: Hút toàn bộ Mã Khách Hàng (MaKh) lên RAM để dùng cho việc tạo Mã Hóa Đơn
                var dictKhachHang = await _context.KhachHang
                    .Where(k => khachHangIds.Contains(k.Id))
                    .ToDictionaryAsync(k => k.Id, k => k.MaKh);

                // 5. Xử lý logic tạo hóa đơn
                Console.WriteLine("[Debug 5] Bắt đầu tính tiền và tạo hóa đơn...");
                foreach (var chiSo in danhSachChiSo)
                {
                    var chiSoIdStr = chiSo.Id.ToString();

                    // Kiểm tra tồn tại trong SQL
                    var daTonTai = await _context.HoaDon.AnyAsync(x => x.ChiSoDienId == chiSoIdStr);
                    if (daTonTai)
                    {
                        continue;
                    }

                    if (chiSo.KhachHangId == null) continue;

                    int soDien = chiSo.ChiSoMoi - chiSo.ChiSoCu;
                    decimal tienDien = TinhTienDienBacThang(soDien, model.DanhSachGia);
                    decimal thue = tienDien * (model.PhanTramVAT / 100m);

                    // =======================================================
                    // [ UPDATE LOGIC TẠO MÃ HÓA ĐƠN CHUẨN ZZZ CỦA BẠN ]
                    // 1. Tìm MaKh từ Dictionary (Tốc độ ánh sáng, không gọi DB)
                    string maKhachHang = dictKhachHang.ContainsKey(chiSo.KhachHangId) ? dictKhachHang[chiSo.KhachHangId] : "UNKNOWN";

                    // 2. Format năm (Chia lấy dư cho 100 sẽ an toàn hơn Substring. VD: 2026 % 100 = 26)
                    string namNgan = (thangMoiNhat.Nam % 100).ToString("D2");

                    // 3. Ghép chuỗi chuẩn định dạng
                    string kyHoaDon = $"{thangMoiNhat.Thang:D2}{namNgan}";
                    string maHd = $"HD-{maKhachHang}-{kyHoaDon}";
                    // =======================================================

                    danhSachHoaDonMoi.Add(new HoaDon
                    {
                        Id = Guid.NewGuid().ToString(),
                        MaHd = maHd, // Nhét cái mã mới xịn xò vào đây
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
                    TempData["Error"] = "Tất cả hóa đơn đã tồn tại.";
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

            var hoaDon = await _context.HoaDon
                .FirstOrDefaultAsync(m => m.Id == id);
            if (hoaDon == null)
            {
                return NotFound();
            }

            return View(hoaDon);
        }

        // POST: HoaDon/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var hoaDon = await _context.HoaDon.FindAsync(id);
            if (hoaDon != null)
            {
                _context.HoaDon.Remove(hoaDon);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
    }
}
