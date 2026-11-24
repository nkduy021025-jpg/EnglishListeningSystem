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
            List<DanhMuc> dsDanhMuc = await _context.DanhMucs
                .Include(dm => dm.BaiHocs)
                .Where(dm => dm.TrangThaiHienThi == true)
                .ToListAsync();

            return View(dsDanhMuc);
        }

        public Task<IActionResult> Baihoc(int id, string? search, string? level)
        {
            return DanhSachBaiHoc(id, search, level);
        }

        public async Task<IActionResult> DanhSachBaiHoc(int id, string? search, string? level)
        {
            DanhMuc? danhMuc = await _context.DanhMucs
                .AsNoTracking()
                .Include(item => item.BaiHocs)
                    .ThenInclude(lesson => lesson.DanhGias)
                .SingleOrDefaultAsync(item => item.MaDM == id && item.TrangThaiHienThi);

            if (danhMuc == null)
            {
                return NotFound();
            }

            search = (search ?? string.Empty).Trim();
            level = (level ?? string.Empty).Trim();

            IEnumerable<string> capDoCuaCacBai = danhMuc.BaiHocs.Select(bh => bh.CapDo);
            string[] dsCapDo = capDoCuaCacBai
                .Where(capDo => !string.IsNullOrWhiteSpace(capDo))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(capDo => capDo, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            IQueryable<CauChep> dsCauDaDuyet = _context.CauCheps
                .Where(cau => cau.TrangThaiDuyet && cau.MaBHNavigation.MaDM == id);
            var nhomCauTheoBai = dsCauDaDuyet.GroupBy(cau => cau.MaBH);
            var thongKeSoCau = nhomCauTheoBai.Select(nhom => new { MaBH = nhom.Key, SoCau = nhom.Count() });
            Dictionary<int, int> soCauTheoBai = await thongKeSoCau
                .ToDictionaryAsync(item => item.MaBH, item => item.SoCau);

            // Lọc theo tên bài và cấp độ trước khi hiện danh sách.
            IEnumerable<BaiHoc> dsBaiPhuHop = danhMuc.BaiHocs
                .Where(bh => bh.TenBH.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                    && (level.Length == 0 || string.Equals(bh.CapDo, level, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(bh => bh.MaBH);

            List<StudentLesson> dsBaiHoc = new List<StudentLesson>();
            foreach (BaiHoc bh in dsBaiPhuHop)
            {
                int soCau = soCauTheoBai.GetValueOrDefault(bh.MaBH);
                double diemTrungBinh = TinhDiemTrungBinh(bh.DanhGias);
                StudentLesson baiHoc = new StudentLesson(
                    bh.MaBH, bh.TenBH, bh.CapDo, soCau, diemTrungBinh, bh.DanhGias.Count);
                dsBaiHoc.Add(baiHoc);
            }

            StudentLessonsViewModel model = new StudentLessonsViewModel(
                id, danhMuc.TenDM, search, level, dsCapDo, dsBaiHoc);
            return View(nameof(DanhSachBaiHoc), model);
        }

        [HttpGet]
        public async Task<IActionResult> LuyenNghe(int id, int thuTu = 1)
        {
            IQueryable<BaiHoc> baiHocHienThi = _context.BaiHocs
                .Where(bh => bh.MaBH == id && bh.MaDMNavigation.TrangThaiHienThi);
            var baiHoc = await baiHocHienThi
                .Select(bh => new
                {
                    bh.MaBH,
                    bh.MaDM,
                    bh.MaDMNavigation.TenDM,
                    bh.TenBH,
                    bh.CapDo,
                    bh.AudioUrl,
                    bh.YoutubeUrl,
                    DsCau = bh.CauCheps
                        .Where(cau => cau.TrangThaiDuyet)
                        .OrderBy(cau => cau.ThuTuCau)
                        .ThenBy(cau => cau.MaCau)
                        .Select(cau => new DictationSentence(cau.NoiDungChuan, cau.ThoiGianBatDau, cau.ThoiGianKetThuc))
                        .ToList()
                })
                .SingleOrDefaultAsync();

            if (baiHoc == null)
            {
                return NotFound();
            }

            List<DanhGia> dsDanhGia = await _context.DanhGias.AsNoTracking()
                .Where(item => item.MaBH == id)
                .Include(item => item.MaTKNavigation)
                .OrderByDescending(item => item.NgayDanhGia)
                .ThenByDescending(item => item.MaDG)
                .ToListAsync();

            DanhGia? danhGiaHienTai = null;
            string? maTaiKhoan = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(maTaiKhoan, out int maTK))
            {
                danhGiaHienTai = dsDanhGia.Find(item => item.MaTK == maTK);
            }

            ViewBag.DiemDanhGiaTrungBinh = TinhDiemTrungBinh(dsDanhGia);
            ViewBag.TongLuotDanhGia = dsDanhGia.Count;
            ViewBag.DanhGiaHienTai = danhGiaHienTai;
            ViewBag.DanhGiaMoiNhat = dsDanhGia.Take(10).ToList();

            int viTriMax = Math.Max(1, baiHoc.DsCau.Count);
            int viTriCau = Math.Clamp(thuTu, 1, viTriMax);
            StudentPracticeViewModel model = new StudentPracticeViewModel(
                baiHoc.MaBH, baiHoc.MaDM, baiHoc.TenDM, baiHoc.TenBH, baiHoc.CapDo,
                baiHoc.AudioUrl, baiHoc.YoutubeUrl, baiHoc.DsCau, viTriCau);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DanhGiaBaiHoc(int maBH, int thuTu, int soSao, string? nhanXet)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int maTK) || maTK <= 0)
            {
                return Forbid();
            }

            bool baiHocHienThi = await _context.BaiHocs
                .AnyAsync(bh => bh.MaBH == maBH && bh.MaDMNavigation.TrangThaiHienThi);
            if (baiHocHienThi == false)
            {
                return NotFound();
            }

            bool nhanXetQuaDai = nhanXet != null && nhanXet.Length > 500;
            if (soSao < 1 || soSao > 5 || nhanXetQuaDai)
            {
                TempData["DanhGiaError"] = "Hãy chọn từ 1 đến 5 sao và nhập nhận xét tối đa 500 ký tự.";
                return RedirectToAction(nameof(LuyenNghe), new { id = maBH, thuTu });
            }

            if (string.IsNullOrWhiteSpace(nhanXet))
            {
                nhanXet = null;
            }
            else
            {
                nhanXet = nhanXet.Trim();
            }

            DateTime ngayDanhGia = DateTime.Now;

            // Nếu đã đánh giá bài này, MySQL cập nhật đánh giá cũ.
            FormattableString lenhSql = $@"INSERT INTO `DanhGia` (`MaTK`, `MaBH`, `SoSao`, `NhanXet`, `NgayDanhGia`)
                VALUES ({maTK}, {maBH}, {soSao}, {nhanXet}, {ngayDanhGia})
                ON DUPLICATE KEY UPDATE `SoSao` = {soSao}, `NhanXet` = {nhanXet}, `NgayDanhGia` = {ngayDanhGia}";
            await _context.Database.ExecuteSqlInterpolatedAsync(lenhSql);

            TempData["DanhGiaSuccess"] = "Đã lưu đánh giá của bạn.";
            return RedirectToAction(nameof(LuyenNghe), new { id = maBH, thuTu });
        }

        private static double TinhDiemTrungBinh(ICollection<DanhGia> danhGias)
        {
            if (danhGias.Count == 0)
            {
                return 0;
            }

            double diemTrungBinh = danhGias.Average(dg => dg.SoSao);
            // Làm tròn điểm để hiển thị một chữ số thập phân.
            return Math.Round(diemTrungBinh, 1, MidpointRounding.AwayFromZero);
        }
    }
}
