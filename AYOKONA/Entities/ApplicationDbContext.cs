using Microsoft.EntityFrameworkCore;
using AYOKONA.Models;

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
        public DbSet<AYOKONA.Models.AdminLoginViewModel> AdminLoginViewModel { get; set; } = default!;
        public DbSet<AYOKONA.Models.StudentProfileViewModel> StudentProfileViewModel { get; set; } = default!;
    }
}