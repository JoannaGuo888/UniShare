using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using UniShare.Models;

namespace UniShare.Data
{
    public class UniShareDbContext: DbContext
    {
        public UniShareDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Ride> Rides { get; set; }
        public DbSet <RideRequest> RideRequests { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<SystemAuditLog> systemAuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Disable cascade delete for EVERY relationship in the database
            foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }

            modelBuilder.Entity<RideRequest>()
                .HasOne(rr => rr.Driver)
                .WithMany() // or .WithMany(u => u.DriverRideRequests)
                .HasForeignKey(rr => rr.DriverId)
                .OnDelete(DeleteBehavior.Restrict); // NO CASCADE

            modelBuilder.Entity<RideRequest>()
                .HasOne(rr => rr.Passenger)
                .WithMany() // or .WithMany(u => u.RiderRideRequests)
                .HasForeignKey(rr => rr.PassengerId)
                .OnDelete(DeleteBehavior.Restrict); // Keep cascade on one side

            modelBuilder.Entity<RideRequest>()
                .HasOne(rr=>rr.Ride)
                .WithMany()
                .HasForeignKey(rr=>rr.RideId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ride>()
                .HasOne(rr => rr.Driver)
                .WithMany()
                .HasForeignKey(rr => rr.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Report model has NO navigation property
            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(r => r.ReporterUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(r => r.SubjectUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

        }



    }
}
