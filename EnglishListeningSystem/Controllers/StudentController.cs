using EnglishListeningSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Security.Claims;

namespace EnglishListeningSystem.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly KhoAmThanhBaiHoc _khoAmThanh;

        public StudentController(ApplicationDbContext context, KhoAmThanhBaiHoc khoAmThanh)
        {
            _context = context;
            _khoAmThanh = khoAmThanh;
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
        public Task<IActionResult> LuyenNghe(int id, int thuTu = 1)
        {
            return Lesson(id, thuTu);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Lesson(int id, int thuTu = 1)
        {
            IQueryable<BaiHoc> baiHocHienThi = _context.BaiHocs.AsNoTracking()
                .Where(bh => bh.MaBH == id && bh.MaDMNavigation.TrangThaiHienThi)
                .Include(bh => bh.MaDMNavigation)
                .Include(bh => bh.CauCheps);
            BaiHoc? baiHoc = await baiHocHienThi.SingleOrDefaultAsync();
            if (baiHoc == null)
            {
                return NotFound();
            }

            if (baiHoc.CauCheps.Count == 0)
            {
                string? duongDanSrt = _khoAmThanh.TimFileSrt(baiHoc.AudioUrl);
                if (duongDanSrt != null)
                {
                    try
                    {
                        List<CauChep> cacCau = KhoAmThanhBaiHoc.DocFileSrt(duongDanSrt);
                        await NhapPhuDeNeuTrong(id, baiHoc.AudioUrl, cacCau);
                    }
                    catch (InvalidDataException loi)
                    {
                        ViewBag.LoiPhuDe = loi.Message;
                    }
                    catch (Exception loi) when (loi is IOException || loi is UnauthorizedAccessException)
                    {
                        ViewBag.LoiPhuDe = "The subtitles could not be read. Please try again later.";
                    }

                    baiHoc = await baiHocHienThi.SingleOrDefaultAsync();
                    if (baiHoc == null)
                    {
                        return NotFound();
                    }
                }
            }

            List<CauChep> dsCau = baiHoc.CauCheps.Where(cau => cau.TrangThaiDuyet)
                .OrderBy(cau => cau.ThuTuCau).ThenBy(cau => cau.MaCau).ToList();
            List<DanhGia> dsDanhGia = await _context.DanhGias.AsNoTracking()
                .Where(item => item.MaBH == id)
                .Include(item => item.MaTKNavigation)
                .OrderByDescending(item => item.NgayDanhGia)
                .ThenByDescending(item => item.MaDG)
                .ToListAsync();

            DanhGia? danhGiaHienTai = null;
            List<int> cacCauDaXong = new List<int>();
            string? maTaiKhoan = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(maTaiKhoan, out int maTK))
            {
                danhGiaHienTai = dsDanhGia.Find(item => item.MaTK == maTK);
                cacCauDaXong = await TruyVanCauDaXong(maTK, id).ToListAsync();
            }

            ViewBag.DiemDanhGiaTrungBinh = TinhDiemTrungBinh(dsDanhGia);
            ViewBag.TongLuotDanhGia = dsDanhGia.Count;
            ViewBag.DanhGiaHienTai = danhGiaHienTai;
            ViewBag.DanhGiaMoiNhat = dsDanhGia.Take(10).ToList();

            ViewBag.DanhSachCau = dsCau;
            ViewBag.CacCauDaXong = cacCauDaXong;
            ViewBag.ViTriCau = Math.Clamp(thuTu, 1, Math.Max(1, dsCau.Count));
            ViewBag.DiemTienDo = TinhTienDo(cacCauDaXong.Count, dsCau.Count);
            ViewBag.CoAudio = _khoAmThanh.TimFileMp3(baiHoc.AudioUrl) != null;
            return View(nameof(Lesson), baiHoc);
        }

        [Authorize]
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> PhatAudio(int maBH)
        {
            string? tenFile = await _context.BaiHocs.AsNoTracking()
                .Where(bh => bh.MaBH == maBH && bh.MaDMNavigation.TrangThaiHienThi)
                .Select(bh => bh.AudioUrl)
                .SingleOrDefaultAsync();
            string? duongDan = _khoAmThanh.TimFileMp3(tenFile);
            if (duongDan == null)
            {
                return NotFound();
            }

            return PhysicalFile(duongDan, "audio/mpeg", enableRangeProcessing: true);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(32 * 1024)]
        public async Task<IActionResult> KiemTraTranscript(int maCau, string noiDungNhap)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int maTK) || maTK <= 0)
            {
                return Forbid();
            }
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(noiDungNhap)
                || noiDungNhap.Length > KhoAmThanhBaiHoc.GioiHanTranscript)
            {
                return BadRequest(new { message = "Enter a transcript of up to 2,000 characters." });
            }

            int? maBH = await _context.CauCheps.AsNoTracking()
                .Where(cau => cau.MaCau == maCau && cau.TrangThaiDuyet && cau.MaBHNavigation.MaDMNavigation.TrangThaiHienThi)
                .Select(cau => (int?)cau.MaBH).SingleOrDefaultAsync();
            if (maBH == null)
            {
                return NotFound(new { message = "This sentence is no longer available. Reload the lesson." });
            }

            await using IDbContextTransaction giaoDich = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            // Khóa dòng tài khoản để hai lượt nộp không ghi trùng lần làm hoặc cộng điểm hai lần.
            List<TaiKhoan> dsTaiKhoan = await _context.TaiKhoans.FromSql(
                $@"SELECT * FROM `TaiKhoan` WHERE `MaTK` = {maTK} AND `TrangThaiTK` = TRUE
                    AND `VaiTro` = 'Student' FOR UPDATE").ToListAsync();
            TaiKhoan? taiKhoan = dsTaiKhoan.SingleOrDefault();
            if (taiKhoan == null)
            {
                return Forbid();
            }

            // Giữ câu hỏi ổn định trong lúc giảng viên có thể thay cả bộ phụ đề.
            await _context.Database.SqlQuery<int>(
                $"SELECT `MaBH` AS `Value` FROM `BaiHoc` WHERE `MaBH` = {maBH.Value} LOCK IN SHARE MODE").ToListAsync();
            CauChep? cau = await _context.CauCheps.AsNoTracking()
                .SingleOrDefaultAsync(item => item.MaCau == maCau && item.MaBH == maBH.Value
                    && item.TrangThaiDuyet && item.MaBHNavigation.MaDMNavigation.TrangThaiHienThi);
            if (cau == null)
            {
                return NotFound(new { message = "This sentence is no longer available. Reload the lesson." });
            }
            if (cau.NoiDungChuan.Length > KhoAmThanhBaiHoc.GioiHanTranscript)
            {
                return UnprocessableEntity(new { message = "This transcript is too long to check." });
            }

            List<KetQuaTu> cacTu = ChamTranscript.SoSanh(cau.NoiDungChuan, noiDungNhap);
            int soTuSai = cacTu.Count(tu => tu.Status != "correct");
            bool chinhXac = cacTu.Count > 0 && soTuSai == 0;
            IQueryable<LamBai> dsLanLam = _context.LamBais.Where(item => item.MaTK == maTK && item.MaCau == maCau);
            int lanLam = (await dsLanLam.MaxAsync(item => (int?)item.Lan) ?? 0) + 1;
            bool daDung = await dsLanLam.AnyAsync(item => item.SoLanSai == 0);

            LamBai lanNay = new LamBai();
            lanNay.MaTK = maTK;
            lanNay.MaCau = maCau;
            lanNay.Lan = lanLam;
            lanNay.SoLanSai = Math.Max(chinhXac ? 0 : 1, soTuSai);
            lanNay.TuVietSai = string.Join(" ", cacTu.Where(tu => tu.Status != "correct" && tu.Actual.Length > 0)
                .Select(tu => tu.Actual));
            lanNay.TuDung = cau.NoiDungChuan;
            lanNay.NgayLam = DateTime.Now;
            _context.LamBais.Add(lanNay);

            int soCauDaXong = await TruyVanCauDaXong(maTK, maBH.Value).CountAsync();
            int soCau = await _context.CauCheps.CountAsync(item => item.MaBH == maBH.Value && item.TrangThaiDuyet);
            int diemCong = 0;
            if (chinhXac && !daDung)
            {
                soCauDaXong++;
                diemCong = 1;
                taiKhoan.DiemTichLuy += diemCong;
            }

            TienDoBaiHoc? tienDo = await _context.TienDoBaiHocs.FindAsync(maTK, maBH.Value);
            if (tienDo == null)
            {
                tienDo = new TienDoBaiHoc();
                tienDo.MaTK = maTK;
                tienDo.MaBH = maBH.Value;
                _context.TienDoBaiHocs.Add(tienDo);
            }
            tienDo.MucDoHoanThanh = TinhTienDo(soCauDaXong, soCau);
            tienDo.NgayCapNhat = lanNay.NgayLam;
            await _context.SaveChangesAsync();
            await giaoDich.CommitAsync();

            return Json(new
            {
                correct = chinhXac,
                sentenceCompleted = daDung || chinhXac,
                completedCount = soCauDaXong,
                progress = tienDo.MucDoHoanThanh,
                pointsAwarded = diemCong,
                words = cacTu
            });
        }

        private async Task NhapPhuDeNeuTrong(int id, string tenFile, List<CauChep> cacCau)
        {
            await using IDbContextTransaction giaoDich = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            // Kiểm tra lại sau khi lấy khóa, tránh nhập trùng khi mở bài cùng lúc.
            await _context.Database.SqlQuery<int>(
                $"SELECT `MaBH` AS `Value` FROM `BaiHoc` WHERE `MaBH` = {id} FOR UPDATE").ToListAsync();
            string? tenFileHienTai = await _context.BaiHocs
                .Where(bh => bh.MaBH == id && bh.MaDMNavigation.TrangThaiHienThi && !bh.CauCheps.Any())
                .Select(bh => bh.AudioUrl).SingleOrDefaultAsync();
            if (string.Equals(tenFileHienTai, tenFile, StringComparison.Ordinal))
            {
                foreach (CauChep cau in cacCau)
                {
                    cau.MaBH = id;
                }
                _context.CauCheps.AddRange(cacCau);
                await _context.SaveChangesAsync();
            }
            await giaoDich.CommitAsync();
        }

        private IQueryable<int> TruyVanCauDaXong(int maTK, int maBH)
        {
            return _context.LamBais.Where(lan => lan.MaTK == maTK && lan.SoLanSai == 0
                && lan.MaCauNavigation.MaBH == maBH && lan.MaCauNavigation.TrangThaiDuyet)
                .Select(lan => lan.MaCau).Distinct();
        }

        private static decimal TinhTienDo(int soCauDaXong, int soCau)
        {
            if (soCau == 0)
            {
                return 0m;
            }
            return Math.Round(100m * soCauDaXong / soCau, 2, MidpointRounding.AwayFromZero);
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
                TempData["DanhGiaError"] = "Choose a rating from 1 to 5 stars. Comments can be up to 500 characters.";
                return RedirectToAction(nameof(Lesson), new { id = maBH, thuTu });
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

            TempData["DanhGiaSuccess"] = "Your review has been saved.";
            return RedirectToAction(nameof(Lesson), new { id = maBH, thuTu });
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
