using AYOKONA.Entities;
using AYOKONA.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AYOKONA.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult AdminRegister()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdminRegister(AdminRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if admin already exists
            if (_context.AdminAccounts.Any(a => a.Email == model.Email))
            {
                ModelState.AddModelError(string.Empty, "Email is already registered as an admin.");
                return View(model);
            }

            var admin = new AdminAccount
            {
                Name = model.Name, // You may want to add a Name field to your view/model for proper admin naming
                Email = model.Email,
                PasswordHash = model.GetHashedPassword() // Use the method to hash the password
            };

            _context.AdminAccounts.Add(admin);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Admin registration successful!";
            return RedirectToAction("AdminLogin");
        }

        public IActionResult AdminLogin()
        {
            return View("");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegistrationModelView model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if user already exists
            if (_context.UserAccounts.Any(u => u.StudentNumber == model.StudentNumber))
            {
                ModelState.AddModelError(string.Empty, "Student number is already registered.");
                return View(model);
            }

            var user = new UserAccount
            {
                Name = model.Name,
                StudentNumber = model.StudentNumber,
                Section = model.Section,
                Email = model.Email,
                PasswordHash = model.GetHashedPassword()
            };

            _context.UserAccounts.Add(user);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Registration successful!";
            return RedirectToAction("Register");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult StudentLogin()
        {
            return View("~/Views/Account/StudentLogin.cshtml");
        }


        [Authorize]
        public IActionResult Homepage()
        {
            ViewBag.Name = HttpContext.User.Identity?.Name;
            return View("Homepage");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
