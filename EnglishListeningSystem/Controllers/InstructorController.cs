using System.Data.Common;
using System.Security.Claims;
using EnglishListeningSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishListeningSystem.Controllers
{
    [Authorize(Roles = "Instructor")]
    public class InstructorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InstructorController(ApplicationDbContext context)
        {
            _context = context;
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
        public async Task<IActionResult> ThemBaiHoc([Bind("TenBH,MaDM,CapDo,AudioUrl")] BaiHoc model)
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
            baiHoc.AudioUrl = model.AudioUrl?.Trim();
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int maTK) && maTK > 0)
            {
                baiHoc.MaTK = maTK;
            }

            _context.BaiHocs.Add(baiHoc);
            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã thêm bài học.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Không thể thêm bài học. Hãy kiểm tra lại chủ đề đã chọn.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaBaiHoc([Bind("MaBH,TenBH,MaDM,CapDo,AudioUrl")] BaiHoc model)
        {
            string? loi = await KiemTraBaiHoc(model);
            if (loi != null)
            {
                TempData["ErrorMessage"] = loi;
                return RedirectToAction(nameof(Index));
            }

            BaiHoc? baiHoc = await _context.BaiHocs.FindAsync(model.MaBH);
            if (baiHoc == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài học cần sửa.";
                return RedirectToAction(nameof(Index));
            }

            baiHoc.TenBH = model.TenBH.Trim();
            baiHoc.MaDM = model.MaDM;
            baiHoc.CapDo = model.CapDo.Trim();
            baiHoc.AudioUrl = model.AudioUrl?.Trim();
            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã cập nhật bài học.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Không thể lưu bài học. Bài học hoặc chủ đề có thể đã thay đổi.";
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
                    TempData["SuccessMessage"] = "Đã xóa bài học.";
                }
                else if (await baiHocTheoMa.AnyAsync())
                {
                    TempData["ErrorMessage"] = "Bài học còn câu hỏi hoặc dữ liệu học viên nên chưa thể xóa.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy bài học cần xóa.";
                }
            }
            catch (DbException)
            {
                TempData["ErrorMessage"] = "Không thể xóa bài học. Hãy kiểm tra dữ liệu liên quan.";
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
                TempData["ErrorMessage"] = "Kiểm tra lại thứ tự, nội dung, gợi ý và khoảng thời gian của câu hỏi.";
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
                TempData["SuccessMessage"] = "Đã cập nhật câu hỏi.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Không thể lưu câu hỏi. Câu hỏi có thể đã thay đổi.";
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
                    TempData["SuccessMessage"] = "Đã xóa câu hỏi.";
                }
                else if (await cauHoiTheoMa.AnyAsync())
                {
                    TempData["ErrorMessage"] = "Câu hỏi đã có lượt làm nên chưa thể xóa.";
                }
                else
                {
                    return NotFound();
                }
            }
            catch (DbException)
            {
                TempData["ErrorMessage"] = "Không thể xóa câu hỏi. Hãy kiểm tra dữ liệu liên quan.";
            }

            return RedirectToAction(nameof(DanhSachCauHoi), new { maBH });
        }

        private async Task<string?> KiemTraBaiHoc(BaiHoc model)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.TenBH) || model.TenBH.Trim().Length > 200
                || string.IsNullOrWhiteSpace(model.CapDo) || model.CapDo.Trim().Length > 50)
            {
                return "Nhập tên bài học tối đa 200 ký tự và cấp độ tối đa 50 ký tự.";
            }

            if (!string.IsNullOrWhiteSpace(model.AudioUrl) && model.AudioUrl.Trim().Length > 255)
            {
                return "Đường dẫn audio tối đa 255 ký tự.";
            }

            if (model.MaDM < 1 || !await _context.DanhMucs.AnyAsync(dm => dm.MaDM == model.MaDM))
            {
                return "Hãy chọn một chủ đề có trong hệ thống.";
            }

            return null;
        }
    }
}
