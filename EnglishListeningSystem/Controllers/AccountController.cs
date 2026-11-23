using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using EnglishListeningSystem.Models;
using EnglishListeningSystem.Models.ViewModel;
using Microsoft.EntityFrameworkCore;
namespace EnglishListeningSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<TaiKhoan> _passwordHasher;
        public AccountController(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<TaiKhoan>();
        }

        public async Task<IActionResult> login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["LoginError"] = "Vui lòng nhập đầy đủ thông tin đăng nhập";
                return RedirectToAction("Index", "Home");
            }
            var user = await _context.TaiKhoans.FirstOrDefaultAsync(u => u.Email == model.EmailOrUsername || u.TenDangNhap == model.EmailOrUsername);
            if (user == null)
            {
                TempData["LoginError"] = "Tài khoản hoặc mật khẩu không chính xác";
                TempData["LastUsername"] = model.EmailOrUsername;
                return RedirectToAction("Index", "Home");
            }
            if (!user.TrangThaiTK)
            {
                TempData["LoginError"] = "Tài khoản của bạn đã bị khóa";
                return RedirectToAction("Index", "Home");
            }
            bool isPasswordValid= _passwordHasher.VerifyHashedPassword(user, user.MatKhau, model.Password) != PasswordVerificationResult.Failed;
            if (!isPasswordValid)
            {
                TempData["LoginError"] = "Tài khoản hoặc mật khẩu không chính xác";
                TempData["LastUsername"] = model.EmailOrUsername;
                return RedirectToAction("Index", "Home");
            }
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier,user.MaTK.ToString()),
                new Claim(ClaimTypes.Name,user.HoTen),
                new Claim(ClaimTypes.Email,user.Email),
                new Claim(ClaimTypes.Role,user.VaiTro)
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
            };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
            if(user.VaiTro== "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }
            else if(user.VaiTro== "Instructor")
            {
                return RedirectToAction("Index", "Instructor");
            }
            else
            {
                return RedirectToAction("Index", "Student");
            }
        }
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            TempData["RegHoten"] = model.HoTen;
            TempData["RegUsername"] = model.Username;
            TempData["RegEmail"] = model.Email;
            if (!ModelState.IsValid)
            {
                TempData["RegisterError"] = "Vui lòng nhập đầy đủ thông tin";
                return RedirectToAction("Index", "Home");
            }
            if (model.Password != model.ConfirmPassword)
            {
                TempData["RegisterError"] = "Mật khẩu xác nhận không khớp";
                return RedirectToAction("Index", "Home");
            }
            var checkUser = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.TenDangNhap == model.Username || x.Email == model.Email);
            if (checkUser != null)
            {
                TempData["RegisterError"] = "Tên đăng nhập hoặc Email này đã tồn tại";
                return RedirectToAction("Index", "Home");
            }
            TaiKhoan tk = new TaiKhoan();
            tk.HoTen = model.HoTen;
            tk.TenDangNhap = model.Username;
            tk.Email = model.Email;
            tk.MatKhau = model.Password;
            tk.VaiTro = "Student";
            tk.DiemTichLuy = 0;
            tk.TrangThaiTK = true;

            PasswordHasher<TaiKhoan> hasher = new PasswordHasher<TaiKhoan>();
            tk.MatKhau = hasher.HashPassword(tk, model.Password);

            _context.TaiKhoans.Add(tk);
            await _context.SaveChangesAsync();

            TempData.Remove("RegHoten");
            TempData.Remove("RegUsername");
            TempData.Remove("RegEmail");

            TempData["RegisterSuccess"] = "Đăng ký thành công!";
            TempData["LastUsername"] = tk.TenDangNhap;
            return RedirectToAction("Index", "Home");
        }
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
