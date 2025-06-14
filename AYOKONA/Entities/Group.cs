using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AYOKONA.Entities
{
    public class Group
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("group_id")] // Maps C# property to SQL column name
        public int GroupId { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(255)] // Based on NVARCHAR (255)
        public string Name { get; set; } = string.Empty; // Initialized to prevent nullability warning

        [Required]
        [Column("category")]
        [MaxLength(50)] // Based on NVARCHAR (50)
        public string Category { get; set; } = string.Empty; // Initialized to prevent nullability warning

        // Navigation property: A Group can have many Reservations
        // Initialized to prevent nullability warning
        public ICollection<Reservation> Reservations { get; set; } = new HashSet<Reservation>();
    }
}
