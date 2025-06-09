using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;

namespace AYOKONA.Models
{
    public class AdminLoginViewModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Required. Please fill in this field.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Required. Please fill in this field.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string GetHashedPassword()
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(Password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
