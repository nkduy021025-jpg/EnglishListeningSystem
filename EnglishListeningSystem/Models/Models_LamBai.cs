using System;
using System.Collections.Generic;

#nullable disable

namespace EnglishListeningSystem.Models
{
    public partial class LamBai
    {
        public int MaTK { get; set; }
        public int MaCau { get; set; }
        public int Lan { get; set; }
        public string TuVietSai { get; set; }
        public string TuDung { get; set; }
        public int SoLanSai { get; set; }
        public string GoiYCaiThien { get; set; }
        public DateTime NgayLam { get; set; }

        public virtual CauChep MaCauNavigation { get; set; }
        public virtual TaiKhoan MaTKNavigation { get; set; }
    }
}
