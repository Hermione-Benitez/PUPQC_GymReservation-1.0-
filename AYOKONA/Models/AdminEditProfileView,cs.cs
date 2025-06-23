using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AYOKONA.Models
{
    public class AdminEditProfileView
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Department { get; set; } = "Physical Education";
        public string Position { get; set; } = "Gym Administrator";
    }
}
