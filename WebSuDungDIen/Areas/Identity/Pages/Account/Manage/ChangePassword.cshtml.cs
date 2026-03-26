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

            // 💥 TÀ THUẬT: Thêm dấu ? để biến nó thành Nullable, vô hiệu hóa Required ngầm định của C#
            [DataType(DataType.Password)]
            public string? OldPassword { get; set; }

            [StringLength(100, ErrorMessage = "Mật khẩu mới phải từ {2} đến {1} ký tự.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string? NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không trùng khớp!")]
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

            // 💥 XÓA ÁN TỬ: Lờ đi lỗi Password nếu khách không thèm nhập
            if (string.IsNullOrEmpty(Input.NewPassword))
            {
                ModelState.Remove("Input.OldPassword");
                ModelState.Remove("Input.NewPassword");
                ModelState.Remove("Input.ConfirmPassword");
            }

            // Nếu vẫn còn lỗi (ví dụ bỏ trống Email), thì phải nạp lại Data rồi mới văng lỗi!
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

            // 2. LƯU PASS (Chỉ khi nào ngứa tay gõ Pass mới)
            if (!string.IsNullOrEmpty(Input.NewPassword))
            {
                if (string.IsNullOrEmpty(Input.OldPassword))
                {
                    ModelState.AddModelError("Input.OldPassword", "Vui lòng nhập Mật khẩu hiện tại!");
                    await LoadAsync(user); // Lỗi cũng phải nạp lại data!
                    return Page();
                }

                var result = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                    await LoadAsync(user);
                    return Page();
                }
                isModified = true;
            }

            if (isModified)
            {
                await _userManager.UpdateAsync(user);
                await _signInManager.RefreshSignInAsync(user);
                StatusMessage = "Cập nhật thông tin thành công!";
            }

            return RedirectToPage();
        }
    }
}