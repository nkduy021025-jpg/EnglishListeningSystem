using System;
using System.Collections.Generic;

#nullable disable

namespace EnglishListeningSystem.Models
{
    public partial class LoTrinh
    {
        public int MaLT { get; set; }
        public int MaTK { get; set; }
        public int? MaDM { get; set; }
        public string TenLT { get; set; }
        public string TrangThaiLT { get; set; }
        public int CauHienTai { get; set; }
        public DateTime NgayCapNhat { get; set; }

        public virtual DanhMuc MaDMNavigation { get; set; }
        public virtual TaiKhoan MaTKNavigation { get; set; }
    }
}
