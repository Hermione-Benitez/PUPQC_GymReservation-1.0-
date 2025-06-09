namespace AYOKONA.Models
{
    public class StudentProfileViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string StudentNumber { get; set; }
        public string Section { get; set; }
        public DateTime DateJoined { get; set; }
        public string Campus { get; set; }
        public string Role { get; set; }
        public List<OrganizationViewModel> Organizations { get; set; }
    }

    public class OrganizationViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public int JoinYear { get; set; }
        public string Status { get; set; }
    }
}
