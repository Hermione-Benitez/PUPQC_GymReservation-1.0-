using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AYOKONA.Entities
{
    [Table("Requests")]
    public class Request
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("request_id")]
        public int RequestId { get; set; }

        [Required]
        [Column("user_account_id")]
        public int UserId { get; set; }

        [Required]
        [Column("period_id")]
        public int PeriodId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Column("date")]
        public DateTime Date { get; set; }

        [Required]
        [StringLength(500)]
        [Column("purpose")]
        public string Purpose { get; set; }

        [Required]
        [StringLength(100)]
        [Column("prof_in_charge")]
        [Display(Name = "Professor in Charge")]
        public string ProfInCharge { get; set; }

        [Required]
        [Column("group_id")]
        [Display(Name = "Group")]
        public int GroupId { get; set; }

        [StringLength(50)]
        [Column("status")]
        public string Status { get; set; } = "pending";

        [Column("CreatedAt")]
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        [Column("reservation_id")]
        public int ReservationId { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual UserAccount User { get; set; }

        [ForeignKey("PeriodId")]
        public virtual Period Period { get; set; }

        [ForeignKey("GroupId")]
        public virtual Group Group { get; set; }

        [ForeignKey("ReservationId")]
        public virtual Reservation Reservation { get; set; }

        // Validation method for status
        public bool IsValidStatus()
        {
            var validStatuses = new[] { "pending", "approved", "denied" };
            return Array.Exists(validStatuses, status =>
                string.Equals(status, Status, StringComparison.OrdinalIgnoreCase));
        }

        // Helper properties
        [NotMapped]
        public bool IsPending => Status?.ToLower() == "pending";

        [NotMapped]
        public bool IsApproved => Status?.ToLower() == "approved";

        [NotMapped]
        public bool IsDenied => Status?.ToLower() == "denied";
    }

    // Enum for better type safety (optional)
    public enum RequestStatus
    {
        Pending,
        Approved,
        Denied
    }
}