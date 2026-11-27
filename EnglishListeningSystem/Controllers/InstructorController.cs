using System.Data;
using System.Data.Common;
using System.Security.Claims;
using EnglishListeningSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EnglishListeningSystem.Controllers
{
    [Authorize(Roles = "Instructor")]
    public class InstructorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly KhoAmThanhBaiHoc _khoAmThanh;

        public InstructorController(ApplicationDbContext context, KhoAmThanhBaiHoc khoAmThanh)
        {
            _context = context;
            _khoAmThanh = khoAmThanh;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? maDM, string? search)
        {
            string tuKhoa = (search ?? string.Empty).Trim();
            IQueryable<BaiHoc> truyVanBaiHoc = _context.BaiHocs.AsNoTracking();
            if (maDM.HasValue)
            {
                truyVanBaiHoc = truyVanBaiHoc.Where(bh => bh.MaDM == maDM.Value);
            }

            if (tuKhoa.Length > 0)
            {
                truyVanBaiHoc = truyVanBaiHoc.Where(bh => bh.TenBH.Contains(tuKhoa));
            }

            List<BaiHoc> dsBaiHoc = await truyVanBaiHoc
                .Include(bh => bh.MaDMNavigation)
                .Include(bh => bh.CauCheps)
                .OrderByDescending(bh => bh.MaBH)
                .ToListAsync();
            List<DanhMuc> dsDanhMuc = await _context.DanhMucs.AsNoTracking()
                .OrderBy(dm => dm.TenDM)
                .ThenBy(dm => dm.MaDM)
                .ToListAsync();

            // Thẻ thống kê đếm toàn bộ dữ liệu, còn bộ lọc chỉ áp dụng cho bảng.
            ViewBag.TongBaiHoc = await _context.BaiHocs.CountAsync();
            ViewBag.TongCauHoi = await _context.CauCheps.CountAsync();
            ViewBag.DanhMucs = dsDanhMuc;
            ViewBag.MaDM = maDM;
            ViewBag.Search = tuKhoa;
            return View(dsBaiHoc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(KhoAmThanhBaiHoc.GioiHanYeuCau)]
        public async Task<IActionResult> ThemBaiHoc([Bind("TenBH,MaDM,CapDo,AudioUrl")] BaiHoc model,
            IFormFile? audioFile, IFormFile? srtFile)
        {
            string? loi = await KiemTraBaiHoc(model);
            if (loi != null)
            {
                TempData["ErrorMessage"] = loi;
                return RedirectToAction(nameof(Index));
            }

            BaiHoc baiHoc = new BaiHoc();
            baiHoc.TenBH = model.TenBH.Trim();
            baiHoc.MaDM = model.MaDM;
            baiHoc.CapDo = model.CapDo.Trim();
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int maTK) && maTK > 0)
            {
                baiHoc.MaTK = maTK;
            }

            string? fileDaTai = null;
            bool dangCommit = false;
            try
            {
                string? tenFile = KiemTraTenTep(model.AudioUrl, audioFile);
                if (audioFile != null || srtFile != null)
                {
                    (fileDaTai, List<CauChep>? cacCau) = await _khoAmThanh.LuuTepAsync(
                        audioFile, srtFile, tenFile, HttpContext.RequestAborted);
                    tenFile = fileDaTai;
                    if (cacCau != null)
                    {
                        baiHoc.CauCheps = cacCau;
                    }
                }

                baiHoc.AudioUrl = tenFile;
                await using IDbContextTransaction giaoDich = await _context.Database.BeginTransactionAsync();
                _context.BaiHocs.Add(baiHoc);
                await _context.SaveChangesAsync();
                dangCommit = true;
                await giaoDich.CommitAsync();
                TempData["SuccessMessage"] = "The lesson has been added.";
            }
            catch (InvalidDataException loiTep)
            {
                TempData["ErrorMessage"] = loiTep.Message;
            }
            catch (Exception loiLuu) when (loiLuu is IOException || loiLuu is UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "We couldn't save the lesson files. Please try again.";
            }
            catch (Exception loiLuu) when (loiLuu is DbUpdateException || loiLuu is DbException)
            {
                TempData["ErrorMessage"] = "We couldn't add the lesson. Check the selected topic and try again.";
            }
            finally
            {
                if (!dangCommit && fileDaTai != null)
                {
                    _khoAmThanh.XoaTepDaTaiLen(fileDaTai);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(KhoAmThanhBaiHoc.GioiHanYeuCau)]
        public async Task<IActionResult> SuaBaiHoc([Bind("MaBH,TenBH,MaDM,CapDo,AudioUrl")] BaiHoc model,
            IFormFile? audioFile, IFormFile? srtFile)
        {
            string? loi = await KiemTraBaiHoc(model);
            if (loi != null)
            {
                TempData["ErrorMessage"] = loi;
                return RedirectToAction(nameof(Index));
            }

            BaiHoc? baiHocHienTai = await _context.BaiHocs.AsNoTracking()
                .SingleOrDefaultAsync(bh => bh.MaBH == model.MaBH);
            if (baiHocHienTai == null)
            {
                TempData["ErrorMessage"] = "The lesson you tried to edit could not be found.";
                return RedirectToAction(nameof(Index));
            }

            string? fileDaTai = null;
            bool dangCommit = false;
            try
            {
                string? tenFile = KiemTraTenTep(model.AudioUrl, audioFile);
                bool dungAudioHienTai = audioFile == null && srtFile != null && tenFile == null;
                List<CauChep>? cacCau = null;
                if (audioFile != null || srtFile != null)
                {
                    (fileDaTai, cacCau) = await _khoAmThanh.LuuTepAsync(audioFile, srtFile,
                        tenFile ?? baiHocHienTai.AudioUrl, HttpContext.RequestAborted);
                    tenFile = fileDaTai;
                }

                await using IDbContextTransaction giaoDich =
                    await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
                List<BaiHoc> dsBai = await _context.BaiHocs.FromSql(
                    $"SELECT * FROM `BaiHoc` WHERE `MaBH` = {model.MaBH} FOR UPDATE").ToListAsync();
                BaiHoc? baiHoc = dsBai.SingleOrDefault();
                if (baiHoc == null || (dungAudioHienTai && baiHoc.AudioUrl != baiHocHienTai.AudioUrl))
                {
                    TempData["ErrorMessage"] = "The lesson or its audio has changed. Reload the page and try again.";
                    return RedirectToAction(nameof(Index));
                }

                if (cacCau != null)
                {
                    // Không thay phụ đề sau khi học viên đã làm bài.
                    IQueryable<CauChep> cauHoiCu = _context.CauCheps.Where(cau => cau.MaBH == baiHoc.MaBH);
                    bool coLuotLam = await cauHoiCu.AnyAsync(cau => cau.LamBais.Any());
                    bool coTienDo = await _context.TienDoBaiHocs.AnyAsync(tienDo =>
                        tienDo.MaBH == baiHoc.MaBH && tienDo.MucDoHoanThanh > 0);
                    if (coLuotLam || coTienDo)
                    {
                        TempData["ErrorMessage"] = "The subtitles cannot be replaced after students have attempted this lesson.";
                        return RedirectToAction(nameof(Index));
                    }

                    await cauHoiCu.ExecuteDeleteAsync();
                    foreach (CauChep cau in cacCau)
                    {
                        cau.MaBH = baiHoc.MaBH;
                    }
                    _context.CauCheps.AddRange(cacCau);
                }

                baiHoc.TenBH = model.TenBH.Trim();
                baiHoc.MaDM = model.MaDM;
                baiHoc.CapDo = model.CapDo.Trim();
                if (tenFile != null)
                {
                    baiHoc.AudioUrl = tenFile;
                }
                await _context.SaveChangesAsync();
                dangCommit = true;
                await giaoDich.CommitAsync();
                TempData["SuccessMessage"] = "The lesson has been updated.";
            }
            catch (InvalidDataException loiTep)
            {
                TempData["ErrorMessage"] = loiTep.Message;
            }
            catch (Exception loiLuu) when (loiLuu is IOException || loiLuu is UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "We couldn't save the lesson files. Please try again.";
            }
            catch (Exception loiLuu) when (loiLuu is DbUpdateException || loiLuu is DbException)
            {
                TempData["ErrorMessage"] = "We couldn't save the lesson. The lesson or topic may have changed.";
            }
            finally
            {
                if (!dangCommit && fileDaTai != null)
                {
                    _khoAmThanh.XoaTepDaTaiLen(fileDaTai);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaBaiHoc(int id)
        {
            IQueryable<BaiHoc> baiHocTheoMa = _context.BaiHocs.Where(bh => bh.MaBH == id);

            // Kiểm tra và xóa trong cùng một lệnh để tránh xóa dây chuyền dữ liệu học viên.
            IQueryable<BaiHoc> baiHocChuaCoDuLieu = baiHocTheoMa.Where(bh =>
                !bh.CauCheps.Any() && !bh.DanhGias.Any() && !bh.GhiChus.Any()
                && !bh.TienDoBaiHocs.Any() && !bh.TuVungs.Any());

            try
            {
                int soBaiDaXoa = await baiHocChuaCoDuLieu.ExecuteDeleteAsync();
                if (soBaiDaXoa > 0)
                {
                    TempData["SuccessMessage"] = "The lesson has been deleted.";
                }
                else if (await baiHocTheoMa.AnyAsync())
                {
                    TempData["ErrorMessage"] = "You can only delete lessons with no questions or student activity.";
                }
                else
                {
                    TempData["ErrorMessage"] = "The lesson you tried to delete could not be found.";
                }
            }
            catch (DbException)
            {
                TempData["ErrorMessage"] = "We couldn't delete the lesson. Check for related records and try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> DanhSachCauHoi(int maBH)
        {
            BaiHoc? baiHoc = await _context.BaiHocs.AsNoTracking()
                .FirstOrDefaultAsync(bh => bh.MaBH == maBH);
            if (baiHoc == null)
            {
                return NotFound();
            }

            List<CauChep> dsCauHoi = await _context.CauCheps.AsNoTracking()
                .Where(cau => cau.MaBH == maBH)
                .OrderBy(cau => cau.ThuTuCau)
                .ThenBy(cau => cau.MaCau)
                .ToListAsync();

            ViewBag.BaiHocCauHoi = baiHoc;
            ViewBag.DanhSachCauHoi = dsCauHoi;
            return View(nameof(Index), new BaiHoc[] { baiHoc });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaCauHoi([Bind("MaCau,MaBH,ThuTuCau,NoiDungChuan,ThoiGianBatDau,ThoiGianKetThuc,GoiY")] CauChep model)
        {
            CauChep? cauHoi = await _context.CauCheps.FindAsync(model.MaCau);
            if (cauHoi == null || cauHoi.MaBH != model.MaBH)
            {
                return NotFound();
            }

            bool thoiGianHopLe = double.IsFinite(model.ThoiGianBatDau) && double.IsFinite(model.ThoiGianKetThuc)
                && model.ThoiGianBatDau >= 0 && model.ThoiGianKetThuc > model.ThoiGianBatDau;
            bool goiYQuaDai = model.GoiY != null && model.GoiY.Trim().Length > 255;
            if (!ModelState.IsValid || model.ThuTuCau < 1 || string.IsNullOrWhiteSpace(model.NoiDungChuan)
                || !thoiGianHopLe || goiYQuaDai)
            {
                TempData["ErrorMessage"] = "Check the question's order, answer, hint, and time range.";
                return RedirectToAction(nameof(DanhSachCauHoi), new { maBH = model.MaBH });
            }

            cauHoi.ThuTuCau = model.ThuTuCau;
            cauHoi.NoiDungChuan = model.NoiDungChuan.Trim();
            cauHoi.ThoiGianBatDau = model.ThoiGianBatDau;
            cauHoi.ThoiGianKetThuc = model.ThoiGianKetThuc;
            cauHoi.GoiY = model.GoiY?.Trim();
            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "The question has been updated.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "We couldn't save the question. It may have changed.";
            }

            return RedirectToAction(nameof(DanhSachCauHoi), new { maBH = model.MaBH });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaCauHoi(int id, int maBH)
        {
            IQueryable<CauChep> cauHoiTheoMa = _context.CauCheps.Where(cau => cau.MaCau == id && cau.MaBH == maBH);
            try
            {
                int soCauDaXoa = await cauHoiTheoMa.Where(cau => !cau.LamBais.Any()).ExecuteDeleteAsync();
                if (soCauDaXoa > 0)
                {
                    TempData["SuccessMessage"] = "The question has been deleted.";
                }
                else if (await cauHoiTheoMa.AnyAsync())
                {
                    TempData["ErrorMessage"] = "You can only delete questions that students haven't attempted.";
                }
                else
                {
                    return NotFound();
                }
            }
            catch (DbException)
            {
                TempData["ErrorMessage"] = "We couldn't delete the question. Check for related records and try again.";
            }

            return RedirectToAction(nameof(DanhSachCauHoi), new { maBH });
        }

        private async Task<string?> KiemTraBaiHoc(BaiHoc model)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.TenBH) || model.TenBH.Trim().Length > 200
                || string.IsNullOrWhiteSpace(model.CapDo) || model.CapDo.Trim().Length > 50)
            {
                return "Enter a lesson title of up to 200 characters and a level of up to 50 characters.";
            }

            if (model.MaDM < 1 || !await _context.DanhMucs.AnyAsync(dm => dm.MaDM == model.MaDM))
            {
                return "Select an existing topic.";
            }

            return null;
        }

        private string? KiemTraTenTep(string? tenFile, IFormFile? audioFile)
        {
            if (audioFile != null || string.IsNullOrWhiteSpace(tenFile))
            {
                return null;
            }

            tenFile = tenFile.Trim();
            if (_khoAmThanh.TimFileMp3(tenFile) == null)
            {
                throw new InvalidDataException("Enter the name of an MP3 file already in the audio folder.");
            }
            return tenFile;
        }
    }
}
