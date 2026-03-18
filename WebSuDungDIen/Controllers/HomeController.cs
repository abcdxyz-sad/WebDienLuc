using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebSuDungDien.Services;
using WebSuDungDIen.Models;

namespace WebSuDungDIen.Controllers
{
    public class HomeController : Controller
    {
        private readonly MongoService _mongo;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, MongoService mongo)
        {
            _logger = logger;
            _mongo = mongo;
        }

        public IActionResult Index()
        {
            var userName = User.Identity.Name;
            string role = "";

            if (User.IsInRole("Admin"))
            {
                role = "Admin";
            }
            else if (User.IsInRole("NhanVien"))
            {
                role = "NhanVien";
            }
            else if (User.IsInRole("KhachHang"))
            {
                role = "KhachHang";
            }

            if (User.Identity.IsAuthenticated)
            {
                _mongo.Logs.InsertOne(new SystemLog
                {
                    Action = "Truy cập trang chủ",
                    User = User.Identity.Name,
                    Role = role,
                    CreatedAt = DateTime.Now
                });
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
