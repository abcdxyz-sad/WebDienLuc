// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using WebSuDungDIen.Models;

namespace WebSuDungDIen.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);

                // 💥 GỠ BỎ TÀNG HÌNH: Báo lỗi thẳng mặt nếu nhập sai Email để dễ test!
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "[ LỖI ] - Email này đéo tồn tại trong hệ thống! Nhập lại đi sếp!");
                    return Page(); // Trả lại trang cũ kèm chữ đỏ, đéo thèm giả vờ Redirect nữa!
                }

                // 1. Tạo mã Token reset pass bí mật
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                // 2. Nhào nặn ra cái đường Link dẫn tới trang Đặt lại mật khẩu
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code, email = Input.Email },
                    protocol: Request.Scheme);

                // 💥 HACK TRỰC TIẾP: In luôn cái link ra màn hình Debug Console của Visual Studio
                Console.WriteLine("\n=======================================================");
                Console.WriteLine($"[ BẠO LỰC ] LINK ĐỔI PASS CỦA MÀY ĐÂY: {callbackUrl}");
                Console.WriteLine("=======================================================\n");

                // 3. Chuẩn bị nội dung Email
                string emailSubject = "[ SYS.WARNING ] - YÊU CẦU ĐẶT LẠI MẬT KHẨU";
                // 💥 ĐÃ THÁO CÙM CHO ĐƯỜNG LINK: Bỏ HtmlEncoder.Default.Encode đi!
                string emailHtmlBody = $@"
                    <div style='background-color: #f4f6f9; padding: 40px 10px; font-family: Arial, Helvetica, sans-serif;'>
                        <table width='100%' max-width='600' cellpadding='0' cellspacing='0' align='center' style='max-width: 600px; background-color: #ffffff; border: 1px solid #dee2e6; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.05); border-collapse: collapse;'>
        
                            <tr>
                                <td style='background-color: #f8f9fa; padding: 25px 30px; border-bottom: 2px solid #e9ecef; text-align: center;'>
                                    <h2 style='margin: 0; color: #0d6efd; font-size: 22px; text-transform: uppercase; letter-spacing: 0.5px;'>
                                        YÊU CẦU ĐẶT LẠI MẬT KHẨU
                                    </h2>
                                    <p style='margin: 5px 0 0 0; color: #6c757d; font-size: 14px; font-weight: bold;'>
                                        HỆ THỐNG QUẢN LÝ ĐIỆN LỰC
                                    </p>
                                </td>
                            </tr>
        
                            <tr>
                                <td style='padding: 30px;'>
                
                                    <p style='margin: 0 0 20px 0; font-size: 16px; color: #333; line-height: 1.5;'>
                                        Kính chào Quý khách,
                                    </p>
                                    <p style='margin: 0 0 25px 0; font-size: 16px; color: #333; line-height: 1.5;'>
                                        Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản liên kết với địa chỉ email: <strong>{Input.Email}</strong>. Vui lòng kiểm tra thông tin chi tiết dưới đây:
                                    </p>

                                    <div style='background-color: #ffffff; border: 1px solid #e9ecef; border-radius: 6px; padding: 20px; margin-bottom: 25px;'>
                                        <table width='100%' cellpadding='0' cellspacing='0' style='border-collapse: collapse;'>
                                            <tr>
                                                <td width='40%' style='padding: 8px 0; color: #6c757d; font-size: 14px; border-bottom: 1px solid #f8f9fa;'>Loại yêu cầu:</td>
                                                <td width='60%' style='padding: 8px 0; color: #212529; font-size: 14px; font-weight: bold; border-bottom: 1px solid #f8f9fa;'>Khôi phục quyền truy cập</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 8px 0; color: #6c757d; font-size: 14px;'>Thời gian ghi nhận:</td>
                                                <td style='padding: 8px 0; color: #212529; font-size: 14px; font-weight: bold;'>{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}</td>
                                            </tr>
                                        </table>
                                    </div>

                                    <div style='text-align: center; margin-bottom: 25px;'>
                                        <a href='{callbackUrl}' style='background-color: #0d6efd; color: #ffffff; padding: 12px 25px; text-decoration: none; font-weight: bold; font-size: 15px; border-radius: 4px; display: inline-block;'>
                                            ĐẶT LẠI MẬT KHẨU
                                        </a>
                                    </div>
                                </td>
                            </tr>
        
                            <tr>
                                <td style='padding: 20px 30px; background-color: #f8f9fa; border-top: 1px solid #e9ecef; text-align: center;'>
                                    <p style='margin: 0 0 5px 0; font-size: 13px; color: #6c757d; font-weight: bold;'>
                                        CẢNH BÁO BẢO MẬT
                                    </p>
                                    <p style='margin: 0; font-size: 13px; color: #6c757d; line-height: 1.5;'>
                                        Nếu Quý khách không yêu cầu đặt lại mật khẩu, vui lòng <strong>bỏ qua email này</strong>. Mật khẩu của Quý khách vẫn được bảo mật và an toàn.
                                    </p>
                                    <p style='margin: 15px 0 0 0; font-size: 12px; color: #adb5bd;'>
                                        © {DateTime.Now.Year} Hệ Thống Quản Lý Điện Lực. All rights reserved.
                                    </p>
                                </td>
                            </tr>

                        </table>
                    </div>";

                // 4. BẮT LỖI GỬI MAIL (Đề phòng cục IEmailSender bị nổ ngầm)
                try
                {
                    await _emailSender.SendEmailAsync(Input.Email, emailSubject, emailHtmlBody);

                    // Chỉ khi nào mail bay đi thành công mới được sang trang Báo Cáo
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }
                catch (Exception ex)
                {
                    // 💥 NẾU XỊT: Móc ruột gan cái lỗi SMTP ra phơi lên màn hình!
                    ModelState.AddModelError(string.Empty, $"[ HỆ THỐNG BÁO LỖI ]: Cấu hình EmailSender của sếp bị nổ rồi! Chi tiết: {ex.Message}");
                    return Page(); // Trả về trang hiện tại để hiển thị cục lỗi màu đỏ!
                }
            }

            return Page();
        }
    }
}
