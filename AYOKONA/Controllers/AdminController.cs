using AYOKONA.Entities;
using AYOKONA.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

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
                return RedirectToAction("AdminDashboard");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> AdminDashboard()
        {
            var requests = await _context.Requests
                .Include(r => r.User) // Corrected: Include User to get user details
                .Include(r => r.Group)       // Include Group to get group name and category
                .Include(r => r.Period)      // Include Period to get start and end times
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.RequestId,
                    r.Date,
                    r.PeriodId,
                    PeriodStartTime = r.Period.StartTime.ToString(),
                    PeriodEndTime = r.Period.EndTime.ToString(),
                    GroupName = r.Group.Name,
                    GroupCategory = r.Group.Category,
                    UserName = r.User.Name, // Corrected: Get user's name from r.User
                    UserSection = r.User.Section, // Corrected: Get user's section from r.User
                    r.Purpose,
                    r.ProfInCharge,
                    r.Status,
                    r.CreatedAt // To sort by latest
                })
                .ToListAsync();

            var options = new System.Text.Json.JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            };
            ViewData["ReservationRequests"] = System.Text.Json.JsonSerializer.Serialize(requests, options);

            return View("AdminDashboard");
        }

        public IActionResult AdminManage()
        {
            return View();
        }

        [Authorize]
        [Authorize]
        public IActionResult AdminProfile()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("Email claim not found.");
            }

            var admin = _context.AdminAccounts.FirstOrDefault(a => a.Email == email);

            if (admin == null)
            {
                return NotFound("Admin profile not found.");
            }

            var model = new AdminProfileViewModel
            {
                FullName = admin.Name,
                Department = "Physical Education",
                Position = "Gym Administrator",
                Email = admin.Email,
                Campus = "PUP Quezon City",
                Role = "Student"
                
            };

            return View(model); // Pass admin to the view
        }

        [Authorize]
        public IActionResult AdminEditProfile()
        {
            var email = User.Identity?.Name;
            var admin = _context.AdminAccounts.FirstOrDefault(a => a.Email == email);

            if (admin == null)
            {
                return NotFound("Admin profile not found.");
            }

            return View(admin);
        }
        /*
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdminEditProfile(Admin updatedAdmin)
        {
            if (!ModelState.IsValid)
            {
                return View(updatedAdmin);
            }

            var email = User.Identity.Name;
            var existingAdmin = _context.Admins.FirstOrDefault(a => a.Email == email);

            if (existingAdmin == null)
            {
                return NotFound("Admin profile not found.");
            }

            /* Update fields
            existingAdmin.FullName = updatedAdmin.FullName;
            existingAdmin.Department = updatedAdmin.Department;
            existingAdmin.Position = updatedAdmin.Position;
            existingAdmin.PhoneNumber = updatedAdmin.PhoneNumber;
            existingAdmin.Address = updatedAdmin.Address;
            existingAdmin.EmploymentStatus = updatedAdmin.EmploymentStatus;
            existingAdmin.Campus = updatedAdmin.Campus; 

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction("AdminProfile");
        } */

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
