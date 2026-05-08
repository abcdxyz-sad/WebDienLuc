document.addEventListener("DOMContentLoaded", function () {
    const urlParams = new URLSearchParams(window.location.search);

    if (urlParams.get('startTour') === 'true') {

        // ==========================================
        // 💥 FIX LỖI TỊT NGÒI: DÙNG API MỚI CỦA INTRO.JS
        // ==========================================
        const tour = introJs.tour();

        let dynamicSteps = [];

        // 1. Cutscene mở màn
        let welcomeBox = document.getElementById('zzz-welcome-msg');
        if (welcomeBox) {
            dynamicSteps.push({
                intro: welcomeBox.getAttribute('data-msg'),
                tooltipClass: 'zzz-cutscene-box'
            });
        }

        // 2. Gom và lọc trùng các thẻ data-intro
        let introNodes = document.querySelectorAll('[data-intro]');
        let uniqueStepsMap = new Map();
        introNodes.forEach(function (el) {
            let stepNum = el.getAttribute('data-step');
            if (stepNum && !uniqueStepsMap.has(stepNum)) {
                uniqueStepsMap.set(stepNum, el);
            }
        });

        let elementsArray = Array.from(uniqueStepsMap.values());
        elementsArray.sort(function (a, b) {
            return parseInt(a.getAttribute('data-step')) - parseInt(b.getAttribute('data-step'));
        });

        elementsArray.forEach(function (el) {
            dynamicSteps.push({
                element: el,
                intro: el.getAttribute('data-intro'),
                position: el.getAttribute('data-position') || 'bottom',
                tooltipClass: el.getAttribute('data-tooltip-class') || ''
            });
        });

        // ==========================================
        // CẤU HÌNH INTRO.JS
        // ==========================================
        tour.setOptions({
            steps: dynamicSteps,
            helperElementPadding: 0,
            nextLabel: 'NEXT >',
            prevLabel: '< BACK',
            skipLabel: '[X] ESC',
            doneLabel: 'FINISH',
            showProgress: true,
            scrollToElement: false,
            disableScroll: true
        });

        // ==========================================
        // BÙA NGĂN BOOTSTRAP ĐÓNG DROPDOWN
        // ==========================================
        let currentActiveDropdown = null;
        const preventBootstrapClose = function (e) {
            if (currentActiveDropdown && currentActiveDropdown.contains(e.target)) {
                e.preventDefault();
            }
        };
        document.addEventListener('hide.bs.dropdown', preventBootstrapClose);

        // ==========================================
        // XỬ LÝ CHUYỂN BƯỚC
        // ==========================================
        tour.onbeforechange(function (targetElement) {
            if (!targetElement) return;
            let loaiSelect = document.getElementById("loaiSelect");

            // 💥 BÙA GIẢI CỨU Z-INDEX: DIỆT WEB ANIMATIONS API 💥
            // Hàm này sẽ ngay lập tức ép animation chạy xong và xóa bỏ transform giam cầm
            const clearAnimationTrap = (elementId) => {
                let el = document.getElementById(elementId);
                if (el) {
                    // Ép tất cả animation của Web Animations API kết thúc ngay lập tức
                    el.getAnimations().forEach(anim => anim.finish());
                    // Gỡ bỏ hoàn toàn transform để Intro.js có thể nhấc phần tử lên trên màn đen
                    el.style.transform = 'none';
                }
            };

            // 1. Nếu phần tử chuẩn bị soi nằm trong Form NHÂN VIÊN
            if (targetElement.closest('#nhanVienFields')) {
                if (loaiSelect.value !== "NhanVien") {
                    loaiSelect.value = "NhanVien";
                    loaiSelect.dispatchEvent(new Event('change'));
                }
                // Sau khi Form mở ra (hoặc nếu đã mở sẵn), lập tức giải cứu z-index
                clearAnimationTrap('nhanVienFields');
            }
            // 2. Nếu phần tử chuẩn bị soi nằm trong Form KHÁCH HÀNG
            else if (targetElement.closest('#khachHangFields')) {
                if (loaiSelect.value !== "KhachHang") {
                    loaiSelect.value = "KhachHang";
                    loaiSelect.dispatchEvent(new Event('change'));
                }
                // Sau khi Form mở ra (hoặc nếu đã mở sẵn), lập tức giải cứu z-index
                clearAnimationTrap('khachHangFields');
            }
            // --- 0. Dọn dẹp Select ở bước cũ ---
            document.querySelectorAll('select').forEach(s => {
                s.setAttribute('size', '1');
                s.style.position = '';
                s.style.zIndex = '';
                s.style.boxShadow = 'none';
                s.style.outline = 'none';

                // Trả lại giá trị cũ nếu người dùng chưa chọn gì thực sự
                if (s.dataset.originalIndex !== undefined) {
                    // Chỉ reset nếu thẻ select đang bị ép highlight bởi tour
                    if (s.getAttribute('data-tour-highlighting') === 'true') {
                        s.selectedIndex = s.dataset.originalIndex;
                        s.removeAttribute('data-tour-highlighting');
                    }
                }
            });

            // --- 1. XỬ LÝ DROPDOWN BOOTSTRAP ---
            let targetDropdown = targetElement.closest('.dropdown');
            if (targetDropdown) {
                currentActiveDropdown = targetDropdown;
                let toggleBtn = targetDropdown.querySelector('.dropdown-toggle');
                let menu = targetDropdown.querySelector('.dropdown-menu');

                targetDropdown.style.position = 'relative';
                targetDropdown.style.zIndex = '9999999';

                if (toggleBtn) {
                    toggleBtn.classList.add('show');
                    toggleBtn.setAttribute('aria-expanded', 'true');
                }
                if (menu) {
                    menu.classList.add('show');
                    menu.setAttribute('data-bs-popper', 'none');
                    menu.style.zIndex = '9999999';
                }
            } else {
                currentActiveDropdown = null;
            }

            // --- 2. XỬ LÝ THẺ SELECT ("Bung" listbox và "Khoanh" item) ---
            let selectEl = targetElement.tagName === "SELECT" ? targetElement : targetElement.querySelector('select');

            if (selectEl) {
                // Đếm số option để bung chiều cao cho hợp lý (tối đa 5 dòng)
                let optCount = selectEl.options.length;
                let sizeToSet = optCount > 5 ? 5 : optCount;
                selectEl.setAttribute('size', sizeToSet.toString());

                // Style nổi bật vùng Select
                selectEl.style.position = 'relative';
                selectEl.style.zIndex = '9999999';
                selectEl.style.backgroundColor = '#fff';
                selectEl.style.outline = '4px solid rgba(40, 167, 69, 0.6)'; // Khoanh vùng xanh lá nổi bật bên ngoài
                selectEl.style.borderRadius = '4px';

                // Lưu lại vị trí thực tế đang chọn (trước khi tour can thiệp)
                if (!selectEl.hasAttribute('data-original-index')) {
                    selectEl.dataset.originalIndex = selectEl.selectedIndex;
                }

                // Ép bôi đen (khoanh vùng) vào mục số 1 (bỏ qua mục 0 thường là placeholder "-- CHỌN --")
                if (optCount > 1) {
                    selectEl.selectedIndex = 1;
                    selectEl.setAttribute('data-tour-highlighting', 'true'); // Đánh dấu là đang bị tour ép chọn tạm thời
                }

                selectEl.focus(); // Jump trỏ chuột vào thẳng select

                // Bắt sự kiện khi sếp CHỦ ĐỘNG click chọn 1 mục
                selectEl.addEventListener('change', function () {
                    // Người dùng đã tự chọn -> Gỡ mác "ép chọn tạm thời"
                    this.removeAttribute('data-tour-highlighting');

                    // Thu nhỏ select lại
                    this.setAttribute('size', '1');
                    this.style.outline = 'none';
                    tour.refresh(); // Làm mới viền Intro.js
                }, { once: true });
            }

            // --- 3. XỬ LÝ CUỘN TRANG (SCROLL) ---
            let position = targetElement.getAttribute('data-position') || 'bottom';
            let rect = targetElement.getBoundingClientRect();
            let absoluteTop = rect.top + window.scrollY;
            let vHeight = window.innerHeight;
            let scrollToY;

            if (position.includes('bottom')) {
                scrollToY = absoluteTop - (vHeight * 0.2);
            } else if (position.includes('top')) {
                scrollToY = absoluteTop - (vHeight * 0.6);
            } else {
                scrollToY = absoluteTop - (vHeight / 2) + (rect.height / 2);
            }
            window.scrollTo({ top: scrollToY, behavior: 'smooth' });

            // --- 4. DỌN DẸP DROPDOWN KHÁC ---
            document.querySelectorAll('.dropdown').forEach(drop => {
                if (drop !== targetDropdown) {
                    drop.style.zIndex = '';
                    let btn = drop.querySelector('.dropdown-toggle');
                    let m = drop.querySelector('.dropdown-menu');

                    if (btn) {
                        btn.classList.remove('show');
                        btn.setAttribute('aria-expanded', 'false');
                    }
                    if (m) {
                        m.classList.remove('show');
                        m.style.zIndex = '';
                    }
                }
            });
        });

        tour.onafterchange(function (targetElement) {
            let tooltipContainer = document.querySelector('.introjs-tooltip');
            if (tooltipContainer && targetElement) {
                tooltipContainer.classList.remove('force-bottom-screen');
                let currentStep = targetElement.getAttribute('data-step');
                if (currentStep === '3') {
                    tooltipContainer.classList.add('force-bottom-screen');
                }
            }
        });

        tour.onchange(function () {
            setTimeout(function () {
                tour.refresh();
            }, 350);
        });

        // ==========================================
        // 💥 HÀM DỌN DẸP & ĐIỀU HƯỚNG THÔNG MINH
        // ==========================================
        function finishTour(status) {
            document.removeEventListener('hide.bs.dropdown', preventBootstrapClose);
            currentActiveDropdown = null;

            document.querySelectorAll('.dropdown').forEach(drop => {
                drop.style.zIndex = '';
                let btn = drop.querySelector('.dropdown-toggle');
                if (btn) {
                    btn.classList.remove('show');
                    btn.setAttribute('aria-expanded', 'false');
                }
                let menu = drop.querySelector('.dropdown-menu');
                if (menu) {
                    menu.classList.remove('show');
                    menu.style.zIndex = '';
                }
            });

            // Dọn dẹp Select & Trả về trạng thái thực
            document.querySelectorAll('select').forEach(s => {
                s.setAttribute('size', '1');
                s.style.position = '';
                s.style.zIndex = '';
                s.style.boxShadow = 'none';
                s.style.outline = 'none';

                if (s.getAttribute('data-tour-highlighting') === 'true') {
                    s.selectedIndex = s.dataset.originalIndex !== undefined ? s.dataset.originalIndex : 0;
                }
                s.removeAttribute('data-tour-highlighting');
                s.removeAttribute('data-original-index');
            });

            const url = new URL(window.location);
            const currentMaPhuong = url.searchParams.get('maPhuongApi');
            const fullPath = window.location.pathname.toLowerCase();

            url.searchParams.delete('startTour');
            window.history.replaceState(null, null, url.toString());

            let message = "";
            let targetUrl = "";

            if (fullPath.includes('/chisodien/create')) {
                message = status === "done"
                    ? "Hướng dẫn thêm mới đã xong. Bạn có muốn quay về danh sách Chỉ Số Điện không?"
                    : "Đã thoát hướng dẫn. Quay về danh sách Chỉ Số Điện nhé?";
                targetUrl = "/ChiSoDien";
            } else {
                message = status === "done"
                    ? "Hướng dẫn đã hoàn tất! Trở về trang Cẩm Nang (FAQ) chứ?"
                    : "Đang xem dở mà? Bạn có muốn trở về trang Cẩm Nang (FAQ) không?";
                targetUrl = "/Identity/FAQ";
            }

            if (confirm(message)) {
                if (targetUrl === "/ChiSoDien" && currentMaPhuong) {
                    window.location.href = targetUrl + "?maPhuongApi=" + currentMaPhuong;
                } else {
                    window.location.href = targetUrl;
                }
            }
        }

        let isTourFinished = false;

        tour.oncomplete(function () {
            isTourFinished = true;
            finishTour("done");
        });

        tour.onexit(function () {
            if (isTourFinished) return;
            finishTour("exit");
        });

        tour.start();

        let resizeTimer;
        window.addEventListener('resize', function () {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function () {
                tour.refresh();
            }, 200);
        });
    }
});