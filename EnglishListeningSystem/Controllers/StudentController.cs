using EnglishListeningSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EnglishListeningSystem.Controllers
{
    
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var dsDanhMuc = _context.DanhMucs
                .Include(dm => dm.BaiHocs)
                .Where(dm => dm.TrangThaiHienThi == true)
                .ToList();

            return View(dsDanhMuc);
        }

        
        public Task<IActionResult> Baihoc(int id, string? search, string? level)
            => DanhSachBaiHoc(id, search, level);

        public async Task<IActionResult> DanhSachBaiHoc(int id, string? search, string? level)
        {
            var category = await _context.DanhMucs
                .AsNoTracking()
                .Include(item => item.BaiHocs)
                    .ThenInclude(lesson => lesson.DanhGias)
                .SingleOrDefaultAsync(item => item.MaDM == id && item.TrangThaiHienThi);

            if (category == null)
            {
                return NotFound();
            }

            search = search?.Trim() ?? string.Empty;
            level = level?.Trim() ?? string.Empty;
            var levels = category.BaiHocs.Select(lesson => lesson.CapDo)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var soCauTheoBai = await _context.CauCheps
                .Where(cau => cau.TrangThaiDuyet && cau.MaBHNavigation.MaDM == id)
                .GroupBy(cau => cau.MaBH)
                .Select(group => new { MaBH = group.Key, SoCau = group.Count() })
                .ToDictionaryAsync(item => item.MaBH, item => item.SoCau);

            var lessons = category.BaiHocs
                .Where(lesson => lesson.TenBH.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                    && (level.Length == 0 || string.Equals(lesson.CapDo, level, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(lesson => lesson.MaBH)
                .Select(lesson => new StudentLesson(lesson.MaBH, lesson.TenBH, lesson.CapDo,
                    soCauTheoBai.GetValueOrDefault(lesson.MaBH), TinhDiemTrungBinh(lesson.DanhGias), lesson.DanhGias.Count))
                .ToArray();

            return View(nameof(DanhSachBaiHoc), new StudentLessonsViewModel(id, category.TenDM, search, level, levels, lessons));
        }

        [HttpGet]
        public async Task<IActionResult> LuyenNghe(int id, int thuTu = 1)
        {
            var lesson = await _context.BaiHocs
                .Where(item => item.MaBH == id && item.MaDMNavigation.TrangThaiHienThi)
                .Select(item => new
                {
                    item.MaBH,
                    item.MaDM,
                    item.MaDMNavigation.TenDM,
                    item.TenBH,
                    item.CapDo,
                    item.AudioUrl,
                    item.YoutubeUrl,
                    Sentences = item.CauCheps.Where(cau => cau.TrangThaiDuyet)
                        .OrderBy(cau => cau.ThuTuCau)
                        .ThenBy(cau => cau.MaCau)
                        .Select(cau => new DictationSentence(cau.NoiDungChuan, cau.ThoiGianBatDau, cau.ThoiGianKetThuc))
                        .ToList()
                })
                .SingleOrDefaultAsync();

            if (lesson == null)
            {
                return NotFound();
            }

            var danhGias = await _context.DanhGias.AsNoTracking()
                .Where(item => item.MaBH == id)
                .Include(item => item.MaTKNavigation)
                .OrderByDescending(item => item.NgayDanhGia)
                .ThenByDescending(item => item.MaDG)
                .ToListAsync();

            ViewBag.DiemDanhGiaTrungBinh = TinhDiemTrungBinh(danhGias);
            ViewBag.TongLuotDanhGia = danhGias.Count;
            ViewBag.DanhGiaHienTai = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var maTK)
                ? danhGias.Find(item => item.MaTK == maTK)
                : null;
            ViewBag.DanhGiaMoiNhat = danhGias.Take(10).ToList();

            var viTriCau = Math.Clamp(thuTu, 1, Math.Max(1, lesson.Sentences.Count));
            return View(new StudentPracticeViewModel(lesson.MaBH, lesson.MaDM, lesson.TenDM,
                lesson.TenBH, lesson.CapDo, lesson.AudioUrl, lesson.YoutubeUrl, lesson.Sentences, viTriCau));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DanhGiaBaiHoc(int maBH, int thuTu, int soSao, string? nhanXet)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var maTK) || maTK <= 0)
            {
                return Forbid();
            }

            if (!await _context.BaiHocs.AnyAsync(item => item.MaBH == maBH && item.MaDMNavigation.TrangThaiHienThi))
            {
                return NotFound();
            }

            if (soSao is < 1 or > 5 || nhanXet?.Length > 500)
            {
                TempData["DanhGiaError"] = "Hãy chọn từ 1 đến 5 sao và nhập nhận xét tối đa 500 ký tự.";
                return RedirectToAction(nameof(LuyenNghe), new { id = maBH, thuTu });
            }

            nhanXet = string.IsNullOrWhiteSpace(nhanXet) ? null : nhanXet.Trim();
            var ngayDanhGia = DateTime.Now;

            // MySQL thêm hoặc cập nhật trong một lệnh, kể cả khi gửi đồng thời.
            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO `DanhGia` (`MaTK`, `MaBH`, `SoSao`, `NhanXet`, `NgayDanhGia`)
                VALUES ({maTK}, {maBH}, {soSao}, {nhanXet}, {ngayDanhGia})
                ON DUPLICATE KEY UPDATE `SoSao` = {soSao}, `NhanXet` = {nhanXet}, `NgayDanhGia` = {ngayDanhGia}
                """);

            TempData["DanhGiaSuccess"] = "Đã lưu đánh giá của bạn.";
            return RedirectToAction(nameof(LuyenNghe), new { id = maBH, thuTu });
        }

        private static double TinhDiemTrungBinh(ICollection<DanhGia> danhGias)
            => danhGias.Count == 0 ? 0 : Math.Round(danhGias.Average(item => item.SoSao), 1, MidpointRounding.AwayFromZero);
    }
}
