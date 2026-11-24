(function () {
    var modalSua = document.getElementById("modalSuaBaiHoc");
    var modalXoa = document.getElementById("modalXoaBaiHoc");

    function dienFormSua(event) {
        var nutSua = event.relatedTarget;
        if (!nutSua || !nutSua.dataset.maBai) {
            event.preventDefault();
            return;
        }

        document.getElementById("sua-ma-bai").value = nutSua.dataset.maBai;
        document.getElementById("sua-ten").value = nutSua.dataset.ten;
        document.getElementById("sua-chu-de").value = nutSua.dataset.chuDe;
        document.getElementById("sua-cap-do").value = nutSua.dataset.capDo;
        document.getElementById("sua-audio").value = nutSua.dataset.audio || "";
    }

    function dienFormXoa(event) {
        var nutXoa = event.relatedTarget;
        if (!nutXoa || !nutXoa.dataset.maBai) {
            event.preventDefault();
            return;
        }

        document.getElementById("xoa-ma-bai").value = nutXoa.dataset.maBai;
        document.getElementById("xoa-ten-bai").textContent = nutXoa.dataset.ten;
    }

    modalSua.addEventListener("show.bs.modal", dienFormSua);
    modalXoa.addEventListener("show.bs.modal", dienFormXoa);
})();
