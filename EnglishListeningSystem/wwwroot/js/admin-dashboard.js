(function () {
    function ganDuLieuModal(modal, tenKhoa, nutChuyenModal) {
        modal.addEventListener("show.bs.modal", function (event) {
            var nutMoModal = event.relatedTarget;
            if (!nutMoModal || !nutMoModal.dataset || !nutMoModal.dataset[tenKhoa]) {
                event.preventDefault();
                return;
            }

            // Giữ mã tài khoản hoặc mã yêu cầu khi mở đúng hộp thoại.
            var taiKhoan = nutMoModal.dataset;
            var cacOMa = modal.querySelectorAll('input[name="id"]');
            for (var i = 0; i < cacOMa.length; i++) {
                cacOMa[i].value = taiKhoan[tenKhoa];
            }

            var cacMucThongTin = modal.querySelectorAll("[data-thong-tin]");
            for (var j = 0; j < cacMucThongTin.length; j++) {
                var muc = cacMucThongTin[j];
                muc.textContent = taiKhoan[muc.dataset.thongTin];
            }

            if (nutChuyenModal) {
                nutChuyenModal.dataset.yeuCauId = taiKhoan.yeuCauId;
                nutChuyenModal.dataset.hoTen = taiKhoan.hoTen;
            }
        });
    }

    ganDuLieuModal(document.getElementById("modalKhoaTaiKhoan"), "maTk");
    ganDuLieuModal(document.getElementById("modalChiTietGiangVien"), "yeuCauId",
        document.getElementById("nutKhongDuyetGiangVien"));
    ganDuLieuModal(document.getElementById("modalXacNhanHuyYeuCau"), "yeuCauId");
})();
