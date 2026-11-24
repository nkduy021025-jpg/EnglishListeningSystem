using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using EnglishListeningSystem.Models;
using EnglishListeningSystem.Models.ViewModel;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
namespace EnglishListeningSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LuuTruYeuCauGiangVien _luuTruYeuCau;
        private readonly PasswordHasher<TaiKhoan> _passwordHasher;
        public AccountController(ApplicationDbContext context, LuuTruYeuCauGiangVien luuTruYeuCau)
        {
            _context = context;
            _luuTruYeuCau = luuTruYeuCau;
            _passwordHasher = new PasswordHasher<TaiKhoan>();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["LoginError"] = "Vui lòng nhập đầy đủ thông tin đăng nhập";
                return RedirectToAction("Index", "Home");
            }
            // Có thể đăng nhập bằng email hoặc tên tài khoản.
            TaiKhoan? tk = await _context.TaiKhoans.FirstOrDefaultAsync(t =>
                t.Email == model.EmailOrUsername || t.TenDangNhap == model.EmailOrUsername);
            if (tk == null)
            {
                TempData["LastUsername"] = model.EmailOrUsername;
                try
                {
                    string tenHoacEmail = model.EmailOrUsername.Trim();
                    List<YeuCauGiangVien> dsChoDuyet = _luuTruYeuCau.LayDanhSach();
                    YeuCauGiangVien? yeuCau = dsChoDuyet.Find(item =>
                        LuuTruYeuCauGiangVien.TrungThongTin(item.TenDangNhap, tenHoacEmail)
                        || LuuTruYeuCauGiangVien.TrungThongTin(item.Email, tenHoacEmail));
                    if (yeuCau != null)
                    {
                        TempData["LoginError"] = "Tài khoản giảng viên của bạn đang chờ quản trị viên phê duyệt.";
                    }
                    else
                    {
                        TempData["LoginError"] = "Tài khoản hoặc mật khẩu không chính xác";
                    }
                }
                catch (IOException)
                {
                    TempData["LoginError"] = "Chưa thể kiểm tra trạng thái đăng ký. Vui lòng thử lại.";
                }

                return RedirectToAction("Index", "Home");
            }
            if (tk.TrangThaiTK == false)
            {
                if (tk.VaiTro == "Instructor")
                {
                    TempData["LoginError"] = "Tài khoản giảng viên của bạn đang chờ quản trị viên phê duyệt.";
                }
                else
                {
                    TempData["LoginError"] = "Tài khoản của bạn đã bị khóa.";
                }

                TempData["LastUsername"] = model.EmailOrUsername;
                return RedirectToAction("Index", "Home");
            }
            // Đối chiếu mật khẩu vừa nhập với bản băm đã lưu.
            PasswordVerificationResult ketQua = _passwordHasher.VerifyHashedPassword(tk, tk.MatKhau, model.Password);
            if (ketQua == PasswordVerificationResult.Failed)
            {
                TempData["LoginError"] = "Tài khoản hoặc mật khẩu không chính xác";
                TempData["LastUsername"] = model.EmailOrUsername;
                return RedirectToAction("Index", "Home");
            }
            List<Claim> dsClaim = new List<Claim>();
            dsClaim.Add(new Claim(ClaimTypes.NameIdentifier, tk.MaTK.ToString()));
            dsClaim.Add(new Claim(ClaimTypes.Name, tk.HoTen));
            dsClaim.Add(new Claim(ClaimTypes.Email, tk.Email));
            dsClaim.Add(new Claim(ClaimTypes.Role, tk.VaiTro));

            ClaimsIdentity danhTinh = new ClaimsIdentity(dsClaim, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal nguoiDung = new ClaimsPrincipal(danhTinh);
            AuthenticationProperties thuocTinhCookie = new AuthenticationProperties();
            thuocTinhCookie.IsPersistent = model.RememberMe;
            if (model.RememberMe)
            {
                thuocTinhCookie.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7);
            }

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, nguoiDung, thuocTinhCookie);

            // Sau khi lưu cookie, đưa người dùng về đúng khu vực của vai trò.
            if (tk.VaiTro == "Student")
            {
                return RedirectToAction("Index", "Student");
            }
            else if (tk.VaiTro == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (tk.VaiTro == "Instructor")
            {
                return RedirectToAction("Index", "Instructor");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            TempData["RegHoTen"] = model.HoTen;
            TempData["RegUsername"] = model.Username;
            TempData["RegEmail"] = model.Email;
            TempData["RegVaiTro"] = model.VaiTro == "Instructor" ? "Instructor" : "Student";

            if (!string.IsNullOrEmpty(model.Password) && !string.IsNullOrEmpty(model.ConfirmPassword)
                && model.Password != model.ConfirmPassword)
            {
                TempData["RegisterError"] = "Mật khẩu xác nhận không khớp";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                TempData["RegisterError"] = "Vui lòng kiểm tra lại thông tin đăng ký";
                return RedirectToAction("Index", "Home");
            }

            // Form đăng ký chỉ nhận hai vai trò này, kể cả khi gửi POST trực tiếp.
            if (model.VaiTro != "Student" && model.VaiTro != "Instructor")
            {
                TempData["RegisterError"] = "Vai trò đăng ký không hợp lệ";
                return RedirectToAction("Index", "Home");
            }

            // Giữ chỗ cho tên và email trong suốt lúc kiểm tra và lưu đăng ký.
            try
            {
                using FileStream khoa = await _luuTruYeuCau.KhoaGhiAsync();
                return await LuuDangKyAsync(model);
            }
            catch (IOException)
            {
                TempData["RegisterError"] = "Chưa thể xử lý yêu cầu đăng ký. Vui lòng thử lại.";
                return RedirectToAction("Index", "Home");
            }
        }

        private async Task<IActionResult> LuuDangKyAsync(RegisterViewModel model)
        {
            string tenDangNhap = model.Username;
            string email = model.Email;
            string? loiTrung = await KiemTraTrungTaiKhoan(tenDangNhap, email);
            if (loiTrung != null)
            {
                TempData["RegisterError"] = loiTrung;
                return RedirectToAction("Index", "Home");
            }

            List<YeuCauGiangVien> dsChoDuyet = _luuTruYeuCau.LayDanhSach();
            if (dsChoDuyet.Exists(item => LuuTruYeuCauGiangVien.TrungThongTin(item.TenDangNhap, tenDangNhap)))
            {
                TempData["RegisterError"] = "Tên đăng nhập này đã có trong yêu cầu giảng viên đang chờ phê duyệt!";
                return RedirectToAction("Index", "Home");
            }

            if (dsChoDuyet.Exists(item => LuuTruYeuCauGiangVien.TrungThongTin(item.Email, email)))
            {
                TempData["RegisterError"] = "Email này đã có trong yêu cầu giảng viên đang chờ phê duyệt!";
                return RedirectToAction("Index", "Home");
            }

            TaiKhoan tk = new TaiKhoan();
            tk.HoTen = model.HoTen;
            tk.TenDangNhap = tenDangNhap;
            tk.Email = email;
            tk.VaiTro = model.VaiTro;
            tk.DiemTichLuy = 0;
            tk.MatKhau = _passwordHasher.HashPassword(tk, model.Password);

            if (tk.VaiTro == "Student")
            {
                tk.TrangThaiTK = true;

                // Lưu lộ trình cùng tài khoản để dùng ngay sau khi đăng ký.
                LoTrinh loTrinh = new LoTrinh();
                loTrinh.TenLT = "Lộ trình học mặc định";
                loTrinh.TrangThaiLT = "Đang học";
                loTrinh.CauHienTai = 1;
                tk.LoTrinhs.Add(loTrinh);

                _context.TaiKhoans.Add(tk);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException loi) when ((loi.InnerException as MySqlException)?.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
                {
                    // Kiểm tra lại nếu yêu cầu khác vừa lưu cùng tên đăng nhập hoặc email.
                    loiTrung = await KiemTraTrungTaiKhoan(tenDangNhap, email);
                    if (loiTrung == null)
                    {
                        throw;
                    }

                    TempData["RegisterError"] = loiTrung;
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                YeuCauGiangVien yeuCau = new YeuCauGiangVien();
                yeuCau.Id = Guid.NewGuid().ToString();
                yeuCau.HoTen = tk.HoTen;
                yeuCau.TenDangNhap = tk.TenDangNhap;
                yeuCau.Email = tk.Email;
                yeuCau.MatKhauDaBam = tk.MatKhau;
                yeuCau.NgayGui = DateTime.Now;
                _luuTruYeuCau.ThemYeuCau(yeuCau);
            }

            TempData.Remove("RegisterError");
            TempData.Remove("RegHoTen");
            TempData.Remove("RegUsername");
            TempData.Remove("RegEmail");
            TempData.Remove("RegVaiTro");

            if (tk.VaiTro == "Student")
            {
                TempData.Remove("InstructorPending");
                TempData["RegisterSuccess"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                TempData["LastUsername"] = tk.TenDangNhap;
            }
            else
            {
                TempData.Remove("RegisterSuccess");
                TempData.Remove("LastUsername");
                TempData["InstructorPending"] = "Vui lòng đợi, quản trị viên sẽ sớm phê duyệt yêu cầu của bạn.";
            }

            return RedirectToAction("Index", "Home");
        }

        private async Task<string?> KiemTraTrungTaiKhoan(string tenDangNhap, string email)
        {
            // Kiểm tra tên đăng nhập trước để báo đúng trường đang bị trùng.
            TaiKhoan? tkTrungTen = await _context.TaiKhoans.AsNoTracking()
                .FirstOrDefaultAsync(tk => tk.TenDangNhap == tenDangNhap);
            if (tkTrungTen != null)
            {
                return "Tên đăng nhập này đã được sử dụng!";
            }

            // Bỏ qua email rỗng để không so với dữ liệu cũ thiếu email.
            if (!string.IsNullOrWhiteSpace(email))
            {
                TaiKhoan? tkTrungEmail = await _context.TaiKhoans.AsNoTracking()
                    .FirstOrDefaultAsync(tk => tk.Email == email);
                if (tkTrungEmail != null)
                {
                    return "Email này đã được sử dụng!";
                }
            }

            return null;
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
