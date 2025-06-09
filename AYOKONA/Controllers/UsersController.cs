using AYOKONA.Entities;
using AYOKONA.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;

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
        public IActionResult StudentLogin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StudentLogin(StudentLoginViewModel model)
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
                // Only allow users with the "Student" role (or no admin privileges)
                // If you have a Role property, check it here. Example:
                // if (user.Role != "Student") { ModelState.AddModelError(...); return View(model); }

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
        [HttpGet]
        public IActionResult Homepage()
        {
            // Optionally set ViewData["Username"] here
            return View();
        }

        // Action to display the User Dashboard
        public IActionResult StudentDashboard()
        {
            return View("StudentDashboard");
        }

        // This action will be hit when the user clicks "Add New Reservation" from the Home page
        public IActionResult Add()
        {
            // Get the logged-in user's name
            var userName = User.Identity?.Name ?? "Unknown User";

            // Fetch the user from the UserAccounts DbSet
            var user = _context.UserAccounts.FirstOrDefault(u => u.Name == userName);

            if (user == null)
            {
                return NotFound();
            }

            ViewData["Fullname"] = user.Name;
            ViewData["Section"] = user.Section;

            return View("AddReservationForm");
        }

        // This action will be hit when the user clicks the "Submit" button on the reservation form
        [HttpPost]
        public IActionResult SubmitReservation(IFormCollection form)
        {
            var category = form["Category"];
            var professorInCharge = form["ProfessorInCharge"];
            var purposeOfUse = form["PurposeOfUse"];
            var reservationDateString = form["ReservationDate"]; // Date comes as a string (YYYY-MM-DD)
            var reservationTime = form["ReservationTime"];

            // var userName = form["Name"];
            // var userSection = form["Section"];

            DateTime reservationDate;
            if (!DateTime.TryParse(reservationDateString, out reservationDate))
            {
                ModelState.AddModelError("ReservationDate", "Invalid reservation date.");
                ViewData["Fullname"] = form["Name"];
                ViewData["Section"] = form["Section"];
                return View("AddReservationForm");
            }

            // --- Add your logic here to save the reservation to your database ---
            /*
            var newReservation = new YourProjectNamespace.Models.Reservation // Replace with your actual model class path
            {
                Category = category,
                // You might get the Name and Section from the current authenticated user instead of hidden fields
                // Or use them from the form if the user is allowed to change them
                Name = form["Name"],
                Section = form["Section"],
                ProfessorInCharge = professorInCharge,
                PurposeOfUse = purposeOfUse,
                ReservationDate = reservationDate, // The parsed DateTime object
                TimeSlot = reservationTime,
                Status = "Pending" // Set initial status (e.g., Pending, Approved, Rejected)
                // Add any other properties like UserId from your authentication system
            };

            _dbContext.Reservations.Add(newReservation); // Assuming _dbContext is your database context
            _dbContext.SaveChanges(); // Persist the changes to the database
            */
            // --- End database saving logic ---

            // After successfully processing and saving, redirect the user
            TempData["SuccessMessage"] = "Your reservation has been submitted successfully!"; // Optional: message for next page
            return RedirectToAction("StudentDashboard"); // Redirects to the Dashboard action in this controller
        }

        // Action to display general Gym Reservations (e.g., a calendar of all reservations)
        public IActionResult AddReservationForm()
        {
            return View(); // This will look for Views/User/Reservation.cshtml by convention
        }

        // Action to display a specific user's reservations
        // This action would be hit by the "/User/Reservations/My" link from the Home page
        // You might need to adjust routing or consider using a single 'Reservation' action with parameters
        // if "Reservation" and "My" are meant to show different views of reservations.
        // For now, let's assume this displays the *current user's* reservations.
        public IActionResult UserReservation()
        {
            // Logic to fetch reservations specific to the logged-in user
            return View(); // This will look for Views/User/My.cshtml by convention
        }

        // Action to display the user's profile
        public ActionResult UsersProfile()
        {
            // Get the logged-in user's name
            var userName = User.Identity?.Name;

            // Fetch the user from the UserAccounts table
            var user = _context.UserAccounts.FirstOrDefault(u => u.Name == userName);

            if (user == null)
            {
                return NotFound();
            }

            var model = new StudentProfileViewModel
            {
                FullName = user.Name,
                Email = user.Email,
                StudentNumber = user.StudentNumber,
                Section = user.Section,

                Campus = "PUP Quezon City",
                Role = "Student",
                // Set other properties as needed, or leave them default/null
            };

            return View(model);
        }


        // Other user-related actions...
    }
}

