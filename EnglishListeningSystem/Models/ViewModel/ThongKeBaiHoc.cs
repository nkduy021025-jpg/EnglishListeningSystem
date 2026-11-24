namespace EnglishListeningSystem.Models.ViewModel
{
    public sealed class ThongKeBaiHoc
    {
        public string TenBaiHoc { get; }
        public string DanhMuc { get; }
        public string CapDo { get; }
        public int SoHocVien { get; }
        public long TongLuotLuyenNghe { get; }

        public ThongKeBaiHoc(
            string tenBaiHoc,
            string danhMuc,
            string capDo,
            int soHocVien,
            long tongLuotLuyenNghe)
        {
            TenBaiHoc = tenBaiHoc;
            DanhMuc = danhMuc;
            CapDo = capDo;
            SoHocVien = soHocVien;
            TongLuotLuyenNghe = tongLuotLuyenNghe;
        }
    }
}
