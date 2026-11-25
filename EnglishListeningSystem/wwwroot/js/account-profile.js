function chuyenHienThiMatKhau(suKien) {
    var nut = suKien.currentTarget;
    var oMatKhau = document.getElementById(nut.getAttribute("aria-controls"));
    var bieuTuong = nut.querySelector(".bi");

    // Mỗi nút chỉ thay đổi ô mật khẩu mà nút đó điều khiển.
    if (oMatKhau.type === "password") {
        oMatKhau.type = "text";
        bieuTuong.classList.replace("bi-eye", "bi-eye-slash");
        nut.setAttribute("aria-pressed", "true");
    } else {
        oMatKhau.type = "password";
        bieuTuong.classList.replace("bi-eye-slash", "bi-eye");
        nut.setAttribute("aria-pressed", "false");
    }
}

var cacNutHienMatKhau = document.querySelectorAll("[data-password-toggle]");
for (var i = 0; i < cacNutHienMatKhau.length; i++) {
    cacNutHienMatKhau[i].addEventListener("click", chuyenHienThiMatKhau);
}
