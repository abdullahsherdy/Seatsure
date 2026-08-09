
using Microsoft.EntityFrameworkCore;
using Seatsure.Domain;

namespace Seatsure.DAL
{

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {


        }

        public DbSet<User> Users { get; set; }

        public DbSet<Event> Events { get; set; }

        public DbSet<TicketType> TicketTypes { get; set; }

        public DbSet<Reservation> Reservations { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {


            base.OnModelCreating(modelBuilder);

            //modelBuilder.Entity<User>().
            //    HasKey(u => u.id);

            //modelBuilder.Entity<User>().
            //    HasIndex(u => u.email);


            //modelBuilder.Entity<Event>().
            //    HasKey(e => e.id);
            //modelBuilder.Entity<Event>()
            //    .HasOne(e => e.Organizer);
            //modelBuilder.Entity<Event>()
            //    .HasMany(e => e.Tickets)
            //    .WithOne(t => t.Event);


            //modelBuilder.Entity<Reservation>().
            //    HasKey(r => r.id);

            //modelBuilder.Entity<Reservation>().
            //    HasOne(r => r.ticket);
            //modelBuilder.Entity<Reservation>().
            //    HasOne(r => r.user);

            //modelBuilder.Entity<TicketType>().
            //    HasOne(t => t.Event);

            // user entity 

            modelBuilder.Entity<User>(u =>
            {
                u.HasKey( u => u.id);
                u.HasIndex(u => u.email).IsUnique();

                u.Property(u => u.name).HasMaxLength(25).IsRequired();

                u.Property(u => u.PasswordHash).IsRequired();
            });

            // Event 
            // HAskey, Foregin key
            // index 
            // nvarchar(max) -> MaxLength(500)

            // organizer 
            
            modelBuilder.Entity<Event>(e =>
            {
                // dashboard, organizer, Admin 
                // id, Title, 
                // dashboard-> fetch every insert( trigger) 
                e.HasKey(e => e.id);

                e.Property(e => e.Title).HasMaxLength(50).IsRequired();

                e.Property(e => e.Description).HasMaxLength(500).IsRequired();


                e.HasOne(e => e.Organizer)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // when organizer entity deleted, set orgranizerId to null

                e.HasMany(e => e.Tickets)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            });


            // Ticket
            modelBuilder.Entity<TicketType>(t =>
            {
                // index, key
                t.HasKey(t => t.id);

                t.Property(t => t.Title).HasMaxLength(50).IsRequired();

                t.Property(t => t.RowVersion).IsRowVersion();
            });


            // Reservation 

            modelBuilder.Entity<Reservation>(r => {
                r.HasKey(r => r.id);

                r.HasOne(r => r.ticket)
                .WithMany(t => t.Reservations)
                .HasForeignKey(r => r.TicketTypeId)
                .OnDelete(DeleteBehavior.Restrict);


                r.HasOne(r => r.user)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            });

            // to get user reservations, join 
            // inner join 

        }

    }
}