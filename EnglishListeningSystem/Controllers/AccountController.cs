using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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
        private const string LoiEmailDaSuDung = "This email address is already in use. Each email address can be used for one account.";
        private const string LoiTenDangNhapDaTonTai = "This username is already taken. Please choose another one.";

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
                TempData["LoginError"] = "Enter your username or email and your password.";
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
                        TempData["LoginError"] = "Your instructor account is awaiting approval from an administrator.";
                    }
                    else
                    {
                        TempData["LoginError"] = "Your username, email, or password is incorrect.";
                    }
                }
                catch (IOException)
                {
                    TempData["LoginError"] = "We couldn't check your registration status. Please try again.";
                }

                return RedirectToAction("Index", "Home");
            }
            if (tk.TrangThaiTK == false)
            {
                if (tk.VaiTro == "Instructor")
                {
                    TempData["LoginError"] = "Your instructor account is awaiting approval from an administrator.";
                }
                else
                {
                    TempData["LoginError"] = "Your account is locked.";
                }

                TempData["LastUsername"] = model.EmailOrUsername;
                return RedirectToAction("Index", "Home");
            }
            // Đối chiếu mật khẩu vừa nhập với bản băm đã lưu.
            PasswordVerificationResult ketQua = _passwordHasher.VerifyHashedPassword(tk, tk.MatKhau, model.Password);
            if (ketQua == PasswordVerificationResult.Failed)
            {
                TempData["LoginError"] = "Your username, email, or password is incorrect.";
                TempData["LastUsername"] = model.EmailOrUsername;
                return RedirectToAction("Index", "Home");
            }
            ClaimsPrincipal nguoiDung = TaoNguoiDungDangNhap(tk);
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ThongTinCaNhan()
        {
            TaiKhoan? tk = await LayTaiKhoanHienTaiAsync();
            if (tk == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(tk);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatThongTin(string? hoTen, string? tenDangNhap)
        {
            if (string.IsNullOrWhiteSpace(hoTen))
            {
                TempData["InfoError"] = "Enter your full name.";
                return RedirectToAction(nameof(ThongTinCaNhan));
            }
            else if (string.IsNullOrWhiteSpace(tenDangNhap))
            {
                TempData["InfoError"] = "Enter your username.";
                return RedirectToAction(nameof(ThongTinCaNhan));
            }

            hoTen = hoTen.Trim();
            string tenDangNhapMoi = tenDangNhap.Trim();
            if (hoTen.Length > 100)
            {
                TempData["InfoError"] = "Your full name must be 100 characters or fewer.";
                return RedirectToAction(nameof(ThongTinCaNhan));
            }
            else if (tenDangNhapMoi.Length > 50)
            {
                TempData["InfoError"] = "Your username must be 50 characters or fewer.";
                return RedirectToAction(nameof(ThongTinCaNhan));
            }

            TaiKhoan? tk;
            try
            {
                // Dùng chung khóa với luồng đăng ký và phê duyệt khi kiểm tra và lưu tên mới.
                using FileStream khoa = await _luuTruYeuCau.KhoaGhiAsync();
                tk = await LayTaiKhoanHienTaiAsync();
                if (tk == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                List<YeuCauGiangVien> dsChoDuyet = _luuTruYeuCau.LayDanhSach();
                if (await TrungTenDangNhapAsync(tenDangNhapMoi, tk.MaTK)
                    || dsChoDuyet.Exists(item => LuuTruYeuCauGiangVien.TrungThongTin(item.TenDangNhap, tenDangNhapMoi)))
                {
                    TempData["InfoError"] = LoiTenDangNhapDaTonTai;
                    return RedirectToAction(nameof(ThongTinCaNhan));
                }

                tk.HoTen = hoTen;
                tk.TenDangNhap = tenDangNhapMoi;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException loi) when ((loi.InnerException as MySqlException)?.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
            {
                TempData["InfoError"] = LoiTenDangNhapDaTonTai;
                return RedirectToAction(nameof(ThongTinCaNhan));
            }
            catch (IOException)
            {
                TempData["InfoError"] = "We couldn't check whether this username is available. Please try again.";
                return RedirectToAction(nameof(ThongTinCaNhan));
            }

            // Giữ tùy chọn ghi nhớ đăng nhập và thời hạn của cookie hiện tại.
            AuthenticateResult phien = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal nguoiDung = TaoNguoiDungDangNhap(tk);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, nguoiDung, phien.Properties);

            TempData["InfoSuccess"] = "Your profile has been updated.";
            return RedirectToAction(nameof(ThongTinCaNhan));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiMatKhau(string? matKhauCu, string? matKhauMoi, string? xacNhanMatKhau)
        {
            TaiKhoan? tk = await LayTaiKhoanHienTaiAsync();
            if (tk == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(matKhauCu) || string.IsNullOrWhiteSpace(matKhauMoi)
                || string.IsNullOrWhiteSpace(xacNhanMatKhau))
            {
                TempData["PasswordError"] = "Complete all password fields.";
                return RedirectToAction(nameof(ThongTinCaNhan));
            }

            PasswordVerificationResult ketQuaKiemTra = _passwordHasher.VerifyHashedPassword(tk, tk.MatKhau, matKhauCu);
            if (ketQuaKiemTra == PasswordVerificationResult.Failed)
            {
                TempData["PasswordError"] = "Your current password is incorrect.";
            }
            else if (matKhauMoi.Length < 6)
            {
                TempData["PasswordError"] = "Your new password must be at least 6 characters long.";
            }
            else if (matKhauMoi != xacNhanMatKhau)
            {
                TempData["PasswordError"] = "Your new password and confirmation don't match.";
            }
            else
            {
                tk.MatKhau = _passwordHasher.HashPassword(tk, matKhauMoi);
                await _context.SaveChangesAsync();
                TempData["PasswordSuccess"] = "Your password has been changed.";
            }

            return RedirectToAction(nameof(ThongTinCaNhan));
        }

        private async Task<TaiKhoan?> LayTaiKhoanHienTaiAsync()
        {
            // Cookie lưu MaTK trong NameIdentifier, còn Name là họ tên hiển thị.
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int maTK))
            {
                return null;
            }

            return await _context.TaiKhoans.FirstOrDefaultAsync(t => t.MaTK == maTK);
        }

        private static ClaimsPrincipal TaoNguoiDungDangNhap(TaiKhoan tk)
        {
            List<Claim> dsClaim = new List<Claim>();
            dsClaim.Add(new Claim(ClaimTypes.NameIdentifier, tk.MaTK.ToString()));
            dsClaim.Add(new Claim(ClaimTypes.Name, tk.HoTen));
            dsClaim.Add(new Claim("TenDangNhap", tk.TenDangNhap));
            dsClaim.Add(new Claim(ClaimTypes.Role, tk.VaiTro));
            if (!string.IsNullOrWhiteSpace(tk.Email))
            {
                dsClaim.Add(new Claim(ClaimTypes.Email, tk.Email));
            }

            ClaimsIdentity danhTinh = new ClaimsIdentity(dsClaim, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(danhTinh);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            string emailNhap = model.Email.Trim();
            TempData["RegHoTen"] = model.HoTen;
            TempData["RegUsername"] = model.Username;
            TempData["RegEmail"] = emailNhap;
            TempData["RegVaiTro"] = model.VaiTro == "Instructor" ? "Instructor" : "Student";

            if (emailNhap.Length == 0)
            {
                TempData["RegisterError"] = "Enter your email address.";
                return RedirectToAction("Index", "Home");
            }

            if (!string.IsNullOrEmpty(model.Password) && !string.IsNullOrEmpty(model.ConfirmPassword)
                && model.Password != model.ConfirmPassword)
            {
                TempData["RegisterError"] = "Your passwords don't match.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                TempData["RegisterError"] = "Check your registration details and try again.";
                return RedirectToAction("Index", "Home");
            }

            // Form đăng ký chỉ nhận hai vai trò này, kể cả khi gửi POST trực tiếp.
            if (model.VaiTro != "Student" && model.VaiTro != "Instructor")
            {
                TempData["RegisterError"] = "Select a valid role.";
                return RedirectToAction("Index", "Home");
            }

            // Giữ chỗ cho tên và email trong suốt lúc kiểm tra và lưu đăng ký.
            try
            {
                using FileStream khoa = await _luuTruYeuCau.KhoaGhiAsync();
                return await LuuDangKyAsync(model, emailNhap);
            }
            catch (IOException)
            {
                TempData["RegisterError"] = "We couldn't process your registration. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        private async Task<IActionResult> LuuDangKyAsync(RegisterViewModel model, string emailNhap)
        {
            string tenDangNhap = model.Username;
            List<YeuCauGiangVien> dsChoDuyet = _luuTruYeuCau.LayDanhSach();
            string? loiTrung = await KiemTraTrungDangKyAsync(tenDangNhap, emailNhap, dsChoDuyet);
            if (loiTrung != null)
            {
                TempData["RegisterError"] = loiTrung;
                return RedirectToAction("Index", "Home");
            }

            TaiKhoan tk = new TaiKhoan();
            tk.HoTen = model.HoTen;
            tk.TenDangNhap = tenDangNhap;
            tk.Email = emailNhap;
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
                    loiTrung = await KiemTraTrungDangKyAsync(tenDangNhap, emailNhap, dsChoDuyet);
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
                yeuCau.Email = emailNhap;
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
                TempData["RegisterSuccess"] = "Your account is ready. Please log in.";
                TempData["LastUsername"] = tk.TenDangNhap;
            }
            else
            {
                TempData.Remove("RegisterSuccess");
                TempData.Remove("LastUsername");
                TempData["InstructorPending"] = "Please wait, an administrator will review and approve your request shortly.";
            }

            return RedirectToAction("Index", "Home");
        }

        private async Task<string?> KiemTraTrungDangKyAsync(string tenDangNhap, string emailNhap, List<YeuCauGiangVien> dsChoDuyet)
        {
            // Ưu tiên báo email trùng, kể cả khi tên đăng nhập cũng đã có người dùng.
            string emailSoSanh = emailNhap.ToLowerInvariant();
            bool trungEmail = await _context.TaiKhoans.AnyAsync(t => t.Email != null && t.Email.ToLower() == emailSoSanh);
            if (trungEmail || dsChoDuyet.Exists(item => LuuTruYeuCauGiangVien.TrungThongTin(item.Email, emailNhap)))
            {
                return LoiEmailDaSuDung;
            }
            else if (await TrungTenDangNhapAsync(tenDangNhap))
            {
                return "This username is already taken. Please choose another one.";
            }
            else if (dsChoDuyet.Exists(item => LuuTruYeuCauGiangVien.TrungThongTin(item.TenDangNhap, tenDangNhap)))
            {
                return "This username is already associated with a pending instructor request.";
            }

            return null;
        }

        private async Task<bool> TrungTenDangNhapAsync(string tenDangNhap, int? maTKBoQua = null)
        {
            string tenSoSanh = tenDangNhap.ToLowerInvariant();
            return await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap.ToLower() == tenSoSanh
                && (maTKBoQua == null || t.MaTK != maTKBoQua));
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
