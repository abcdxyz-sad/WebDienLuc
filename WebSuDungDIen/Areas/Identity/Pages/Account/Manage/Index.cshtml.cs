// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using WebSuDungDIen.Data;
using WebSuDungDIen.Models;
using Microsoft.EntityFrameworkCore;
namespace WebSuDungDIen.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

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
        /// 
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Phone]
            [Display(Name = "Phone number")]
            [Required(ErrorMessage = "[ LỖI ] - Vui lòng nhập vào số điện thoại!")]
            public string PhoneNumber { get; set; }
            [Required(ErrorMessage = "[ LỖI ] - Vui lòng nhập tên bạn!")]
            public string FullName { get; set; }
            public string UserCode { get; set; }
            [Required(ErrorMessage = "[ LỖI ] - Vui lòng nhập địa chỉ của bạn!")]
            public string Address { get; set; }
            public string AccountType { get; set; }
            [Required(ErrorMessage = "[ LỖI ] - Vui lòng nhập địa chỉ email!")]
            public string Email { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            Input = new InputModel();

            // 1. MÓC DATA NHÂN VIÊN TỪ SQL SERVER
            if (roles.Contains("Admin") || roles.Contains("NhanVien") || roles.Contains("Employee"))
            {
                // Dùng _context.TênBảng thay vì collection của Mongo
                var nhanVien = await _context.NhanVien
                    .FirstOrDefaultAsync(nv => nv.IdentityUserId == user.Id);

                if (nhanVien != null)
                {
                    Input.FullName = nhanVien.TenNV;
                    Input.UserCode = nhanVien.MaNV;
                    Input.PhoneNumber = nhanVien.DienThoai;
                    Input.Address = nhanVien.DiaChi;
                    Input.AccountType = $"Nhân viên nội bộ - {nhanVien.ChucVu}";
                    Input.Email = await _userManager.GetEmailAsync(user);
                }
                else
                {
                    Input.FullName = "LỖI: CHƯA CÓ DATA NHÂN VIÊN TRONG DB";
                    Input.AccountType = $"Identity ID: {user.Id}";
                    Input.UserCode = "Null";
                }
            }
            // 2. MÓC DATA KHÁCH HÀNG TỪ SQL SERVER
            else
            {
                // Dùng _context.TênBảng
                var khachHang = await _context.KhachHang
                    .FirstOrDefaultAsync(kh => kh.IdentityUserId == user.Id);

                if (khachHang != null)
                {
                    Input.FullName = khachHang.TenKh;
                    Input.UserCode = khachHang.MaKh;
                    Input.PhoneNumber = khachHang.DienThoai;
                    Input.Address = !string.IsNullOrEmpty(khachHang.DiaChiDayDu) ? khachHang.DiaChiDayDu : khachHang.DiaChi;
                    Input.AccountType = "Khách hàng sử dụng điện";
                    Input.Email = await _userManager.GetEmailAsync(user);
                }
                else
                {
                    Input.FullName = "LỖI: CHƯA CÓ DATA KHÁCH HÀNG TRONG DB";
                    Input.UserCode = "Null";
                    Input.AccountType = $"Identity ID: {user.Id}";
                }
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user); // Gọi hàm mình vừa viết ở trên
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // Cắm 2 cái cờ để theo dõi mọi nhất cử nhất động
            bool isIdentityChanged = false;
            bool isNhanVienChanged = false;

            // ==========================================
            // 1. CẬP NHẬT PHẦN IDENTITY (SĐT & EMAIL)
            // ==========================================

            // Xử lý Số điện thoại
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    TempData["Error"] = "[ LỖI ] - Không thể ghi đè Số điện thoại!";
                    return RedirectToPage();
                }
                isIdentityChanged = true; // Phất cờ có thay đổi Identity
            }

            // Xử lý Email (Tà thuật đổi Email trực tiếp không cần gửi mail xác nhận)
            var email = await _userManager.GetEmailAsync(user);
            if (Input.Email != email)
            {
                // Ghi đè Email và Username (Thường Username = Email)
                await _userManager.SetEmailAsync(user, Input.Email);
                await _userManager.SetUserNameAsync(user, Input.Email);

                // Vì sửa Email là sửa định danh, nên ép hệ thống xác nhận luôn để khỏi lằng nhằng
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);

                isIdentityChanged = true; // Phất cờ có thay đổi Identity
            }

            // ==========================================
            // 2. CẬP NHẬT BẢNG NHÂN VIÊN TRONG SQL SERVER (TÊN & ĐỊA CHỈ)
            // ==========================================

            // Soi xem nó có quyền Nhân Viên/Admin không
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin") || roles.Contains("NhanVien") || roles.Contains("Employee"))
            {
                // Móc hồ sơ dưới CSDL lên
                var nhanVien = await _context.NhanVien.FirstOrDefaultAsync(nv => nv.IdentityUserId == user.Id);
                if (nhanVien != null)
                {
                    // Kiểm tra xem có đổi Tên không
                    if (nhanVien.TenNV != Input.FullName)
                    {
                        nhanVien.TenNV = Input.FullName;
                        isNhanVienChanged = true;
                    }

                    // 💥 TÀ THUẬT VÁ LỖ HỔNG: Ép bảng NhanVien cũng phải nhận số điện thoại mới!
                    if (nhanVien.DienThoai != Input.PhoneNumber)
                    {
                        nhanVien.DienThoai = Input.PhoneNumber;
                        isNhanVienChanged = true;
                    }

                    // Kiểm tra xem có đổi Địa chỉ không
                    if (nhanVien.DiaChi != Input.Address)
                    {
                        nhanVien.DiaChi = Input.Address;
                        isNhanVienChanged = true;
                    }

                    // Có thay đổi thì bóp cò lưu xuống CSDL
                    if (isNhanVienChanged)
                    {
                        _context.NhanVien.Update(nhanVien);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            // ==========================================
            // 3. CHỐT CHẶN CUỐI CÙNG: CÓ ĐỔI GÌ KHÔNG?
            // ==========================================

            // Nếu cả 2 cờ đều nằm im (không thay đổi Identity, cũng không đổi SQL)
            if (!isIdentityChanged && !isNhanVienChanged)
            {
                // Báo lỗi bằng StatusMessage thay vì TempData vì Razor Pages Identity xài cái này
                TempData["Error"] = "Không có thông tin nào được thay đổi!";
                return RedirectToPage();
            }

            // Nếu Identity có thay đổi thì F5 lại phiên đăng nhập để thông tin mới ăn vào hệ thống ngay lập tức
            if (isIdentityChanged)
            {
                await _signInManager.RefreshSignInAsync(user);
            }

            TempData["ThongBao"] = "[ THÀNH CÔNG ] - Thay đổi đã được thực thi!";
            return RedirectToPage();
        }
    }
}
