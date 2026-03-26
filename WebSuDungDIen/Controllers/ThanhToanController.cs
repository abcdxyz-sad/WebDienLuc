using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using WebSuDungDIen.Data;
using WebSuDungDIen.Hubs;
using WebSuDungDIen.Models;
using WebSuDungDIen.Services;
[AllowAnonymous]
public class ThanhToanController : Controller
{
    private readonly IMongoCollection<KhachHang> _khachHangCollection;
    private readonly IMongoCollection<HoaDon> _hoaDonCollection;
    private readonly IHubContext<PaymentHub> _hubContext;
    private readonly ApplicationDbContext _context;
    private readonly EmailSender _emailSender;

    // ĐÃ SỬA LỖI: Bỏ IMongoCollection<HoaDon> ra khỏi tham số
    public ThanhToanController(IMongoClient mongoClient, IHubContext<PaymentHub> hubContext, ApplicationDbContext context, EmailSender emailSender)
    {
        // LƯU Ý: Nhớ đổi "TenDatabaseCuaBan" thành tên DB thật của bạn nhé!
        var db = mongoClient.GetDatabase("TenDatabaseCuaBan");

        // Tự lấy Collection ra từ db, không bắt hệ thống DI tự tiêm vào nữa!
        _khachHangCollection = db.GetCollection<KhachHang>("KhachHang");
        _hoaDonCollection = db.GetCollection<HoaDon>("HoaDon");
        _context = context;
        _hubContext = hubContext;
        _emailSender = emailSender;
    }

    // 2. API Xử lý thanh toán khi bấm nút trên điện thoại
    [HttpPost]
    [IgnoreAntiforgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> XacNhanThanhToan(string id)
    {
        // 1. Tìm hóa đơn trong SQL Server bằng _context
        var hoaDon = await _context.HoaDon.FindAsync(id);

        if (hoaDon != null)
        {
            // 2. Cập nhật trạng thái, lụm tiền
            hoaDon.TrangThai = "DaThanhToan";
            hoaDon.NgayThanhToan = DateTime.Now;

            // 3. Lưu vào Database
            _context.HoaDon.Update(hoaDon);
            await _context.SaveChangesAsync();

            // 4. Móc thông tin thằng khách ra (Đằng nào cũng phải móc để lấy Email)
            var khach = await _context.KhachHang.FindAsync(hoaDon.KhachHangId);

            // Lấy thông tin phòng hờ nó bị null
            string maKhach = khach != null ? khach.MaKh : "Khách Ẩn Danh";
            string emailKhach = "";
            string tenKhach = khach != null ? khach.TenKh : "Khách Hàng Cyber";

            if (khach != null && !string.IsNullOrEmpty(khach.IdentityUserId))
            {
                // Tà thuật dùng _context mò thẳng sang bảng Users của Identity
                var taiKhoanIdentity = await _context.Users.FindAsync(khach.IdentityUserId);

                if (taiKhoanIdentity != null)
                {
                    emailKhach = taiKhoanIdentity.Email; // Đã bắt được con tin!
                }
            }

            // 5. Bắn pháo hoa báo hiệu (SignalR) lên màn hình
            await _hubContext.Clients.All.SendAsync("ReceivePaymentSuccess", maKhach, hoaDon.MaHd);

            // 6. 💥 RÚT SÚNG VÀ NÃ ĐẠN EMAIL!
            // Lưu ý: Tôi đang giả định bảng HoaDon của sếp có cột "TongTien" (Tổng tiền).
            // Nếu sếp đặt tên khác (ví dụ ThanhTien, SoTien) thì tự sửa lại chữ TongTien cho đúng nhé!
            decimal soTienDaThu = hoaDon.TongThanhToan;

            // Bóp cò! Lỗi hay không lỗi thì cái Try-Catch bên trong hàm này nó cũng nuốt hết!
            // Móc thông tin số điện thoại của khách ra
            string sdtKhach = khach != null ? khach.DienThoai : "";

            // Móc chỉ số điện từ Hóa Đơn ra (giả sử cột tên là DienTieuThu)
            int dienDaDung = hoaDon.SoDienTieuThu;

            // 💥 Kéo nòng súng, nhét đủ 7 viên đạn vào!
            await _emailSender.GuiBienLaiThanhToanAsync(
                emailKhach,      // 1. Email
                tenKhach,        // 2. Tên khách
                maKhach,         // 3. Mã khách
                sdtKhach,        // 4. Số điện thoại
                hoaDon.MaHd,     // 5. Mã Hóa đơn
                dienDaDung,      // 6. Điện tiêu thụ
                soTienDaThu      // 7. Tổng tiền
            );

            // 7. Chốt hạ, báo cáo hoàn tất
            return Json(new { success = true, message = "Thanh toán thành công! Pháo hoa đã nổ, Email đã bắn!" });
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