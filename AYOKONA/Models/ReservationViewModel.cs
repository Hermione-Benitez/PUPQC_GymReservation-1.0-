using System;
using System.ComponentModel.DataAnnotations;

namespace AYOKONA.Models
{
    public class ReservationFormViewModel
    {
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty; // Auto-filled, readonly in form

        public string Section { get; set; } = string.Empty; // Auto-filled, readonly in form

        [Required(ErrorMessage = "Please select a category.")]
        [Display(Name = "Reservation Category")]
        public string Category { get; set; } = string.Empty; // "section" or "org"

        [Required(ErrorMessage = "Professor in charge is required.")]
        [Display(Name = "Professor In Charge")]
        public string ProfessorInCharge { get; set; } = string.Empty;

        [Required(ErrorMessage = "Purpose of use is required.")]
        [Display(Name = "Purpose of Use")]
        public string PurposeOfUse { get; set; } = string.Empty;

        [Required(ErrorMessage = "Reservation date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Reservation Date")]
        public DateTime ReservationDate { get; set; }

        [Required(ErrorMessage = "Please select a time period.")]
        [Display(Name = "Time Slot")]
        public int PeriodId { get; set; }

        [Display(Name = "Organization Name")]
        public string? OrganizationName { get; set; } // ✅ Nullable – only validated in controller if category is "org"
    }
}
