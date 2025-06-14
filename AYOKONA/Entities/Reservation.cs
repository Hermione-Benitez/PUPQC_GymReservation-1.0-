using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AYOKONA.Entities
{
    public class Reservation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("reservation_id")]
        public int ReservationId { get; set; }

        [Required]
        [Column("user_account_id")]
        public int UserId { get; set; }

        [Required]
        [Column("group_id")]
        public int GroupId { get; set; }

        [Required]
        [Column("period_id")]
        public int PeriodId { get; set; }

        [Required]
        [Column("date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [Column("Purpose")]
        [MaxLength(200)]
        public string Purpose { get; set; } = string.Empty;

        [Required]
        [Column("prof_in_charge")]
        [MaxLength(100)]
        public string ProfInCharge { get; set; } = string.Empty;

        [Column("status")]
        [MaxLength(50)]
        public string Status { get; set; } = "ongoing";

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public UserAccount User { get; set; } = null!;

        [ForeignKey("GroupId")]
        public Group Group { get; set; } = null!;

        [ForeignKey("PeriodId")]
        public Period Period { get; set; } = null!;
    }
}
