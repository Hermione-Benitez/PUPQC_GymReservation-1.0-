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
        public IActionResult AdminRegister(AdminLoginViewModel model)
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
                Name = model.Email, // You may want to add a Name field to your view/model for proper admin naming
                Email = model.Email,
                PasswordHash = model.GetHashedPassword()
            };

            _context.AdminAccounts.Add(admin);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Admin registration successful!";
            return RedirectToAction("AdminRegister");
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(StudentLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var hashedPassword = model.GetHashedPassword();

            var user = _context.UserAccounts
                .FirstOrDefault(u => u.StudentNumber == model.StudentNumber && u.PasswordHash == hashedPassword);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim("Name", user.Name),
                    new Claim(ClaimTypes.Role, "Student")
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

                TempData["SuccessMessage"] = "Login successful!";
                return RedirectToAction("Homepage");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid student number or password.");
                return View(model);
            }
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
