using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebSuDungDIen.Models;

namespace WebSuDungDIen.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<NhanVien> NhanVien { get; set; } = null!;
        public DbSet<KhachHang> KhachHang { get; set; } = null!;
        public DbSet<ChiSoDien> ChiSoDien { get; set; } = null!;
        public DbSet<HoaDon> HoaDon { get; set; } = null!;
        public DbSet<DonGiaDien> DonGiaDien { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<KhachHang>()
               .Property(k => k.IdentityUserId)
               .IsRequired(false);
            // 🔗 NhanVien ↔ ApplicationUser (1–1)
            modelBuilder.Entity<NhanVien>()
                .HasOne(nv => nv.User)
                .WithOne()
                .HasForeignKey<NhanVien>(nv => nv.IdentityUserId)
                .IsRequired();

            // 🔗 KhachHang ↔ ApplicationUser (1–1)
            modelBuilder.Entity<KhachHang>()
                .HasOne(kh => kh.User)
                .WithOne()
                .HasForeignKey<KhachHang>(kh => kh.IdentityUserId)
                .IsRequired();

            // ===============================
            // 🔗 KhachHang (1) ↔ ChiSoDien (N)
            // ===============================
            modelBuilder.Entity<ChiSoDien>()
                .HasOne<KhachHang>()
                .WithMany(kh => kh.ChiSoDien)
                .HasForeignKey(cs => cs.KhachHangId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===============================
            // 🔗 NhanVien (1) ↔ ChiSoDien (N)
            // ===============================
            modelBuilder.Entity<ChiSoDien>()
                .HasOne<NhanVien>()
                .WithMany()
                .HasForeignKey(cs => cs.NhanVienId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===============================
            // 🔗 ChiSoDien (1) ↔ HoaDon (1)
            // ===============================
            modelBuilder.Entity<HoaDon>()
                .HasOne<ChiSoDien>()
                .WithOne()
                .HasForeignKey<HoaDon>(hd => hd.ChiSoDienId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===============================
            // 🔗 KhachHang (1) ↔ HoaDon (N)
            // ===============================
            modelBuilder.Entity<HoaDon>()
                .HasOne<KhachHang>()
                .WithMany()
                .HasForeignKey(hd => hd.KhachHangId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===============================
            // 🔗 NhanVien (1) ↔ HoaDon (N)
            // ===============================
            modelBuilder.Entity<HoaDon>()
                .HasOne<NhanVien>()
                .WithMany()
                .HasForeignKey(hd => hd.NhanVienId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===============================
            // 🔗 DonGiaDien (1) ↔ HoaDon (N)
            // ===============================
            modelBuilder.Entity<HoaDon>()
                .HasOne<DonGiaDien>()
                .WithMany()
                .HasForeignKey(hd => hd.DonGiaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
