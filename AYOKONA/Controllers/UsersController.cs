using AYOKONA.Entities;
using AYOKONA.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace AYOKONA.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult StudentLogin() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StudentLogin(StudentLoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var hashedPassword = model.GetHashedPassword();

            var user = _context.UserAccounts
                .FirstOrDefault(u => u.StudentNumber == model.StudentNumber && u.PasswordHash == hashedPassword);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim("Section", user.Section),
                    new Claim(ClaimTypes.Role, "Student")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = true };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                TempData["SuccessMessage"] = "Login successful!";
                return RedirectToAction("Homepage");
            }

            ModelState.AddModelError(string.Empty, "Invalid student number or password.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Homepage() => View();

        public IActionResult StudentDashboard() => View("StudentDashboard");

        [HttpGet]
        public async Task<IActionResult> AddReservationForm()
        {
            var userAccount = await GetCurrentUserAccountAsync();
            if (userAccount == null)
            {
                TempData["ErrorMessage"] = "You must be logged in.";
                return RedirectToAction("StudentLogin");
            }

            var model = new ReservationFormViewModel
            {
                Name = userAccount.Name,
                Section = userAccount.Section,
                ReservationDate = DateTime.Today
            };

            ViewData["Periods"] = await _context.Periods.OrderBy(p => p.StartTime).ToListAsync();

            // Get all approved requests (date, periodId)
            var approvedSlots = await _context.Requests
                .Where(r => r.Status == "approved")
                .Select(r => new { r.Date, r.PeriodId })
                .ToListAsync();
            ViewData["ApprovedSlots"] = approvedSlots;

            // Get all fully booked slots (date, periodId) with 3 or more reservations
            var fullyBookedSlots = await _context.Reservations
                .GroupBy(r => new { r.Date, r.PeriodId })
                .Where(g => g.Count() >= 3)
                .Select(g => new { g.Key.Date, g.Key.PeriodId })
                .ToListAsync();
            ViewData["FullyBookedSlots"] = fullyBookedSlots;

            return View("AddReservationForm", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReservation(ReservationFormViewModel model)
        {
            var userAccount = await GetCurrentUserAccountAsync();
            if (userAccount == null)
            {
                ModelState.AddModelError("", "You are not logged in.");
                ViewData["Periods"] = await _context.Periods.OrderBy(p => p.StartTime).ToListAsync();
                return View("AddReservationForm", model);
            }

            model.Name = userAccount.Name;
            model.Section = userAccount.Section;

            if (model.Category == "org")
            {
                if (string.IsNullOrWhiteSpace(model.OrganizationName))
                {
                    ModelState.AddModelError("OrganizationName", "Organization is required when category is 'org'.");
                }
            }
            else if (model.Category == "section")
            {
                if (string.IsNullOrWhiteSpace(model.Section))
                {
                    ModelState.AddModelError("Section", "Section is required when category is 'section'.");
                }

                model.OrganizationName = null;
            }
            else
            {
                ModelState.AddModelError("Category", "Please select a valid category.");
            }

            if (model.ReservationDate < DateTime.Today)
            {
                ModelState.AddModelError("ReservationDate", "Reservation date cannot be in the past.");
            }

            if (model.PeriodId == 0)
            {
                ModelState.AddModelError("PeriodId", "Please select a valid time period.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Periods"] = await _context.Periods.OrderBy(p => p.StartTime).ToListAsync();
                return View("AddReservationForm", model);
            }

            // Updated part: Auto-create group if not found (for section)
            string groupName = model.Category == "section"
                ? userAccount.Section?.Trim() ?? ""
                : model.OrganizationName?.Trim() ?? "";

            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.Category == model.Category && g.Name == groupName);

            if (group == null)
            {
                if (model.Category == "section" && !string.IsNullOrWhiteSpace(groupName))
                {
                    group = new Group
                    {
                        Name = groupName,
                        Category = "section"
                    };
                    _context.Groups.Add(group);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    ModelState.AddModelError("", $"No matching group found for category '{model.Category}' and name '{groupName}'.");
                    ViewData["Periods"] = await _context.Periods.OrderBy(p => p.StartTime).ToListAsync();
                    return View("AddReservationForm", model);
                }
            }

            var period = await _context.Periods.FindAsync(model.PeriodId);
            if (period == null)
            {
                ModelState.AddModelError("PeriodId", "Invalid time slot.");
                ViewData["Periods"] = await _context.Periods.OrderBy(p => p.StartTime).ToListAsync();
                return View("AddReservationForm", model);
            }

            // Prevent more than 3 reservations for the same period and date across ALL users
            var existingReservationsCount = await _context.Reservations
                .CountAsync(r => r.PeriodId == model.PeriodId && r.Date == model.ReservationDate);
            if (existingReservationsCount >= 3)
            {
                ModelState.AddModelError("", "The selected time slot and date is fully booked.");
                ViewData["Periods"] = await _context.Periods.OrderBy(p => p.StartTime).ToListAsync();
                return View("AddReservationForm", model);
            }

            // Prevent more than 3 reservation attempts per real-time day
            var today = DateTime.Today;
            var todayAttempts = await _context.Reservations
                .CountAsync(r => r.UserId == userAccount.Id && r.CreatedAt.Date == today);
            if (todayAttempts >= 3)
            {
                ModelState.AddModelError("", "You have reached the maximum of 3 reservation attempts for today. Please return tomorrow");
                ViewData["Periods"] = await _context.Periods.OrderBy(p => p.StartTime).ToListAsync();
                return View("AddReservationForm", model);
            }

            var newReservation = new Reservation
            {
                UserId = userAccount.Id,
                GroupId = group.GroupId,
                PeriodId = period.PeriodId,
                Date = model.ReservationDate,
                Purpose = model.PurposeOfUse,
                ProfInCharge = model.ProfessorInCharge,
                Status = "ongoing",
                CreatedAt = DateTime.Now
            };

            // Add the reservation first
            _context.Reservations.Add(newReservation);
            await _context.SaveChangesAsync(); // newReservation.ReservationId is now set

            // Now create the request and link it
            var newRequest = new Request
            {
                UserId = userAccount.Id,
                GroupId = group.GroupId,
                PeriodId = period.PeriodId,
                Date = model.ReservationDate,
                Purpose = model.PurposeOfUse,
                ProfInCharge = model.ProfessorInCharge,
                Status = "pending",
                CreatedAt = DateTime.Now,
                ReservationId = newReservation.ReservationId // Link to reservation
            };

            _context.Requests.Add(newRequest);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Reservation submitted successfully!";
            return RedirectToAction("StudentDashboard");
        }

        public IActionResult UserReservation() => View();

        public IActionResult UsersProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("StudentLogin");

            int id = int.Parse(userId);
            var user = _context.UserAccounts.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            var model = new StudentProfileViewModel
            {
                FullName = user.Name,
                Email = user.Email,
                StudentNumber = user.StudentNumber,
                Section = user.Section,
                Campus = "PUP Quezon City",
                Role = "Student"
            };

            return View(model);
        }

        private async Task<UserAccount?> GetCurrentUserAccountAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return null;

            int id = int.Parse(userId);
            return await _context.UserAccounts.FindAsync(id);
        }
    }
}