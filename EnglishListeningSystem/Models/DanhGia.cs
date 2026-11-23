using System.ComponentModel.DataAnnotations;

namespace EnglishListeningSystem.Models;

public class DanhGia
{
    public int MaDG { get; set; }
    public int MaTK { get; set; }
    public int MaBH { get; set; }

    [Required]
    [Range(1, 5)]
    public int SoSao { get; set; }

    [MaxLength(500)]
    public string? NhanXet { get; set; }

    public DateTime NgayDanhGia { get; set; } = DateTime.Now;

    public virtual TaiKhoan MaTKNavigation { get; set; } = null!;
    public virtual BaiHoc MaBHNavigation { get; set; } = null!;
}
