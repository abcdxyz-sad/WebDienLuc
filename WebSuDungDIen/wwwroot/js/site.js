document.addEventListener('show.bs.dropdown', function (event) {
    // Bắt lấy cái nút vừa được bấm
    var button = event.target;
    var dropdownContainer = button.closest('.dropdown');

    // Đo đạc tọa độ của nút so với màn hình hiện tại
    var rect = button.getBoundingClientRect();
    var spaceBelow = window.innerHeight - rect.bottom;

    // Chiều cao dự kiến của cái menu (sếp có thể tăng giảm cho vừa ý, menu này cỡ 120px)
    var menuHeight = 150;

    // Nếu khoảng trống bên dưới không đủ chứa menu -> Ép nó bật ngược lên!
    if (spaceBelow < menuHeight) {
        dropdownContainer.classList.add('dropup');
    } else {
        // Nếu đủ chỗ thì cứ thả xuống như bình thường
        dropdownContainer.classList.remove('dropup');
    }
});

// Chống kẹt hiệu ứng khi menu đóng lại
document.addEventListener('hide.bs.dropdown', function (event) {
    var dropdownContainer = event.target.closest('.dropdown');
    if (dropdownContainer) {
        // Có thể để lại dòng này nếu muốn nó trả về mặc định sau khi đóng
        dropdownContainer.classList.remove('dropup'); 
    }
});