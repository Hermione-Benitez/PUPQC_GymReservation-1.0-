using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace AYOKONA.Models
{
    public class RegistrationModelView
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Required. Please fill in this field.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Required. Please fill in this field.")]
        [RegularExpression(@"^\d{4}-\d{5}-CM-\d{1}$", ErrorMessage = "Student Number must be in the format '0000-00000-CM-0'.")]
        public string StudentNumber { get; set; }

        [Required(ErrorMessage = "Required. Please fill in this field.")]
        public string Section { get; set; }

        [Required(ErrorMessage = "Required. Please fill in this field.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Required. Please fill in this field.")]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; } // Store hashed password

        [Compare("PasswordHash", ErrorMessage = "Please Confirm your password.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } // For confirmation, not stored in the database

        public string GetHashedPassword()
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(PasswordHash);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}