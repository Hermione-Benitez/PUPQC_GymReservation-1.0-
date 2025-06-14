using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
// using AYOKONA.Models; // Note: This 'using' might be specific to your original setup.

namespace AYOKONA.Entities
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<AdminAccount> AdminAccounts { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Group> Groups { get; set; } // Add this if not already present
        public DbSet<Period> Periods { get; set; } // Add this if not already present
    }
}
