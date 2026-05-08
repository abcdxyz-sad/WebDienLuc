namespace WebSuDungDIen.ViewModels
{
    public class LichSuDonGiaVM
    {
        public int Thang { get; set; }
        public int Nam { get; set; }
        public DateTime NgayNhapLanDau { get; set; } // Ngày cắm cờ lần đầu tiên trong tháng

        // Trải phẳng 6 bậc ra để hiển thị trên 1 hàng ngang
        public decimal Gia1 { get; set; }
        public decimal Gia2 { get; set; }
        public decimal Gia3 { get; set; }
        public decimal Gia4 { get; set; }
        public decimal Gia5 { get; set; }
        public decimal Gia6 { get; set; }
    }
}
