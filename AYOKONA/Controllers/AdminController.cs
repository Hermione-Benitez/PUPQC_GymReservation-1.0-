using AYOKONA.Entities;
using AYOKONA.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AYOKONA.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult AdminLogin()
        {
            return View();
        }

        // In AdminController.cs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var hashedPassword = model.GetHashedPassword();

            var admin = _context.AdminAccounts
                .FirstOrDefault(a => a.Email == model.Email && a.PasswordHash == hashedPassword);

            if (admin != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, admin.Name),
                    new Claim(ClaimTypes.Email, admin.Email),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                TempData["SuccessMessage"] = "Admin login successful!";
                // This line ensures redirection to AdminDashboard
                return RedirectToAction("AdminDashboard");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult AdminDashboard()
        {
            return View("AdminDashboard");
        }

        public IActionResult AdminManage()
        {
            return View();
        }

        public IActionResult AdminProfile()
        {
            return View();
        }

        public IActionResult AdminEditProfile()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}