using System.Diagnostics;
using AYOKONA.Models;
using Microsoft.AspNetCore.Mvc;

namespace AYOKONA.Controllers
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
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // Redirect authenticated users to Users/Homepage
                return RedirectToAction("Homepage", "Users");
            }
            return View();
        }

        public IActionResult Register()
        {
            return View("~/Views/Registration/Register.cshtml");
        }

        public IActionResult StudentLogin()
        {
            return View("~/Views/Account/StudentLogin.cshtml");
        }

        public IActionResult AdminLogin()
        {
            return View("~/Views/Account/AdminLogin.cshtml");
        }

        public IActionResult ForgotPass()
        {
            return View("~/Views/Account/ForgotPass.cshtml");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
