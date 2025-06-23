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
        public async Task<IActionResult> AdminDashboard(int? month, int? year)
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
                .Take(3)
                .ToListAsync();

            var options = new System.Text.Json.JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            };
            ViewData["ReservationRequests"] = System.Text.Json.JsonSerializer.Serialize(requests, options);

            // Calendar Data
            DateTime targetDate = DateTime.Today;
            if (month.HasValue && year.HasValue)
            {
                targetDate = new DateTime(year.Value, month.Value, 1);
            }

            ViewData["CurrentMonth"] = targetDate.Month;
            ViewData["CurrentYear"] = targetDate.Year;

            var periods = await _context.Periods
                .Where(p => p.PeriodId != 6) // Exclude PeriodId 6 (Whole Day)
                .OrderBy(p => p.StartTime)
                .Select(p => new { p.PeriodId, StartTime = p.StartTime.ToString(), EndTime = p.EndTime.ToString() })
                .ToListAsync();
            ViewData["Periods"] = System.Text.Json.JsonSerializer.Serialize(periods, options);

            var approvedCalendarSlots = await _context.Requests
                .Where(r => r.Status == "approved" && r.Date.Month == targetDate.Month && r.Date.Year == targetDate.Year)
                .Select(r => new { r.Date, r.PeriodId })
                .ToListAsync();
            ViewData["ApprovedCalendarSlots"] = System.Text.Json.JsonSerializer.Serialize(approvedCalendarSlots, options);

            return View("AdminDashboard");
        }

        public IActionResult AdminManage()
        {
            var requests = _context.Requests
                .Include(r => r.User)
                .Include(r => r.Group)
                .Include(r => r.Period)
                .ToList();

            var approved = requests.Where(r => r.Status.ToLower() == "approved").ToList();
            var pending = requests.Where(r => r.Status.ToLower() == "pending").ToList();
            var declined = requests.Where(r => r.Status.ToLower() == "declined" || r.Status.ToLower() == "denied" || r.Status.ToLower() == "cancelled").ToList();

            ViewData["Approved"] = approved;
            ViewData["Pending"] = pending;
            ViewData["Declined"] = declined;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRequestStatus(int id, string status)
        {
            var request = await _context.Requests
                .Include(r => r.Reservation)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request != null)
            {
                if (status == "Approved")
                {
                    request.Status = "Approved";
                    if (request.Reservation != null)
                    {
                        request.Reservation.Status = "completed";
                    }

                    // Auto-cancel other requests with the same date and period
                    var conflictingRequests = await _context.Requests
                        .Include(r => r.Reservation)
                        .Where(r =>
                            r.RequestId != request.RequestId &&
                            r.Date == request.Date &&
                            r.PeriodId == request.PeriodId &&
                            r.Status.ToLower() == "pending")
                        .ToListAsync();

                    foreach (var other in conflictingRequests)
                    {
                        other.Status = "Denied";
                        if (other.Reservation != null)
                        {
                            other.Reservation.Status = "denied";
                        }
                    }
                }
                else if (status == "Declined")
                {
                    request.Status = "Declined";
                    if (request.Reservation != null)
                    {
                        request.Reservation.Status = "denied";
                    }
                }
                else
                {
                    request.Status = status;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Request has been {status}.";
                return RedirectToAction(nameof(AdminManage));
            }
            return RedirectToAction("AdminManage");
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
                Department = admin.Department,
                Position = admin.Position,
                Email = admin.Email,
                Campus = "PUP Quezon City",
                Role = "Student"

            };

            return View(model); // Pass admin to the view
        }

        [Authorize]
        public IActionResult AdminEditProfile()
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            var admin = _context.AdminAccounts.FirstOrDefault(a => a.Email == email);
            if (admin == null)
            {
                return NotFound("Admin profile not found.");
            }
            var model = new AYOKONA.Models.AdminEditProfileView
            {
                Id = admin.Id,
                Name = admin.Name,
                Email = admin.Email,
                Department = admin.Department ?? "Physical Education",
                Position = admin.Position ?? "Gym Administrator"
            };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminEditProfile(AdminEditProfileView model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var existingAdmin = _context.AdminAccounts.FirstOrDefault(a => a.Id == model.Id);
            if (existingAdmin == null)
            {
                return NotFound("Admin profile not found.");
            }
            bool emailChanged = existingAdmin.Email != model.Email;
            existingAdmin.Name = model.Name;
            existingAdmin.Email = model.Email;
            existingAdmin.Department = model.Department;
            existingAdmin.Position = model.Position;
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Profile updated successfully.";

            // If email changed, update authentication cookie
            if (emailChanged)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, existingAdmin.Name),
                    new Claim(ClaimTypes.Email, existingAdmin.Email)
                };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity)
                );
            }
            return RedirectToAction("AdminProfile");
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
