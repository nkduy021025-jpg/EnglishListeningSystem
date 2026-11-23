using System;
using System.Collections.Generic;

#nullable disable

namespace EnglishListeningSystem.Models
{
    public partial class TuVung
    {
        public int MaTV { get; set; }
        public int MaTK { get; set; }
        public int? MaBH { get; set; }
        public string TuTiengAnh { get; set; }
        public string PhienAm { get; set; }
        public string NghiaNguCanh { get; set; }
        public string ViDu { get; set; }
        public bool TrangThaiOnTap { get; set; }

        public virtual BaiHoc MaBHNavigation { get; set; }
        public virtual TaiKhoan MaTKNavigation { get; set; }
    }
}
