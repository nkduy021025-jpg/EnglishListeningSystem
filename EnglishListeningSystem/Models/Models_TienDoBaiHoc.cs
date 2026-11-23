using System;
using System.Collections.Generic;

#nullable disable

namespace EnglishListeningSystem.Models
{
    public partial class TienDoBaiHoc
    {
        public int MaTK { get; set; }
        public int MaBH { get; set; }
        public decimal MucDoHoanThanh { get; set; }
        public DateTime NgayCapNhat { get; set; }

        public virtual BaiHoc MaBHNavigation { get; set; }
        public virtual TaiKhoan MaTKNavigation { get; set; }
    }
}
