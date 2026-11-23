using EnglishListeningSystem.Models;
using EnglishListeningSystem.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishListeningSystem.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(ApplicationDbContext context) : Controller
{
    private readonly ApplicationDbContext _context = context;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var dsHocVien = await _context.TaiKhoans.AsNoTracking()
            .Where(t => t.VaiTro == "Student" || t.VaiTro == "student")
            .OrderBy(t => t.MaTK)
            .ToListAsync();

        ViewBag.TongHocVien = dsHocVien.Count;
        ViewBag.HocVienHoatDong = dsHocVien.Count(t => t.TrangThaiTK);
        ViewBag.HocVienBiKhoa = dsHocVien.Count(t => !t.TrangThaiTK);

        var baiHocs = await _context.BaiHocs.AsNoTracking()
            .Include(b => b.MaDMNavigation)
            .Include(b => b.TienDoBaiHocs)
            .Include(b => b.CauCheps)
                .ThenInclude(c => c.LamBais)
            .AsSplitQuery()
            .ToListAsync();

        // Mỗi bản ghi tiến độ tương ứng với một tài khoản tham gia bài học.
        var thongKeBaiHoc = baiHocs
            .Select(b => new ThongKeBaiHoc(b.TenBH, b.MaDMNavigation.TenDM, b.CapDo,
                b.TienDoBaiHocs.Count, b.CauCheps.Sum(c => (long)c.LamBais.Count)))
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
    public Task<IActionResult> KhoaTaiKhoan(int id) => CapNhatTrangThai(id, false);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> MoKhoaTaiKhoan(int id) => CapNhatTrangThai(id, true);

    private async Task<IActionResult> CapNhatTrangThai(int id, bool hoatDong)
    {
        var taiKhoan = await _context.TaiKhoans.FindAsync(id);
        if (taiKhoan == null || !string.Equals(taiKhoan.VaiTro, "Student", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        taiKhoan.TrangThaiTK = hoatDong;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = hoatDong
            ? "Đã mở khóa tài khoản thành công!"
            : "Đã khóa tài khoản thành công!";

        return RedirectToAction(nameof(Index));
    }
}
