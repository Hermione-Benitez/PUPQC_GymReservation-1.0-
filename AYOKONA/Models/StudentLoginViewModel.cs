using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace AYOKONA.Models
{
    public class StudentLoginViewModel
    {
        [Required(ErrorMessage = "Student Number is required.")]
        public string StudentNumber { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        // Returns the SHA256 hash of the password
        public string GetHashedPassword()
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(Password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}