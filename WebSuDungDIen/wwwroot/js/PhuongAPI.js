const host = "https://provinces.open-api.vn/api/v2/";

document.addEventListener("DOMContentLoaded", function () {

    // =====================================================================
    // 1. TÍNH NĂNG LỌC PHƯỜNG (DÀNH CHO TRANG INDEX)
    // =====================================================================
    const filterProvince = document.getElementById("filterProvince");
    const filterWard = document.getElementById("filterWard");

    if (filterProvince) {
        // A. Load Tỉnh
        fetch(host + "p/")
            .then(response => response.json())
            .then(data => {
                let html = '<option value="">-- Chọn Tỉnh/Thành --</option>';
                data.forEach(element => {
                    html += `<option value="${element.code}">${element.name}</option>`;
                });
                filterProvince.innerHTML = html;
            });

        // B. Chọn Tỉnh -> Xổ thẳng Phường (Dùng API V2, depth=2)
        filterProvince.addEventListener("change", function () {
            const provCode = this.value;
            if (provCode) {
                fetch(host + "p/" + provCode + "?depth=2")
                    .then(response => response.json())
                    .then(data => {
                        let html = '<option value="">-- Chọn Phường/Xã --</option>';
                        if (data.wards && data.wards.length > 0) {
                            data.wards.forEach(ward => {
                                html += `<option value="${ward.code}">${ward.name}</option>`;
                            });
                        } else {
                            html += '<option value="">(Không có dữ liệu Phường/Xã)</option>';
                        }
                        filterWard.innerHTML = html;
                    })
                    .catch(err => console.error("Lỗi API Phường:", err));
            } else {
                filterWard.innerHTML = '<option value="">-- Chọn Phường/Xã --</option>';
            }
        });
    }

    // =====================================================================
    // 2. TÍNH NĂNG CHỌN TỈNH -> PHƯỜNG & GHÉP ĐỊA CHỈ (TRANG CREATE/EDIT)
    // =====================================================================
    const provinceSelect = document.getElementById("province");
    const wardSelect = document.getElementById("ward");
    const maMienInput = document.getElementById("maMien");
    const inputDiaChiDayDu = document.getElementById("DiaChiDayDu");

    if (provinceSelect) {
        // A. Load Tỉnh
        fetch(host + "p/")
            .then(response => response.json())
            .then(data => {
                let html = '<option value="">-- Chọn Tỉnh/Thành --</option>';
                data.forEach(element => {
                    html += `<option value="${element.code}">${element.name}</option>`;
                });
                provinceSelect.innerHTML = html;
            });

        // B. Chọn Tỉnh -> Chốt Mã Miền & Load Phường/Xã
        provinceSelect.addEventListener("change", function () {
            const provinceCode = this.value;

            if (provinceCode) {
                const codeInt = parseInt(provinceCode);

                // Chốt Mã Miền EVN
                if (maMienInput) {
                    if (codeInt === 1) maMienInput.value = "PD";
                    else if (codeInt >= 2 && codeInt <= 37) maMienInput.value = "PA";
                    else if (codeInt >= 38 && codeInt <= 68) maMienInput.value = "PC";
                    else if (codeInt === 79) maMienInput.value = "PE";
                    else if (codeInt >= 70 && codeInt <= 96 && codeInt !== 79) maMienInput.value = "PB";
                }

                // Gọi API V2 lấy Phường của Tỉnh (depth=2)
                fetch(host + "p/" + provinceCode + "?depth=2")
                    .then(response => response.json())
                    .then(data => {
                        let html = '<option value="">-- Chọn Phường/Xã --</option>';
                        if (data.wards) {
                            data.wards.forEach(ward => {
                                html += `<option value="${ward.code}">${ward.name}</option>`;
                            });
                        }
                        wardSelect.innerHTML = html;
                        capNhatDiaChiDayDu();
                    });
            } else {
                wardSelect.innerHTML = '<option value="">-- Chọn Phường/Xã --</option>';
                if (maMienInput) maMienInput.value = "";
                capNhatDiaChiDayDu();
            }
        });

        function capNhatDiaChiDayDu() {
            if (!inputDiaChiDayDu) return;

            const soNha = inputDiaChi ? inputDiaChi.value.trim() : "";
            let tenTinh = provinceSelect.selectedIndex > 0 ? provinceSelect.options[provinceSelect.selectedIndex].text : "";
            let tenPhuong = wardSelect.selectedIndex > 0 ? wardSelect.options[wardSelect.selectedIndex].text : "";

            // Nối chuỗi: Số nhà, Phường, Tỉnh
            let cacPhan = [];
            if (soNha) cacPhan.push(soNha);
            if (tenPhuong) cacPhan.push(tenPhuong);
            if (tenTinh) cacPhan.push(tenTinh);

            inputDiaChiDayDu.value = cacPhan.join(", ");
        }

        // Lắng nghe sự kiện
        if (inputDiaChi) inputDiaChi.addEventListener('input', capNhatDiaChiDayDu);
        if (wardSelect) wardSelect.addEventListener('change', capNhatDiaChiDayDu);
    }
});