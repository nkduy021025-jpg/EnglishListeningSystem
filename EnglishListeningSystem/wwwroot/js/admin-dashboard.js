(() => {
    const modal = document.getElementById("modalKhoaTaiKhoan");
    const maTaiKhoan = modal.querySelector('input[name="id"]');
    const thongTinHocVien = modal.querySelectorAll("[data-thong-tin]");

    // Lấy thông tin từ nút khóa vừa được bấm và điền vào hộp xác nhận.
    modal.addEventListener("show.bs.modal", event => {
        const hocVien = event.relatedTarget?.dataset;
        if (!hocVien?.maTk) {
            event.preventDefault();
            return;
        }

        maTaiKhoan.value = hocVien.maTk;
        thongTinHocVien.forEach(muc => {
            muc.textContent = hocVien[muc.dataset.thongTin];
        });
    });
})();
