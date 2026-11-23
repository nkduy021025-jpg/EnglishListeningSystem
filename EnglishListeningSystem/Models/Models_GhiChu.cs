using System;
using System.Collections.Generic;

#nullable disable

namespace EnglishListeningSystem.Models
{
    public partial class GhiChu
    {
        public int MaGC { get; set; }
        public int MaTK { get; set; }
        public int MaBH { get; set; }
        public string NoiDungGC { get; set; }
        public DateTime NgayTaoGC { get; set; }

        public virtual BaiHoc MaBHNavigation { get; set; }
        public virtual TaiKhoan MaTKNavigation { get; set; }
    }
}
