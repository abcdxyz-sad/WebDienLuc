using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
namespace WebSuDungDIen.Services // Sửa lại namespace cho đúng với project của sếp
{
    public class EmailSender : IEmailSender
    {
        // 💥 GÀI ĐẠN: Sếp nhét Email và Mật khẩu ứng dụng (16 ký tự của Google) vào đây
        private readonly string _emailNguon = "kle45356@gmail.com";
        private readonly string _matKhauUngDung = "wmqvqopxrwityeci";

        // Đã khoét thêm nòng: maKhach, soDienThoai, dienTieuThu
        public async Task GuiBienLaiThanhToanAsync(string emailDich, string tenKhach, string maKhach, string soDienThoai, string maHoaDon, int dienTieuThu, decimal soTien)
        {
            // Lọc bọn nhập linh tinh (không có @) cho đỡ rác server
            if (string.IsNullOrWhiteSpace(emailDich) || !emailDich.Contains("@"))
            {
                Console.WriteLine("[BÁO ĐỘNG] LỖI! Email rỗng hoặc sai định dạng!");
                return;
            }

            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(_emailNguon, _matKhauUngDung),
                    EnableSsl = true,
                };

                // Thiết kế cái biên lai đỏ máu, bọc thép bằng HTML
                var noiDungHtml = $@"
                <div style='background-color: #f4f6f9; padding: 30px 15px; margin: 0; font-family: ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; border: 1px solid #eaedf1; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05);'>

                        <div style='background-color: #f8f9fa; padding: 25px 30px; border-bottom: 2px solid #eaedf1; text-align: center;'>
                            <h2 style='margin: 0; color: #0d6efd; font-size: 24px; text-transform: uppercase; letter-spacing: 1px;'>Biên Lai Thanh Toán</h2>
                            <p style='margin: 5px 0 0 0; color: #6c757d; font-size: 14px; font-weight: bold;'>HỆ THỐNG ĐIỆN LỰC CYBER</p>
                        </div>

                        <div style='padding: 30px;'>
                            <p style='font-size: 16px; margin-top: 0;'>Kính chào <b>{tenKhach}</b>,</p>
                            <p style='font-size: 16px; color: #495057; line-height: 1.6;'>Hệ thống đã ghi nhận khoản thanh toán thành công. Cảm ơn quý khách đã tin tưởng và sử dụng dịch vụ của chúng tôi!</p>
    
                            <div style='background-color: #ffffff; border: 1px solid #eaedf1; border-radius: 8px; padding: 20px; margin: 25px 0;'>
                                <h4 style='margin: 0 0 15px 0; color: #212529; font-size: 14px; text-transform: uppercase; letter-spacing: 0.5px;'><span style='color: #0d6efd;'>■</span> Định danh khách hàng</h4>
                                <table style='width: 100%; border-collapse: collapse;'>
                                    <tr>
                                        <td style='padding: 6px 0; color: #6c757d; font-size: 15px;'>Mã khách hàng:</td>
                                        <td style='padding: 6px 0; text-align: right; font-weight: bold; color: #212529; font-size: 15px;'>{maKhach}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 6px 0; color: #6c757d; font-size: 15px;'>Số điện thoại:</td>
                                        <td style='padding: 6px 0; text-align: right; font-weight: bold; color: #212529; font-size: 15px;'>{(string.IsNullOrEmpty(soDienThoai) ? "Chưa cập nhật" : soDienThoai)}</td>
                                    </tr>
                                </table>
                            </div>

                            <div style='background-color: #f8f9fa; border: 1px solid #eaedf1; border-radius: 8px; padding: 20px; margin: 25px 0;'>
                                <h4 style='margin: 0 0 15px 0; color: #212529; font-size: 14px; text-transform: uppercase; letter-spacing: 0.5px;'><span style='color: #dc3545;'>■</span> Chi tiết giao dịch</h4>
        
                                <table style='width: 100%; border-collapse: collapse;'>
                                    <tr>
                                        <td style='padding: 8px 0; color: #6c757d; font-size: 15px;'>Mã hóa đơn:</td>
                                        <td style='padding: 8px 0; text-align: right; font-weight: bold; color: #0d6efd; font-size: 15px;'>{maHoaDon}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; color: #6c757d; font-size: 15px;'>Điện tiêu thụ:</td>
                                        <td style='padding: 8px 0; text-align: right; font-weight: bold; color: #198754; font-size: 15px;'>{dienTieuThu} kWh</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; color: #6c757d; font-size: 15px;'>Thời gian ghi nhận:</td>
                                        <td style='padding: 8px 0; text-align: right; font-weight: bold; color: #212529; font-size: 15px;'>{DateTime.Now:dd/MM/yyyy HH:mm}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 15px 0 0 0; color: #212529; font-size: 15px; border-top: 1px dashed #ced4da; font-weight: bold;'>Tổng tiền thanh toán:</td>
                                        <td style='padding: 15px 0 0 0; text-align: right; font-weight: bold; color: #dc3545; font-size: 20px; border-top: 1px dashed #ced4da;'>{soTien:N0} VNĐ</td>
                                    </tr>
                                </table>
                            </div>

                            <p style='font-size: 15px; color: #495057; line-height: 1.6; margin-bottom: 0;'>Nếu có bất kỳ thắc mắc nào, vui lòng truy cập trung tâm hỗ trợ trên website của chúng tôi.</p>
                        </div>

                        <div style='background-color: #f8f9fa; padding: 20px 30px; text-align: center; border-top: 1px solid #eaedf1;'>
                            <p style='margin: 0; color: #6c757d; font-size: 13px;'>Email này được gửi tự động từ hệ thống lõi.</p>
                            <p style='margin: 4px 0 0 0; color: #6c757d; font-size: 13px; font-weight: bold;'>Xin vui lòng không trả lời email này!</p>
                            <p style='margin: 15px 0 0 0; color: #adb5bd; font-size: 12px;'>© {DateTime.Now.Year} Cyber Power. All rights reserved.</p>
                        </div>

                    </div>
                </div>";

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailNguon, "ĐIỆN LỰC CYBER"),
                    Subject = $"[BIÊN LAI] Thanh toán thành công HĐ {maHoaDon}",
                    Body = noiDungHtml,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(emailDich);

                // BÓP CÒ!
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("========================================");
                Console.WriteLine("LỖI BẮN EMAIL: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("CHI TIẾT: " + ex.InnerException.Message);
                }
                Console.WriteLine("========================================");
            }
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@")) return;

            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(_emailNguon, _matKhauUngDung),
                    EnableSsl = true,
                };

                // 💥 CHỮA LỖI Ở ĐÂY: Khởi tạo truyền thống, trói cứng From và To!
                var mailMessage = new MailMessage(
                    from: new MailAddress(_emailNguon, "HỆ THỐNG ĐIỆN LỰC CYBER"), // Phải gán cứng bằng MailAddress
                    to: new MailAddress(email)
                )
                {
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                // Bóp cò xả đạn!
                await smtpClient.SendMailAsync(mailMessage);
                Console.WriteLine($"[ SUCCESS ] - Đã nã đạn Email Identity thành công tới: {email}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("========================================");
                Console.WriteLine("💥 LỖI BẮN EMAIL IDENTITY: " + ex.Message);
                Console.WriteLine("========================================");
                throw; // Ném lỗi ra để trang web báo đỏ!
            }
        }
    }
}