document.addEventListener("DOMContentLoaded", function () {
    setTimeout(function () {
        var tb = document.getElementById("thongBao");
        if (tb) {
            // Thêm transition cho cả opacity và transform
            tb.style.transition = "opacity 0.5s ease, transform 0.5s ease";

            // Biến mất và trượt sang phải
            tb.style.opacity = "0";
            tb.style.transform = "translateX(50px) skewX(-10deg)"; // Giữ độ nghiêng nhưng đẩy sang phải

            setTimeout(function () {
                tb.remove();
            }, 500);
        }
    }, 3000); // 3 giây
});