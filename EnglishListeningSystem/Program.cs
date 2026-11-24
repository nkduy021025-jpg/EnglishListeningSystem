using EnglishListeningSystem.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>();
builder.Services.AddSingleton<LuuTruYeuCauGiangVien>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/Home/Index";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.Events.OnValidatePrincipal = async context =>
    {
        var principal = context.Principal;
        if (principal == null || (!principal.IsInRole("Student") && !principal.IsInRole("student")))
        {
            return;
        }

        // Kiểm tra cả phiên đăng nhập hiện có khi quản trị viên khóa học viên.
        var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var dangHoatDong = int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var maTK)
            && await db.TaiKhoans.AnyAsync(t => t.MaTK == maTK && t.TrangThaiTK
                && (t.VaiTro == "Student" || t.VaiTro == "student"), context.HttpContext.RequestAborted);

        if (!dangHoatDong)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(context.Scheme.Name);
        }
    };
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
