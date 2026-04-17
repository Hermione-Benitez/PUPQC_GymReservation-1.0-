using AYOKONA.Entities;
using AYOKONA.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AYOKONA.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // DTO for updating reservation details
        public class UpdateReservationViewModel
        {
            public int ReservationId { get; set; }
            public string Purpose { get; set; }
            public string ProfInCharge { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReservation([FromBody] UpdateReservationViewModel model)
        {
            var userAccount = await GetCurrentUserAccountAsync();
            if (userAccount == null)
            {
                return Json(new { success = false, message = "Not authenticated." });
            }

            var reservation = await _context.Reservations
                .Where(r => r.ReservationId == model.ReservationId && r.UserId == userAccount.Id)
                .FirstOrDefaultAsync();

            if (reservation == null)
            {
                return Json(new { success = false, message = "Reservation not found or you don't have permission to edit it." });
            }

            // Only allow editing for 'ongoing' reservations
            if (reservation.Status != "ongoing")
            {
                return Json(new { success = false, message = "Only ongoing reservations can be edited." });
            }

            // Update reservation details
            reservation.Purpose = model.Purpose;
            reservation.ProfInCharge = model.ProfInCharge;

            try
            {
                // Find and update the corresponding request
                var request = await _context.Requests
                    .Where(r => r.ReservationId == reservation.ReservationId)
                    .FirstOrDefaultAsync();

                if (request != null)
                {
                    request.Purpose = model.Purpose;
                    request.ProfInCharge = model.ProfInCharge;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Reservation and request updated successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception (e.g., using a logging framework)
                Debug.WriteLine($"Error updating reservation and request: {ex.Message}");
                return Json(new { success = false, message = "Error updating reservation and request." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DiscardReservation([FromBody] UpdateReservationViewModel model)
        {
            var userAccount = await GetCurrentUserAccountAsync();
            if (userAccount == null)
            {
                return Json(new { success = false, message = "Not authenticated." });
            }

            var reservation = await _context.Reservations
                .Where(r => r.ReservationId == model.ReservationId && r.UserId == userAccount.Id)
                .FirstOrDefaultAsync();

            if (reservation == null)
            {
                return Json(new { success = false, message = "Reservation not found or you don't have permission to discard it." });
            }

            // Only allow discarding for 'ongoing' reservations
            if (reservation.Status != "ongoing")
            {
                return Json(new { success = false, message = "Only ongoing reservations can be discarded." });
            }

            try
            {
                // Update reservation status
                reservation.Status = "cancelled";

                // Find and update the corresponding request
                var request = await _context.Requests
                    .Where(r => r.ReservationId == reservation.ReservationId)
                    .FirstOrDefaultAsync();

                if (request != null)
                {
                    request.Status = "cancelled";
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Reservation and request have been cancelled successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception (e.g., using a logging framework)
                Debug.WriteLine($"Error discarding reservation and request: {ex.Message}");
                return Json(new { success = false, message = "Error discarding reservation and request." });
            }
        }

        [HttpGet]
        public IActionResult StudentLogin() => View("~/Views/Account/StudentLogin.cshtml");

        [HttpPost("StudentLogin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StudentLogin(StudentLoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Account/StudentLogin.cshtml", model);

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
            return View("~/Views/Account/StudentLogin.cshtml", model);
        }

        [HttpGet]
        public IActionResult Homepage() => View();

        public async Task<IActionResult> StudentDashboard(int? month, int? year)
        {
            var userAccount = await GetCurrentUserAccountAsync();
            if (userAccount == null)
            {
                TempData["ErrorMessage"] = "You must be logged in.";
                return RedirectToAction("StudentLogin");
            }

            // Set current month and year for calendar display
            DateTime targetDate = DateTime.Today;
            if (month.HasValue && year.HasValue)
            {
                targetDate = new DateTime(year.Value, month.Value, 1);
            }

            ViewData["CurrentMonth"] = targetDate.Month;
            ViewData["CurrentYear"] = targetDate.Year;

            var userReservations = await _context.Reservations
                .Where(r => r.UserId == userAccount.Id)
                .Include(r => r.Group)
                .Include(r => r.Period)
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.ReservationId,
                    r.Date,
                    r.PeriodId,
                    PeriodStartTime = r.Period.StartTime.ToString(),
                    PeriodEndTime = r.Period.EndTime.ToString(),
                    GroupName = r.Group.Name,
                    GroupCategory = r.Group.Category,
                    r.Purpose,
                    r.ProfInCharge,
                    r.Status
                })
                .ToListAsync();

            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };
            ViewData["UserReservations"] = JsonSerializer.Serialize(userReservations, options);

            // Fetch Periods for column headers, excluding 'Whole Day' (PeriodId 6)
            var periods = await _context.Periods
                .Where(p => p.PeriodId != 6) // Exclude PeriodId 6 (Whole Day)
                .OrderBy(p => p.StartTime)
                .Select(p => new { p.PeriodId, StartTime = p.StartTime.ToString(), EndTime = p.EndTime.ToString() })
                .ToListAsync();
            ViewData["Periods"] = JsonSerializer.Serialize(periods, options);

            // Fetch approved reservations for the current month and year
            var approvedCalendarSlots = await _context.Requests
                .Where(r => r.Status == "approved" && r.Date.Month == targetDate.Month && r.Date.Year == targetDate.Year)
                .Select(r => new { r.Date, r.PeriodId })
                .ToListAsync();
            ViewData["ApprovedCalendarSlots"] = JsonSerializer.Serialize(approvedCalendarSlots, options);

            // Fetch fully booked slots (3 or more reservations for a specific period on a date)
            var fullyBookedSlots = await _context.Reservations
                .Where(r => r.Date.Month == targetDate.Month && r.Date.Year == targetDate.Year)
                .GroupBy(r => new { r.Date, r.PeriodId })
                .Where(g => g.Count() >= 3)
                .Select(g => new { g.Key.Date, g.Key.PeriodId })
                .ToListAsync();
            ViewData["FullyBookedSlots"] = JsonSerializer.Serialize(fullyBookedSlots, options);

            // Fetch 'Whole Day' approved/completed requests/reservations
            var wholeDayReservations = await _context.Reservations
                .Where(r => r.Date.Month == targetDate.Month && r.Date.Year == targetDate.Year && r.PeriodId == 6)
                .Select(r => new { r.Date })
                .ToListAsync();

            var wholeDayRequests = await _context.Requests
                .Where(r => r.Date.Month == targetDate.Month && r.Date.Year == targetDate.Year && r.PeriodId == 6 && r.Status == "approved")
                .Select(r => new { r.Date })
                .ToListAsync();

            var wholeDayBlockedDates = wholeDayReservations.Select(r => r.Date)
                                        .Concat(wholeDayRequests.Select(req => req.Date))
                                        .Distinct()
                                        .ToList();
            ViewData["WholeDayBlockedDates"] = JsonSerializer.Serialize(wholeDayBlockedDates, options);

            return View("StudentDashboard");
        }

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
                ReservationDate = DateTime.Today.AddDays(1)
            };

            ViewData["Periods"] = await _context.Periods.OrderBy(p => p.StartTime).ToListAsync();
            await PopulateReservationSlotsViewData(); // Call helper method to populate all slot data

            // Get all approved requests (date, periodId) - These generally make a slot unavailable for any group, e.g. for facility closure
            // If an approved request means the group can re-reserve, this list should only contain truly generally unavailable slots (e.g. PeriodId 6 Whole Day, or if the facility is full)
            var approvedSlots = await _context.Requests
                .Where(r => r.Status == "approved")
                .Select(r => new { r.Date, r.PeriodId })
                .ToListAsync();
            ViewData["ApprovedSlots"] = approvedSlots;

            // Get all fully booked slots (date, periodId) with 3 or more reservations - General time slot limit
            var fullyBookedSlots = await _context.Reservations
                .GroupBy(r => new { r.Date, r.PeriodId })
                .Where(g => g.Count() >= 3)
                .Select(g => new { g.Key.Date, g.Key.PeriodId })
                .ToListAsync();
            ViewData["FullyBookedSlots"] = fullyBookedSlots;

            // Get group-specific blocking slots (ongoing reservations or pending requests for the specific group)
            // These combinations will prevent new reservations for the same group/date/period
            var blockingReservations = await _context.Reservations
                .Where(r => r.Status == "ongoing") // Ongoing reservations block for a group
                .Include(r => r.Group)
                .Select(r => new
                {
                    r.Date,
                    r.PeriodId,
                    r.GroupId,
                    GroupName = r.Group.Name,
                    GroupCategory = r.Group.Category
                })
                .ToListAsync();

            var blockingRequests = await _context.Requests
                .Where(r => r.Status == "pending") // Pending requests block for a group
                .Include(r => r.Group)
                .Select(r => new
                {
                    r.Date,
                    r.PeriodId,
                    r.GroupId,
                    GroupName = r.Group.Name,
                    GroupCategory = r.Group.Category
                })
                .ToListAsync();

            var blockingSlots = blockingReservations
                .Concat(blockingRequests)
                .Distinct() // Ensure unique entries based on all properties
                .ToList();

            ViewData["BlockingSlots"] = blockingSlots;

            // Get all relevant reservation details for client-side validation
            var existingReservations = await _context.Reservations
                .Include(r => r.Group) // Include Group to get Name and Category
                .Select(r => new
                {
                    r.ReservationId,
                    r.Date,
                    r.PeriodId,
                    r.GroupId,
                    r.Status,
                    GroupName = r.Group.Name, // Get the name of the group/section/organization
                    GroupCategory = r.Group.Category // Get the category of the group (section/org)
                })
                .ToListAsync();
            ViewData["ExistingReservations"] = existingReservations;

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
                await PopulateReservationSlotsViewData(); // Re-populate for error return
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

            if (model.ReservationDate < DateTime.Today.AddDays(1)) // Check against tomorrow for future dates
            {
                ModelState.AddModelError("ReservationDate", "Reservation date cannot be today or in the past.");
            }

            if (model.PeriodId == 0)
            {
                ModelState.AddModelError("PeriodId", "Please select a valid time period.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Periods"] = await _context.Periods.OrderBy(p => p.StartTime).ToListAsync();
                await PopulateReservationSlotsViewData(); // Re-populate for error return
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
                    await PopulateReservationSlotsViewData(); // Re-populate for error return
                    return View("AddReservationForm", model);
                }
            }

            // Server-side validation: Prevent duplicate ongoing reservations or pending requests for the same group/date/period
            Debug.WriteLine($"Server-side validation check started for GroupId: {group.GroupId}, PeriodId: {model.PeriodId}, Date: {model.ReservationDate:yyyy-MM-dd}");

            var existingOngoingReservation = await _context.Reservations
                .AnyAsync(r => r.GroupId == group.GroupId &&
                               r.PeriodId == model.PeriodId &&
                               r.Date == model.ReservationDate &&
                               r.Status == "ongoing");
            Debug.WriteLine($"Existing Ongoing Reservation found: {existingOngoingReservation}");

            var existingPendingRequest = await _context.Requests
                .AnyAsync(req => req.GroupId == group.GroupId &&
                                 req.PeriodId == model.PeriodId &&
                                 req.Date == model.ReservationDate &&
                                 req.Status == "pending");
            Debug.WriteLine($"Existing Pending Request found: {existingPendingRequest}");

            if (existingOngoingReservation || existingPendingRequest)
            {
                Debug.WriteLine("Blocking submission: Duplicate active reservation/request found.");
                ModelState.AddModelError("", $"A reservation for {group.Name} on {model.ReservationDate:yyyy-MM-dd} during the selected time period is already ongoing or pending. Please select a different date or time, or wait for the existing reservation/request to be completed/approved.");
                ViewData["Periods"] = await _context.Periods.OrderBy(p => p.StartTime).ToListAsync();
                await PopulateReservationSlotsViewData(); // Call helper method to re-populate for error
                return View("AddReservationForm", model);
            }
            Debug.WriteLine("No blocking reservation/request found. Proceeding with submission.");

            var period = await _context.Periods.FindAsync(model.PeriodId);
            if (period == null)
            {
                ModelState.AddModelError("PeriodId", "Invalid time slot.");
                ViewData["Periods"] = await _context.Periods.OrderBy(p => p.StartTime).ToListAsync();
                await PopulateReservationSlotsViewData(); // Re-populate for error return
                return View("AddReservationForm", model);
            }

            // Prevent more than 3 reservations for the same period and date across ALL users
            var existingReservationsCount = await _context.Reservations
                .CountAsync(r => r.PeriodId == model.PeriodId && r.Date == model.ReservationDate);
            if (existingReservationsCount >= 3)
            {
                ModelState.AddModelError("", "The selected time slot and date is fully booked.");
                ViewData["Periods"] = await _context.Periods.OrderBy(p => p.StartTime).ToListAsync();
                await PopulateReservationSlotsViewData(); // Re-populate for error return
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
                await PopulateReservationSlotsViewData(); // Re-populate for error return
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

            TempData["SubmissionSuccess"] = true; // Set a flag for success message
            
            return RedirectToAction("UserReservation");


        }

        // Helper method to populate ViewData for reservation slots
        private async Task PopulateReservationSlotsViewData()
        {
            // Get all approved requests and their associated reservations
            var approvedRequests = await _context.Requests
                .Include(r => r.Reservation)
                .Include(r => r.Group)
                .Where(r => r.Status == "approved")
                .ToListAsync();

            // Get all pending requests
            var pendingRequests = await _context.Requests
                .Include(r => r.Group)
                .Where(r => r.Status == "pending")
                .ToListAsync();

            // Get all fully booked slots (approved reservations)
            var fullyBookedSlots = approvedRequests
                .Select(r => new
                {
                    Date = r.Date.ToString("yyyy-MM-dd"),
                    PeriodId = r.PeriodId,
                    GroupName = r.Group.Name,
                    Category = r.Group.Category
                })
                .ToList();

            // Get all blocking slots (pending requests)
            var blockingSlots = pendingRequests
                .Select(r => new
                {
                    Date = r.Date.ToString("yyyy-MM-dd"),
                    PeriodId = r.PeriodId,
                    GroupName = r.Group.Name,
                    Category = r.Group.Category
                })
                .ToList();

            // Check for approved Whole Day reservations (PeriodId 6)
            var wholeDayRequests = await _context.Requests
                .Include(r => r.Group)
                .Where(r => r.Status == "approved" && r.PeriodId == 6)
                .ToListAsync();

            // Add all time slots for dates with approved Whole Day reservations to fullyBookedSlots
            var allPeriods = await _context.Periods.ToListAsync();
            foreach (var wholeDayReq in wholeDayRequests)
            {
                foreach (var period in allPeriods)
                {
                    fullyBookedSlots.Add(new
                    {
                        Date = wholeDayReq.Date.ToString("yyyy-MM-dd"),
                        PeriodId = period.PeriodId,
                        GroupName = wholeDayReq.Group.Name,
                        Category = wholeDayReq.Group.Category
                    });
                }
            }

            ViewData["ApprovedSlots"] = fullyBookedSlots;
            ViewData["FullyBookedSlots"] = fullyBookedSlots;
            ViewData["BlockingSlots"] = blockingSlots;
        }

        public async Task<IActionResult> UserReservation()
        {
            var userAccount = await GetCurrentUserAccountAsync();
            if (userAccount == null)
            {
                TempData["ErrorMessage"] = "You must be logged in.";
                return RedirectToAction("StudentLogin");
            }

            var userReservations = await _context.Reservations
                .Where(r => r.UserId == userAccount.Id)
                .Include(r => r.Group)
                .Include(r => r.Period)
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.ReservationId,
                    r.Date,
                    r.PeriodId,
                    PeriodStartTime = r.Period.StartTime.ToString(),
                    PeriodEndTime = r.Period.EndTime.ToString(),
                    GroupName = r.Group.Name,
                    GroupCategory = r.Group.Category,
                    r.Purpose,
                    r.ProfInCharge,
                    r.Status
                })
                .ToListAsync();

            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };
            ViewData["UserReservations"] = JsonSerializer.Serialize(userReservations, options);

            return View();
        }

        public IActionResult UsersProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("StudentLogin");
            }
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

        [Authorize]
        public async Task<IActionResult> UserEditProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "You must be logged in.";
                return RedirectToAction("StudentLogin");
            }
            int id = int.Parse(userId);
            var user = await _context.UserAccounts.FindAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User profile not found.";
                return RedirectToAction("UsersProfile");
            }
            var model = new AYOKONA.Models.UserEditProfileView
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Section = user.Section,
                StudentNumber = user.StudentNumber,
            };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserEditProfile(UserEditProfileView model)
        {
            Console.WriteLine($"Model.Id: {model.Id}, Name: {model.Name}, Email: {model.Email}");
            var existingUser = await _context.UserAccounts.FindAsync(model.Id);
            if (existingUser == null)
            {
                Console.WriteLine("User not found in DB.");
            }
            else
            {
                Console.WriteLine($"Before: {existingUser.Name}, {existingUser.Email}");
                existingUser.Name = model.Name;
                existingUser.Email = model.Email;
                existingUser.Section = model.Section;
                existingUser.StudentNumber = model.StudentNumber;
                var result = await _context.SaveChangesAsync();
                Console.WriteLine($"SaveChangesAsync result: {result}");
                Console.WriteLine($"After: {existingUser.Name}, {existingUser.Email}");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            bool emailChanged = existingUser.Email != model.Email;
            existingUser.Name = model.Name;
            existingUser.Email = model.Email;
            existingUser.Section = model.Section;
            existingUser.StudentNumber = model.StudentNumber;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile updated successfully.";

            // If email changed, update authentication cookie
            if (emailChanged)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, existingUser.Id.ToString()),
                    new Claim(ClaimTypes.Name, existingUser.Name),
                    new Claim(ClaimTypes.Email, existingUser.Email),
                    new Claim("Section", existingUser.Section),
                    new Claim(ClaimTypes.Role, "Student")
                };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity)
                );
            }

            return RedirectToAction("UsersProfile");
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