using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using WebSuDungDIen.Data;
using WebSuDungDIen.Hubs;
using WebSuDungDIen.Models;
[AllowAnonymous]
public class ThanhToanController : Controller
{
    private readonly IMongoCollection<KhachHang> _khachHangCollection;
    private readonly IMongoCollection<HoaDon> _hoaDonCollection;
    private readonly IHubContext<PaymentHub> _hubContext;
    private readonly ApplicationDbContext _context;

    // ĐÃ SỬA LỖI: Bỏ IMongoCollection<HoaDon> ra khỏi tham số
    public ThanhToanController(IMongoClient mongoClient, IHubContext<PaymentHub> hubContext, ApplicationDbContext context)
    {
        // LƯU Ý: Nhớ đổi "TenDatabaseCuaBan" thành tên DB thật của bạn nhé!
        var db = mongoClient.GetDatabase("TenDatabaseCuaBan");

        // Tự lấy Collection ra từ db, không bắt hệ thống DI tự tiêm vào nữa!
        _khachHangCollection = db.GetCollection<KhachHang>("KhachHang");
        _hoaDonCollection = db.GetCollection<HoaDon>("HoaDon");
        _context = context;
        _hubContext = hubContext;
    }

    // 2. API Xử lý thanh toán khi bấm nút trên điện thoại
    [HttpPost]
    [IgnoreAntiforgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> XacNhanThanhToan(string id)
    {
        // 1. Tìm hóa đơn trong SQL Server bằng _context
        // (Nếu Id trong C# của bạn là kiểu Guid thì dùng Guid.Parse(id), nếu là string thì để nguyên id)
        var hoaDon = await _context.HoaDon.FindAsync(id);

        // Nếu dòng FindAsync ở trên báo lỗi gạch đỏ do sai kiểu dữ liệu, 
        // bạn comment nó lại và xài dòng dưới này nhé:
        // var hoaDon = await _context.HoaDon.FirstOrDefaultAsync(h => h.Id.ToString() == id);

        if (hoaDon != null)
        {
            // 2. Cập nhật trạng thái
            hoaDon.TrangThai = "DaThanhToan";
            hoaDon.NgayThanhToan = DateTime.Now;

            // 3. Lưu vào Database
            _context.HoaDon.Update(hoaDon);
            await _context.SaveChangesAsync();

            // 4. Bắn pháo hoa báo hiệu (SignalR)
            var khach = await _context.KhachHang.FindAsync(hoaDon.KhachHangId);

            // Nếu tìm thấy khách thì lấy MaKh, không thì để chữ "Khách Ẩn Danh" cho chắc cốp
            string maKhach = khach != null ? khach.MaKh : "Khách Ẩn Danh";

            // 4. Bắn pháo hoa (Gửi Mã Khách và Mã Hóa Đơn đi)
            await _hubContext.Clients.All.SendAsync("ReceivePaymentSuccess", maKhach, hoaDon.MaHd);
            return Json(new { success = true, message = "Thanh toán thành công!" });
        }

        return Json(new { success = false, message = "Lỗi: Không tìm thấy hóa đơn trong Database!" });
    }

    [HttpGet]
    public IActionResult Mobile(string id, string maHd, string maKh, int thang, int nam, int soDien, decimal tongthanhtoan)
    {
        ViewBag.HoaDonId = id;
        ViewBag.MaHd = maHd;
        ViewBag.MaKh = maKh;
        ViewBag.Thang = thang;
        ViewBag.Nam = nam;
        ViewBag.SoDien = soDien;
        ViewBag.TongThanhToan = tongthanhtoan;
        return View();
    }
}