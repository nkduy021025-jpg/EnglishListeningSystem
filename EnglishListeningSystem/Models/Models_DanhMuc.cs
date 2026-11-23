using System;
using System.Collections.Generic;

#nullable disable

namespace EnglishListeningSystem.Models
{
    public partial class DanhMuc
    {
        public DanhMuc()
        {
            BaiHocs = new HashSet<BaiHoc>();
            LoTrinhs = new HashSet<LoTrinh>();
        }

        public int MaDM { get; set; }
        public string TenDM { get; set; }
        public string MoTa { get; set; }
        public bool TrangThaiHienThi { get; set; }

        public virtual ICollection<BaiHoc> BaiHocs { get; set; }
        public virtual ICollection<LoTrinh> LoTrinhs { get; set; }
    }
}
