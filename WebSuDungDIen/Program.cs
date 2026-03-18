using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using WebSuDungDien.Services;
using WebSuDungDIen.Data;
using WebSuDungDIen.Hubs;
using WebSuDungDIen.Models;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    // TẮT RÀNG BUỘC PASSWORD
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 1;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

builder.Services.AddScoped<TaoMaService>();

builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient("mongodb+srv://kira:kira@cluster0.9j2bsjo.mongodb.net/?appName=Cluster0"));

builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var database = client.GetDatabase("WebSuDungDienLogs");
    return database.GetCollection<SystemLog>("SystemLogs");
});

builder.Services.AddControllersWithViews(options =>
{
    // Tạo một chính sách: Bắt buộc tất cả người dùng phải xác thực (đăng nhập)
    var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

    // Áp dụng chính sách này làm màng lọc cho TOÀN BỘ hệ thống
    options.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddSingleton<MongoService>();

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var users = userManager.Users.ToList();

    foreach (var user in users)
    {
        await userManager.UpdateSecurityStampAsync(user);
    }
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = { "Admin", "NhanVien", "KhachHang" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    await DbInitializer.SeedAdminAsync(scope.ServiceProvider);
}
app.MapHub<PaymentHub>("/paymentHub");


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
