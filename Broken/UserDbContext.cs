using BrokenCode.Model;
using Microsoft.EntityFrameworkCore;

namespace BrokenCode
{
    public class UserDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies();
        }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<User>()
                .HasIndex(u => u.UserEmail).IsUnique();

            mb.Entity<User>()
                .HasOne(e => e.Email)
                .WithOne(t => t.User)
                .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<User>()
                .HasOne(e => e.Drive)
                .WithOne(t => t.User)
                .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<User>()
                .HasOne(e => e.Calendar)
                .WithOne(t => t.User)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(mb);
        }

        public virtual DbSet<User> Users { get; set; }

        public virtual DbSet<Email> Emails { get; set; }

        public virtual DbSet<Drive> Drives { get; set; }

        public virtual DbSet<Calendar> Calendars { get; set; }
    }
}
