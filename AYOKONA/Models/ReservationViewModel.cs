namespace AYOKONA.Models
{
    public class ReservationFormViewModel
    {
        public string Fullname { get; set; }
        public string Section { get; set; }
        public string Category { get; set; }
        public string ProfessorInCharge { get; set; }
        public string PurposeOfUse { get; set; }
        public DateTime ReservationDate { get; set; }
        public string ReservationTime { get; set; }
    }
}
