using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AYOKONA.Models
{
    public class UserEditProfileView
    {
       
        public string Name { get; set; }

        [Required(ErrorMessage = "Required. Please fill in this field.")]
        [RegularExpression(@"^\d{4}-\d{5}-CM-\d{1}$", ErrorMessage = "Student Number must be in the format '2023-00030-CM-0'.")]
        public string StudentNumber { get; set; }

        [Required(ErrorMessage = "Required. Please fill in this field.")]
        public string Section { get; set; }

        [Required(ErrorMessage = "Required. Please fill in this field.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; }

        public string Organization { get; set; }

    }
}
