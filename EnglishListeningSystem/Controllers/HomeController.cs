using EnglishListeningSystem.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace EnglishListeningSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            string? maYeuCau = Activity.Current?.Id;
            if (maYeuCau == null)
            {
                maYeuCau = HttpContext.TraceIdentifier;
            }

            ErrorViewModel model = new ErrorViewModel();
            model.RequestId = maYeuCau;
            return View(model);
        }
    }
}
