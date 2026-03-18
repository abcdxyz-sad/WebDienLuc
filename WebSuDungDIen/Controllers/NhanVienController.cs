using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebSuDungDIen.Data;
using WebSuDungDIen.Models;

namespace WebSuDungDIen.Controllers
{
    public class NhanVienController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TaoMaService _taoMaService;

        public NhanVienController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, TaoMaService taoMaService)
        {
            _context = context;
            _userManager = userManager;
            _taoMaService = taoMaService;
        }

        // GET: NhanVien
        public async Task<IActionResult> Index()
        {
            var nhanViens = await _context.NhanVien.ToListAsync();

            var userIds = nhanViens
                .Select(nv => nv.IdentityUserId)
                .ToList();

            var users = _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToList();

            foreach (var nv in nhanViens)
            {
                nv.User = users
                    .FirstOrDefault(u => u.Id == nv.IdentityUserId);
            }

            return View(nhanViens);
        }

        // GET: NhanVien/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nhanVien = await _context.NhanVien
                .Include(n => n.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (nhanVien == null)
            {
                return NotFound();
            }

            return View(nhanVien);
        }

        public async Task<JsonResult> GetHoTen(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return Json("");

            return Json(user.HoTen);
        }

        // GET: NhanVien/Create
        public async Task<IActionResult> Create(int? id)
        {
            var users = await _userManager.GetUsersInRoleAsync("NhanVien");
            var nhanVien = await _context.NhanVien.FindAsync(id);
            var daCoHoSo = _context.NhanVien
                                .Select(n => n.IdentityUserId)
                                .ToList();

            var usersChuaCoHoSo = users
                .Where(u => !daCoHoSo.Contains(u.Id))
                .ToList();

            ViewBag.IdentityUserId =
                new SelectList(usersChuaCoHoSo, "Id", "UserName");

            ViewBag.ChucVuList = new SelectList(new List<object>
            {
                new { Value = "NhanVien", Text = "Nhân viên" },
                new { Value = "Admin", Text = "Admin" }
            }, "Value", "Text", nhanVien?.ChucVu);


            return View();
        }

        // POST: NhanVien/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdentityUserId,DiaChi,DienThoai,ChucVu,TrangThai")] NhanVien nhanVien)
        {
            // 🔎 Tìm user trước
            var user = await _userManager.FindByIdAsync(nhanVien.IdentityUserId);

            if (user == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản.");
                await LoadNhanVienDropdown(null);
                return View(nhanVien);
            }

            // 🔥 Sinh mã tự động
            var maNV = _taoMaService.GenerateUniqueCode(
                "NV",
                code => _context.NhanVien.Any(x => x.MaNV == code)
            );

            nhanVien.MaNV = maNV;

            // 🔥 Lấy tên từ ApplicationUser
            nhanVien.TenNV = user.HoTen;

            // ❗ Sau khi đã set đầy đủ mới check ModelState
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                await LoadNhanVienDropdown(nhanVien.IdentityUserId);
                return View(nhanVien);
            }

            _context.NhanVien.Add(nhanVien);
            await _context.SaveChangesAsync();
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Xoá role cũ (nếu muốn chỉ giữ 1 role)
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            // Thêm role mới theo ChucVu
            await _userManager.AddToRoleAsync(user, nhanVien.ChucVu);

            return RedirectToAction(nameof(Index));
        }


        private async Task LoadNhanVienDropdown(string? selectedId)
        {
            var users = await _userManager.GetUsersInRoleAsync("NhanVien");

            var daCoHoSo = _context.NhanVien
                                .Select(n => n.IdentityUserId)
                                .ToList();

            var usersChuaCoHoSo = users
                .Where(u => !daCoHoSo.Contains(u.Id))
                .ToList();

            ViewData["IdentityUserId"] =
                new SelectList(usersChuaCoHoSo, "Id", "UserName", selectedId);
        }

        // GET: NhanVien/Edit/5
        public async Task<IActionResult> Edit(string id) // BƯỚC SỬA QUAN TRỌNG NHẤT: Đổi int? thành string
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var nhanVien = await _context.NhanVien.FindAsync(id);
            if (nhanVien == null)
                return NotFound();

            // Dropdown chức vụ
            ViewBag.ChucVuList = new SelectList(new List<object>
                {
                    new { Value = "NhanVien", Text = "Nhân viên" },
                    new { Value = "Admin", Text = "Admin" }
                }, "Value", "Text", nhanVien?.ChucVu);

            return View(nhanVien);
        }

        // POST: NhanVien/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id,[Bind("Id,TenNV,DiaChi,DienThoai,ChucVu,TrangThai")] NhanVien model)
        {
            if (id != model.Id)
                return NotFound();

            var nhanVien = await _context.NhanVien.FindAsync(id);
            if (nhanVien == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                // Chỉ update field cho phép
                nhanVien.TenNV = model.TenNV;
                nhanVien.DiaChi = model.DiaChi;
                nhanVien.DienThoai = model.DienThoai;
                nhanVien.ChucVu = model.ChucVu;
                nhanVien.TrangThai = model.TrangThai;

                await _context.SaveChangesAsync();

                // 🔥 Update role Identity theo ChucVu
                var user = await _userManager.FindByIdAsync(nhanVien.IdentityUserId);

                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, roles);
                    await _userManager.AddToRoleAsync(user, nhanVien.ChucVu);
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.ChucVuList = new SelectList(new List<string>
            {
                "NhanVien",
                "Admin"
            }, model.ChucVu);

            return View(model);
        }


        // GET: NhanVien/Delete/5
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nhanVien = await _context.NhanVien
                .Include(n => n.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (nhanVien == null)
            {
                return NotFound();
            }

            return View(nhanVien);
        }

        // POST: NhanVien/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nhanVien = await _context.NhanVien.FindAsync(id);
            if (nhanVien != null)
            {
                _context.NhanVien.Remove(nhanVien);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NhanVienExists(string id)
        {
            return _context.NhanVien.Any(e => e.Id == id);
        }
    }
}
