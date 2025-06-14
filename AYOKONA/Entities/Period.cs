using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AYOKONA.Entities
{
    public class Period
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("period_id")]
        public int PeriodId { get; set; }

        [Required]
        [Column("start_time")]
        [DataType(DataType.Time)] // Represents time without date
        public TimeSpan StartTime { get; set; }

        [Required]
        [Column("end_time")]
        [DataType(DataType.Time)] // Represents time without date
        public TimeSpan EndTime { get; set; }

        // Navigation property: A Period can have many Reservations
        // Initialized to prevent nullability warning
        public ICollection<Reservation> Reservations { get; set; } = new HashSet<Reservation>();
    }

}
