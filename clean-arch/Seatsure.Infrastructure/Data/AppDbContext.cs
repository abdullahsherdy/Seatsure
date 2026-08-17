using Microsoft.EntityFrameworkCore;
using Seatsure.Domain;

namespace Seatsure.Infrastructure.Data;

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // all classes exit in the same file or wrking dir. 

        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<TicketType> TicketTypes { get; set; }
        public DbSet<Reservation> Reservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // user configurations

            modelBuilder.Entity<User>(u =>
            {
                u.HasKey(x => x.Id);
                u.HasIndex(x => x.Email).IsUnique();
                u.Property(x => x.Name).HasMaxLength(50).IsRequired();
                u.Property(x => x.Email).IsRequired();
                u.Property(x => x.PasswordHash).IsRequired();
            });


            modelBuilder.Entity<Event>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).HasMaxLength(50).IsRequired();
                e.Property(x => x.Description).HasMaxLength(500).IsRequired();
                e.HasOne(x => x.Organizer)
                    .WithMany()
                    .HasForeignKey(x => x.OrganizerId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasMany(x => x.TicketTypes)
                    .WithOne(t => t.Event)
                    .HasForeignKey(t => t.EventId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TicketType>(t =>
            {
                t.HasKey(x => x.Id);
                t.Property(x => x.Name).HasMaxLength(50).IsRequired();
                // concurrency token — EF includes RowVersion in WHERE on every UPDATE 
                t.Property(x => x.RowVersion).IsRowVersion();
                
            });

            modelBuilder.Entity<Reservation>(r =>
            {
                r.HasKey(x => x.Id);

                r.HasOne(x => x.TicketType)
                    .WithMany(t => t.Reservations)
                    .HasForeignKey(x => x.TicketTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                r.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
