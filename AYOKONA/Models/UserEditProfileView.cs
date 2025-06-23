using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AYOKONA.Models
{
    public class UserEditProfileView
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [RegularExpression(@"^\d{4}-\d{5}-CM-\d{1}$", ErrorMessage = "Student Number must be in the format '2023-00030-CM-0'.")]
        public string StudentNumber { get; set; }

        public string Section { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; }

    }
}
