using EnglishListeningSystem.Models;
using EnglishListeningSystem.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishListeningSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LuuTruYeuCauGiangVien _luuTruYeuCau;

        public AdminController(ApplicationDbContext context, LuuTruYeuCauGiangVien luuTruYeuCau)
        {
            _context = context;
            _luuTruYeuCau = luuTruYeuCau;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<YeuCauGiangVien> dsChoDuyet = new List<YeuCauGiangVien>();
            try
            {
                dsChoDuyet = _luuTruYeuCau.LayDanhSach()
                    .OrderByDescending(yeuCau => yeuCau.NgayGui)
                    .ToList();
            }
            catch (IOException)
            {
                TempData["ErrorMessage"] = "We couldn't load the instructor requests. Please try again.";
            }

            ViewBag.DsChoDuyet = dsChoDuyet;
            ViewBag.SoLuongChoDuyet = dsChoDuyet.Count;

            List<TaiKhoan> dsHocVien = await _context.TaiKhoans.AsNoTracking()
                .Where(t => t.VaiTro == "Student" || t.VaiTro == "student")
                .OrderBy(t => t.MaTK)
                .ToListAsync();

            ViewBag.TongHocVien = dsHocVien.Count;
            ViewBag.HocVienHoatDong = dsHocVien.Count(t => t.TrangThaiTK);
            ViewBag.HocVienBiKhoa = dsHocVien.Count(t => !t.TrangThaiTK);

            List<BaiHoc> dsBaiHoc = await _context.BaiHocs.AsNoTracking()
                .Include(b => b.MaDMNavigation)
                .Include(b => b.TienDoBaiHocs)
                .Include(b => b.CauCheps)
                    .ThenInclude(c => c.LamBais)
                .AsSplitQuery()
                .ToListAsync();

            List<ThongKeBaiHoc> dsThongKe = new List<ThongKeBaiHoc>();
            foreach (BaiHoc bh in dsBaiHoc)
            {
                // Mỗi bản ghi tiến độ là một học viên tham gia bài.
                int soHocVien = bh.TienDoBaiHocs.Count;
                long tongLuotLam = 0;
                foreach (CauChep cau in bh.CauCheps)
                {
                    tongLuotLam += cau.LamBais.Count;
                }

                ThongKeBaiHoc thongKe = new ThongKeBaiHoc(
                    bh.TenBH, bh.MaDMNavigation.TenDM, bh.CapDo, soHocVien, tongLuotLam);
                dsThongKe.Add(thongKe);
            }

            List<ThongKeBaiHoc> thongKeBaiHoc = dsThongKe
                .OrderByDescending(b => b.SoHocVien)
                .ThenByDescending(b => b.TongLuotLuyenNghe)
                .ThenBy(b => b.TenBaiHoc)
                .ToList();

            ViewBag.ThongKeBaiHoc = thongKeBaiHoc;
            ViewBag.TongBaiHoc = thongKeBaiHoc.Count;
            ViewBag.TongLuotThamGia = thongKeBaiHoc.Sum(b => (long)b.SoHocVien);

            return View(dsHocVien);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DuyetGiangVien(string id)
        {
            bool taiKhoanDaLuu = false;
            try
            {
                using FileStream khoa = await _luuTruYeuCau.KhoaGhiAsync();
                List<YeuCauGiangVien> dsYeuCau = _luuTruYeuCau.LayDanhSach();
                YeuCauGiangVien? yeuCau = dsYeuCau.Find(item => item.Id == id);
                if (yeuCau == null)
                {
                    TempData["ErrorMessage"] = "This pending instructor request could not be found.";
                    return RedirectToAction(nameof(Index));
                }

                string tenSoSanh = yeuCau.TenDangNhap.ToLowerInvariant();
                string emailSoSanh = yeuCau.Email.ToLowerInvariant();
                TaiKhoan? tkDaCo = await _context.TaiKhoans.AsNoTracking().FirstOrDefaultAsync(tk =>
                    tk.TenDangNhap.ToLower() == tenSoSanh || (tk.Email != null && tk.Email.ToLower() == emailSoSanh));
                if (tkDaCo == null)
                {
                    TaiKhoan tkMoi = new TaiKhoan();
                    tkMoi.HoTen = yeuCau.HoTen;
                    tkMoi.TenDangNhap = yeuCau.TenDangNhap;
                    tkMoi.Email = yeuCau.Email;
                    tkMoi.MatKhau = yeuCau.MatKhauDaBam;
                    tkMoi.VaiTro = "Instructor";
                    tkMoi.TrangThaiTK = true;
                    tkMoi.DiemTichLuy = 0;
                    _context.TaiKhoans.Add(tkMoi);
                    await _context.SaveChangesAsync();
                }
                else if (tkDaCo.VaiTro != "Instructor" || !tkDaCo.TrangThaiTK
                    || tkDaCo.TenDangNhap != yeuCau.TenDangNhap || tkDaCo.Email != yeuCau.Email
                    || tkDaCo.MatKhau != yeuCau.MatKhauDaBam)
                {
                    TempData["ErrorMessage"] = "Another account already uses this username or email address.";
                    return RedirectToAction(nameof(Index));
                }

                // Nếu lần duyệt trước đã lưu tài khoản, chỉ cần dọn yêu cầu còn sót.
                taiKhoanDaLuu = true;
                _luuTruYeuCau.XoaYeuCau(id);
                TempData["SuccessMessage"] = "The instructor account has been approved and saved.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "We couldn't save the instructor account. The request is still pending approval.";
            }
            catch (IOException)
            {
                if (taiKhoanDaLuu)
                {
                    TempData["ErrorMessage"] = "The account was saved, but the request is still pending. Please try approving it again.";
                }
                else
                {
                    TempData["ErrorMessage"] = "We couldn't load this instructor request. Please try again.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyYeuCauGiangVien(string id)
        {
            try
            {
                using FileStream khoa = await _luuTruYeuCau.KhoaGhiAsync();
                bool daXoa = _luuTruYeuCau.XoaYeuCau(id);
                if (daXoa)
                {
                    TempData["SuccessMessage"] = "The instructor registration request has been rejected.";
                }
                else
                {
                    TempData["ErrorMessage"] = "This pending instructor request could not be found.";
                }
            }
            catch (IOException)
            {
                TempData["ErrorMessage"] = "We couldn't reject this instructor request. Please try again.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> KhoaTaiKhoan(int id)
        {
            return CapNhatTrangThai(id, false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> MoKhoaTaiKhoan(int id)
        {
            return CapNhatTrangThai(id, true);
        }

        private async Task<IActionResult> CapNhatTrangThai(int id, bool hoatDong)
        {
            // Chỉ cho phép khóa hoặc mở khóa tài khoản học viên.
            TaiKhoan? taiKhoan = await _context.TaiKhoans.FindAsync(id);
            if (taiKhoan == null || !string.Equals(taiKhoan.VaiTro, "Student", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            taiKhoan.TrangThaiTK = hoatDong;
            await _context.SaveChangesAsync();

            if (hoatDong)
            {
                TempData["SuccessMessage"] = "The account has been unlocked.";
            }
            else
            {
                TempData["SuccessMessage"] = "The account has been locked.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
