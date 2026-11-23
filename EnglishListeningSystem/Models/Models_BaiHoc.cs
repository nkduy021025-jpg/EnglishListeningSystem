using System;
using System.Collections.Generic;

#nullable disable

namespace EnglishListeningSystem.Models
{
    public partial class BaiHoc
    {
        public BaiHoc()
        {
            CauCheps = new HashSet<CauChep>();
            DanhGias = new HashSet<DanhGia>();
            GhiChus = new HashSet<GhiChu>();
            TienDoBaiHocs = new HashSet<TienDoBaiHoc>();
            TuVungs = new HashSet<TuVung>();
        }

        public int MaBH { get; set; }
        public int MaDM { get; set; }
        public int? MaTK { get; set; }
        public string TenBH { get; set; }
        public string CapDo { get; set; }
        public string AudioUrl { get; set; }
        public string YoutubeUrl { get; set; }
        public DateTime NgayTao { get; set; }

        public virtual DanhMuc MaDMNavigation { get; set; }
        public virtual TaiKhoan MaTKNavigation { get; set; }
        public virtual ICollection<CauChep> CauCheps { get; set; }
        public virtual ICollection<DanhGia> DanhGias { get; set; }
        public virtual ICollection<GhiChu> GhiChus { get; set; }
        public virtual ICollection<TienDoBaiHoc> TienDoBaiHocs { get; set; }
        public virtual ICollection<TuVung> TuVungs { get; set; }
    }
}
