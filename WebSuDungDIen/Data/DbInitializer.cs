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

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        if (!await roleManager.RoleExistsAsync("KhachHang"))
        {
            await roleManager.CreateAsync(new IdentityRole("KhachHang"));
        }

        var user = await userManager.FindByNameAsync(adminUserName);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = adminUserName,
                Email = adminEmail,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, adminPassword);

            if (!result.Succeeded)
            {
                throw new Exception("Không tạo được admin: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        // ✅ 3. GÁN ROLE ADMIN
        if (!await userManager.IsInRoleAsync(user, "Admin"))
        {
            await userManager.AddToRoleAsync(user, "Admin");
        }

        // ✅ 4. Tạo bảng Nhân viên nếu chưa có
        var nhanVien = context.NhanVien
            .FirstOrDefault(x => x.IdentityUserId == user.Id);

        if (nhanVien == null)
        {
            nhanVien = new NhanVien
            {
                IdentityUserId = user.Id,
                MaNV = "NVADMIN",
                TenNV = "Quản trị hệ thống",
                ChucVu = "Admin",
                TrangThai = true
            };

            context.NhanVien.Add(nhanVien);
            await context.SaveChangesAsync();
        }
    }
}
