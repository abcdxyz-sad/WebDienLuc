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
            public string PhoneNumber { get; set; }
            public string FullName { get; set; }
            public string UserCode { get; set; }
            public string Address { get; set; }
            public string AccountType { get; set; }
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
                }
                else
                {
                    Input.FullName = "LỖI: CHƯA CÓ DATA NHÂN VIÊN TRONG DB";
                    Input.UserCode = "Null";
                    Input.AccountType = $"Identity ID: {user.Id}";
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

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
