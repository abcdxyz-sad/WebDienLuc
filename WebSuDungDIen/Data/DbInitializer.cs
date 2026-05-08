using Microsoft.AspNetCore.Identity;
using WebSuDungDIen.Data;
using WebSuDungDIen.Models;

public static class DbInitializer
{
    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        const string adminUserName = "admin";
        const string adminEmail = "admin@admin.com";
        const string adminPassword = "123";

        // ✅ 1. ĐẢM BẢO CÁC ROLE CƠ BẢN TỒN TẠI (Gộp chung vào đây cho sạch Program.cs)
        string[] roles = { "Admin", "NhanVien", "KhachHang" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // ✅ 2. LOGIC BỌC THÉP: Tìm xem CÓ AI đang làm Admin chưa? (Không tìm theo UserName nữa)
        var danhSachAdmin = await userManager.GetUsersInRoleAsync("Admin");

        // ✅ 3. NẾU CHƯA CÓ AI LÀM ADMIN -> MỚI TIẾN HÀNH TẠO MỚI
        if (danhSachAdmin.Count == 0)
        {
            var user = new ApplicationUser
            {
                UserName = adminUserName,
                Email = adminEmail,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, adminPassword);

            if (!result.Succeeded)
            {
                throw new Exception("Không tạo được admin: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // Gán quyền Admin
            await userManager.AddToRoleAsync(user, "Admin");

            // Tạo luôn hồ sơ Nhân viên cho ông Admin này
            var nhanVien = new NhanVien
            {
                IdentityUserId = user.Id,
                MaNV = "NVADMIN",
                TenNV = "Quản trị hệ thống",
                ChucVu = "Admin",
                TrangThai = true
            };

            context.NhanVien.Add(nhanVien);
            await context.SaveChangesAsync();

            Console.WriteLine("\n[HỆ THỐNG] Đã khởi tạo tài khoản Admin mặc định thành công!");
        }
        else
        {
            // Đã có Admin rồi (dù đổi tên/email) thì hệ thống sẽ im lặng đi tiếp, không đẻ thêm ma!
            Console.WriteLine("\n[HỆ THỐNG] Đã có Quản trị viên điều hành. Bỏ qua khởi tạo.");
        }
    }
}