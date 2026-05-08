using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using WebSuDungDien.Services;
using WebSuDungDIen.Data;
using WebSuDungDIen.Hubs;
using WebSuDungDIen.Models;
using WebSuDungDIen.Services;
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
builder.Services.AddTransient<EmailSender>();
builder.Services.AddTransient<IEmailSender, EmailSender>();
// 1. Đăng ký MongoClient bằng cách đọc từ Render Environment
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    // Render: MongoDB__ConnectionString => C#: Configuration["MongoDB:ConnectionString"]
    var connectionString = builder.Configuration["MongoDB:ConnectionString"];

    if (string.IsNullOrEmpty(connectionString))
    {
        // Dự phòng nếu sếp quên set hoặc muốn chạy ở Local mà chưa có biến môi trường
        throw new Exception("LỖI: Chưa tìm thấy chuỗi kết nối MongoDB trên hệ thống!");
    }

    return new MongoClient(connectionString);
});

// 2. Đăng ký Collection bằng cách đọc Database Name từ Render
builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();

    // Render: MongoDB__DatabaseName => C#: Configuration["MongoDB:DatabaseName"]
    var dbName = builder.Configuration["MongoDB:DatabaseName"] ?? "WebSuDungDienLogs";

    var database = client.GetDatabase(dbName);
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
// Đăng ký HttpContextAccessor để lấy IP và User-Agent
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IMongoArchiveService, MongoArchiveService>();
// Đăng ký Service bắt anomaly
builder.Services.AddSingleton<AnomalyLoginService>();
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
