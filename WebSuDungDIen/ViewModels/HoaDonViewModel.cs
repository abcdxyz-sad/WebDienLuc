namespace WebSuDungDIen.Models
{
    public class HoaDonViewModel
    {
        public int ChiSoCu { get; set; }
        public int ChiSoMoi { get; set; }

        public decimal Gia1 { get; set; }
        public decimal Gia2 { get; set; }
        public decimal Gia3 { get; set; }
        public decimal Gia4 { get; set; }
        public decimal Gia5 { get; set; }
        public decimal Gia6 { get; set; }

        public decimal PhanTramVAT { get; set; } // nhập 8 hoặc 10
    }

}
