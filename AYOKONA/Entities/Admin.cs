using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AYOKONA.Entities
{
    public class AdminAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Ensure auto-increment
        public int Id { get; set; }

        [Required(ErrorMessage = "Required. Please fill in this field.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Required. Please fill in this field.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Required. Please fill in this field.")]
        public string PasswordHash { get; set; }
    }
}