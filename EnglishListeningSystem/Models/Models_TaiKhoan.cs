using System;
using System.Collections.Generic;

#nullable disable

namespace EnglishListeningSystem.Models
{
    public partial class TaiKhoan
    {
        public TaiKhoan()
        {
            BaiHocs = new HashSet<BaiHoc>();
            DanhGias = new HashSet<DanhGia>();
            GhiChus = new HashSet<GhiChu>();
            LamBais = new HashSet<LamBai>();
            LoTrinhs = new HashSet<LoTrinh>();
            TienDoBaiHocs = new HashSet<TienDoBaiHoc>();
            TuVungs = new HashSet<TuVung>();
        }

        public int MaTK { get; set; }
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string VaiTro { get; set; }
        public int DiemTichLuy { get; set; }
        public bool TrangThaiTK { get; set; }

        public virtual ICollection<BaiHoc> BaiHocs { get; set; }
        public virtual ICollection<DanhGia> DanhGias { get; set; }
        public virtual ICollection<GhiChu> GhiChus { get; set; }
        public virtual ICollection<LamBai> LamBais { get; set; }
        public virtual ICollection<LoTrinh> LoTrinhs { get; set; }
        public virtual ICollection<TienDoBaiHoc> TienDoBaiHocs { get; set; }
        public virtual ICollection<TuVung> TuVungs { get; set; }
    }
}
