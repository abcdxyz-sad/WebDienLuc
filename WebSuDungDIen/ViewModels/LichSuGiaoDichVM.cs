namespace WebSuDungDIen.ViewModels
{
    public class LichSuGiaoDichVM
    {
        public string MaGiaoDich { get; set; } // Mã GD của VNPay/Momo hoặc hệ thống
        public DateTime NgayGiaoDich { get; set; }

        // 💥 THỂ LOẠI: Thanh toán hóa đơn, Hoàn tiền, Đóng phạt...
        public string TheLoaiGiaoDich { get; set; }
        public string ThangNamHoaDon { get; set; } // Ghi chú thanh toán cho tháng nào

        // 💥 PHƯƠNG THỨC: VNPay, Momo, Tiền mặt, Chuyển khoản ngân hàng...
        public string PhuongThuc { get; set; }

        public decimal SoTien { get; set; }
        public bool TrangThaiThanhCong { get; set; } // True = Thành công, False = Thất bại/Hủy
    }
}
