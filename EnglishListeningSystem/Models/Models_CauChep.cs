using System;
using System.Collections.Generic;

#nullable disable

namespace EnglishListeningSystem.Models
{
    public partial class CauChep
    {
        public CauChep()
        {
            LamBais = new HashSet<LamBai>();
        }

        public int MaCau { get; set; }
        public int MaBH { get; set; }
        public int ThuTuCau { get; set; }
        public string NoiDungChuan { get; set; }
        public double ThoiGianBatDau { get; set; }
        public double ThoiGianKetThuc { get; set; }
        public string GoiY { get; set; }
        public bool TrangThaiDuyet { get; set; }

        public virtual BaiHoc MaBHNavigation { get; set; }
        public virtual ICollection<LamBai> LamBais { get; set; }
    }
}
