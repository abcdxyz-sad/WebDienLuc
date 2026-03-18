// File: wwwroot/js/qrPayment.js

// 1. HÀM MỞ MODAL QR (ĐÃ BỌC THÉP CHỐNG LỖI TÀNG HÌNH)
function openQRPayment(id, maHd, maKh, trangThai, thang, nam, soDien, tongthanhtoan) {

    // Xử lý kỳ chốt nếu bị thiếu
    if (thang == 0 || nam == 0) {
        let parts = maHd.split('-');
        if (parts.length > 1) {
            let duoi = parts[parts.length - 1];
            if (duoi.length === 4 && !isNaN(duoi)) {
                thang = duoi.substring(0, 2);
                nam = "20" + duoi.substring(2, 4);
            }
        }
    }

    // Chặn thanh toán rồi
    if (trangThai === "DaThanhToan" || trangThai === "Đã Thanh Toán") {
        alert("Khoan đã! Hóa đơn này đã được thanh toán rồi. Không cần quét mã nữa!");
        return;
    }

    // BẬT CÙNG LÚC CẢ BẢNG QR LẪN MÀNG ĐEN CHỐNG OAN HỒN
    let modal = document.getElementById('qrModal');
    let overlay = document.getElementById('qrOverlay');

    if (modal) modal.style.display = 'block';
    if (overlay) {
        overlay.style.display = 'block';
        overlay.style.backdropFilter = 'blur(3px)'; // Bật mờ
    }
    document.body.style.overflow = 'hidden'; // Khóa cuộn màn hình

    // Format tiền tệ kiểu VNĐ cho nó sang chảnh (VD: 500.000)
    let formattedMoney = new Intl.NumberFormat('vi-VN').format(tongthanhtoan) + " đ";

    // Gắn thông tin phụ + Số tiền to chà bá
    document.getElementById('qr-invoice-id').innerHTML = `
        <span class="text-muted fs-6">Mã HĐ: ${maHd}</span> <br/> 
        <span class="text-muted fs-6">Kỳ: ${thang}/${nam}</span> <br/> 
        <span class="text-muted fs-6">Tiêu thụ: ${soDien} kWh</span> <br/>
        <div class="mt-2 text-danger" style="font-size: 2rem;">${formattedMoney}</div>
    `;

    // TẠO LINK THANH TOÁN (ĐÃ BỌC THÉP CHỐNG LỖI KÝ TỰ BẰNG encodeURIComponent)
    let currentDomain = window.location.origin;
    let paymentUrl = `${currentDomain}/ThanhToan/Mobile?id=${encodeURIComponent(id)}&maHd=${encodeURIComponent(maHd)}&maKh=${encodeURIComponent(maKh)}&thang=${encodeURIComponent(thang)}&nam=${encodeURIComponent(nam)}&soDien=${encodeURIComponent(soDien)}&tongthanhtoan=${encodeURIComponent(tongthanhtoan)}`;

    console.log("👉 Đang tạo mã QR cho Link:", paymentUrl);

    // ==========================================
    // CHIÊU THỨC BẤT TỬ ĐỂ VẼ QR (CHỐNG TÀNG HÌNH)
    // ==========================================
    let qrContainer = document.getElementById("qrcode");

    // Nếu hệ thống chưa thuê thợ vẽ QR (Lần đầu tiên bấm)
    if (!window.qrCodeWorker) {
        qrContainer.innerHTML = ""; // Quét dọn 1 lần duy nhất
        window.qrCodeWorker = new QRCode(qrContainer, {
            text: paymentUrl,
            width: 250,
            height: 250,
            colorDark: "#111111",
            colorLight: "#ffffff",
            correctLevel: QRCode.CorrectLevel.L
        });
    } else {
        // Nếu thợ vẽ đã ở sẵn đó (Bấm lần 2, 3...), chỉ cần xóa nét vẽ cũ và vẽ lại
        window.qrCodeWorker.clear();
        window.qrCodeWorker.makeCode(paymentUrl);
    }
}


// 2. HÀM ĐÓNG MODAL (DỌN SẠCH BÁCH MÀNG ĐEN VÀ MỞ KHÓA CUỘN CHUỘT)
function closeQRModal() {
    console.log("=== ĐÓNG CỔNG QR VÀ DỌN DẸP OAN HỒN ===");
    try {
        let modal = document.getElementById('qrModal');
        if (modal) modal.style.display = 'none';

        let overlay = document.getElementById('qrOverlay');
        if (overlay) {
            overlay.style.backdropFilter = 'none'; // Phá phép sương mù
            overlay.style.display = 'none';
        }

        document.body.style.overflow = 'auto'; // Cho phép khách lăn chuột lại
    } catch (error) {
        console.error("Lỗi khi đóng Modal: ", error);
        document.getElementById('qrOverlay').setAttribute("style", "display: none !important;");
        document.body.style.overflow = 'auto';
    }
}


// 3. KẾT NỐI SIGNALR ĐỂ CHỜ BÁO TING TING
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/paymentHub")
    .build();

// Hứng 2 biến từ C# gửi qua: maKh và maHd
connection.on("ReceivePaymentSuccess", function (maKh, maHd) {
    // Đóng khung QR lại bằng hàm xịn bên trên
    closeQRModal();

    // Bắn thông báo siêu xịn xò
    alert(`🎉 TING TING! Khách hàng [${maKh}] vừa thanh toán thành công hóa đơn [${maHd}]!`);

    // Tải lại trang để cập nhật huy hiệu "ĐÃ THANH TOÁN"
    window.location.reload();
});

connection.start().catch(function (err) {
    console.error("Lỗi SignalR: ", err.toString());
});