using Microsoft.EntityFrameworkCore;
using ResortBookingMVC.Models;
using ResortBookingMVC.Models.Enums;

namespace ResortBookingMVC.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Location> Locations { get; set; } = null!;
        public DbSet<Resort> Resorts { get; set; } = null!;
        public DbSet<ResortImage> ResortImages { get; set; } = null!;
        public DbSet<RoomType> RoomTypes { get; set; } = null!;
        public DbSet<Room> Rooms { get; set; } = null!;
        public DbSet<RoomImage> RoomImages { get; set; } = null!;
        public DbSet<PricingRule> PricingRules { get; set; } = null!;
        public DbSet<Service> Services { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;
        public DbSet<BookingRoom> BookingRooms { get; set; } = null!;
        public DbSet<BookingService> BookingServices { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<CancellationPolicy> CancellationPolicies { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>().HasMaxLength(20);

            // Resort
            modelBuilder.Entity<Resort>()
                .HasIndex(r => r.Slug).IsUnique();
            modelBuilder.Entity<Resort>()
                .HasOne(r => r.Location)
                .WithMany(l => l.Resorts)
                .HasForeignKey(r => r.LocationId);

            // ResortImage
            modelBuilder.Entity<ResortImage>()
                .HasOne(i => i.Resort)
                .WithMany(r => r.Images)
                .HasForeignKey(i => i.ResortId);

            // RoomType
            modelBuilder.Entity<RoomType>()
                .HasOne(rt => rt.Resort)
                .WithMany(r => r.RoomTypes)
                .HasForeignKey(rt => rt.ResortId);

            // Room
            modelBuilder.Entity<Room>()
                .HasOne(r => r.RoomType)
                .WithMany(rt => rt.Rooms)
                .HasForeignKey(r => r.RoomTypeId);
            modelBuilder.Entity<Room>()
                .Property(r => r.Status)
                .HasConversion<string>().HasMaxLength(20);

            // Booking
            modelBuilder.Entity<Booking>(b =>
            {
                b.HasIndex(x => x.BookingCode).IsUnique();
                b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                b.HasOne(x => x.User).WithMany(u => u.Bookings).HasForeignKey(x => x.UserId);
                b.HasOne(x => x.Resort).WithMany(r => r.Bookings).HasForeignKey(x => x.ResortId);
            });

            // BookingRoom
            modelBuilder.Entity<BookingRoom>(br =>
            {
                br.HasOne(x => x.Booking).WithMany(b => b.BookingRooms)
                    .HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
                br.HasOne(x => x.Room).WithMany(r => r.BookingRooms)
                    .HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
                br.HasOne(x => x.RoomType).WithMany()
                    .HasForeignKey(x => x.RoomTypeId).OnDelete(DeleteBehavior.Restrict);
            });

            // BookingService
            modelBuilder.Entity<BookingService>(bs =>
            {
                bs.HasOne(x => x.Booking).WithMany(b => b.BookingServices)
                    .HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
                bs.HasOne(x => x.Service).WithMany(s => s.BookingServices)
                    .HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
            });

            // Payment
            modelBuilder.Entity<Payment>(p =>
            {
                p.HasIndex(x => x.TransactionCode).IsUnique();
                p.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(20);
                p.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                p.HasOne(x => x.Booking).WithMany(b => b.Payments)
                    .HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
            });

            // Review
            modelBuilder.Entity<Review>(r =>
            {
                r.HasOne(x => x.Booking).WithOne(b => b.Review)
                    .HasForeignKey<Review>(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
                r.HasOne(x => x.User).WithMany(u => u.Reviews)
                    .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
                r.HasOne(x => x.Resort).WithMany(res => res.Reviews)
                    .HasForeignKey(x => x.ResortId).OnDelete(DeleteBehavior.Restrict);
            });

            // Service
            modelBuilder.Entity<Service>()
                .HasOne(s => s.Resort).WithMany(r => r.Services)
                .HasForeignKey(s => s.ResortId);

            // CancellationPolicy
            modelBuilder.Entity<CancellationPolicy>()
                .HasOne(c => c.Resort).WithMany(r => r.CancellationPolicies)
                .HasForeignKey(c => c.ResortId);
        }
    }
}
