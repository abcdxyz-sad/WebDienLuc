using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WebSuDungDIen.Models;

namespace WebSuDungDIen.Areas.Identity.Pages.Account.Manage
{
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<ChangePasswordModel> _logger;

        public ChangePasswordModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<ChangePasswordModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Email không được để trống.")]
            [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
            public string Email { get; set; }

            [DataType(DataType.Password)]
            public string? OldPassword { get; set; }

            [DataType(DataType.Password)]
            public string? NewPassword { get; set; }

            [DataType(DataType.Password)]
            public string? ConfirmPassword { get; set; }
        }

        // 💥 HÀM CỨU MẠNG: Tải lại data để không bị trắng bóc màn hình
        private async Task LoadAsync(ApplicationUser user)
        {
            Input = new InputModel
            {
                Username = user.UserName,
                Email = user.Email
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound($"Không tìm thấy user.");

            await LoadAsync(user); // Nạp đạn cho giao diện
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // 💥 MÁY QUÉT Ý ĐỊNH: Đã gõ 1 chữ vào 1 trong 3 ô thì xác định là đang muốn đổi Pass
            bool isChangingPassword = !string.IsNullOrEmpty(Input.OldPassword) ||
                                      !string.IsNullOrEmpty(Input.NewPassword) ||
                                      !string.IsNullOrEmpty(Input.ConfirmPassword);

            if (!isChangingPassword)
            {
                ModelState.Remove("Input.OldPassword");
                ModelState.Remove("Input.NewPassword");
                ModelState.Remove("Input.ConfirmPassword");
            }
            else
            {
                // 💥 BẪY 1: Nhập thiếu ô
                if (string.IsNullOrEmpty(Input.OldPassword) ||
                    string.IsNullOrEmpty(Input.NewPassword) ||
                    string.IsNullOrEmpty(Input.ConfirmPassword))
                {
                    if (string.IsNullOrEmpty(Input.OldPassword)) ModelState.AddModelError("Input.OldPassword", "Vui lòng nhập mật khẩu cũ.");
                    if (string.IsNullOrEmpty(Input.NewPassword)) ModelState.AddModelError("Input.NewPassword", "Vui lòng tạo mật khẩu mới.");
                    if (string.IsNullOrEmpty(Input.ConfirmPassword)) ModelState.AddModelError("Input.ConfirmPassword", "Vui lòng xác nhận mật khẩu mới.");

                    // 👉 Bắn Notification:
                    TempData["Error"] = "Vui lòng nhập đầy đủ cả 3 trường mật khẩu!";
                }
                // 💥 BẪY 2: Nhập đủ nhưng Mới và Xác nhận đéo giống nhau
                else if (Input.NewPassword != Input.ConfirmPassword)
                {
                    ModelState.AddModelError("Input.ConfirmPassword", "Mật khẩu xác nhận không khớp với mật khẩu mới!");

                    // 👉 Bắn Notification:
                    TempData["Error"] = "Mật khẩu xác nhận không khớp!";
                }
            }

            // Nếu dính bất kỳ bẫy nào ở trên (hoặc lỗi Email/Username) -> Văng lỗi ra màn hình ngay lập tức!
            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            bool isModified = false;

            // 1. LƯU USERNAME & EMAIL
            if (Input.Username != user.UserName)
            {
                await _userManager.SetUserNameAsync(user, Input.Username);
                isModified = true;
            }
            if (Input.Email != user.Email)
            {
                await _userManager.SetEmailAsync(user, Input.Email);
                user.EmailConfirmed = true;
                isModified = true;
            }

            // 2. LƯU PASS (Chạy tới đây là 3 ô đã được điền đầy đủ và khớp nhau)
            if (isChangingPassword)
            {
                var result = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);

                // 💥 BẪY 3: Gõ sai mật khẩu cũ (Server trả về lỗi)
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);

                    // 👉 Bắn Notification:
                    TempData["Error"] = "Đổi mật khẩu thất bại. Vui lòng kiểm tra lại mật khẩu cũ!";

                    await LoadAsync(user);
                    return Page();
                }
                isModified = true;
            }

            // 3. CHỐT HẠ THÔNG BÁO CUỐI CÙNG
            if (isModified)
            {
                await _userManager.UpdateAsync(user);
                await _signInManager.RefreshSignInAsync(user);
                TempData["ThongBao"] = "Cập nhật thông tin thành công!";
            }
            else
            {
                // Chửi vào mặt những kẻ bấm lưu dạo
                TempData["Error"] = "Không có thông tin nào được thay đổi!";
            }

            return RedirectToPage();
        }
    }
}