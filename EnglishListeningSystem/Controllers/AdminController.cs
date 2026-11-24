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
                TempData["ErrorMessage"] = "Chưa thể đọc danh sách yêu cầu giảng viên. Vui lòng thử lại.";
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
                    TempData["ErrorMessage"] = "Không tìm thấy yêu cầu giảng viên đang chờ phê duyệt.";
                    return RedirectToAction(nameof(Index));
                }

                TaiKhoan? tkDaCo = await _context.TaiKhoans.AsNoTracking().FirstOrDefaultAsync(tk =>
                    tk.TenDangNhap == yeuCau.TenDangNhap || tk.Email == yeuCau.Email);
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
                    TempData["ErrorMessage"] = "Tên đăng nhập hoặc email đã được một tài khoản khác sử dụng.";
                    return RedirectToAction(nameof(Index));
                }

                // Nếu lần duyệt trước đã lưu tài khoản, chỉ cần dọn yêu cầu còn sót.
                taiKhoanDaLuu = true;
                _luuTruYeuCau.XoaYeuCau(id);
                TempData["SuccessMessage"] = "Đã phê duyệt và lưu tài khoản giảng viên vào hệ thống!";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Chưa thể lưu tài khoản giảng viên. Yêu cầu vẫn đang chờ phê duyệt.";
            }
            catch (IOException)
            {
                if (taiKhoanDaLuu)
                {
                    TempData["ErrorMessage"] = "Đã lưu tài khoản nhưng chưa dọn được yêu cầu. Hãy thử duyệt lại.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Chưa thể đọc yêu cầu giảng viên. Vui lòng thử lại.";
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
                    TempData["SuccessMessage"] = "Đã hủy yêu cầu đăng ký tài khoản giảng viên!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy yêu cầu giảng viên đang chờ phê duyệt.";
                }
            }
            catch (IOException)
            {
                TempData["ErrorMessage"] = "Chưa thể hủy yêu cầu giảng viên. Vui lòng thử lại.";
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
                TempData["SuccessMessage"] = "Đã mở khóa tài khoản thành công!";
            }
            else
            {
                TempData["SuccessMessage"] = "Đã khóa tài khoản thành công!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
