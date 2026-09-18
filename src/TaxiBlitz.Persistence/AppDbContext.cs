using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Domain.Identity;

namespace TaxiBlitz.Persistence
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Tour>               Tours               { get; set; }
        public DbSet<BookingTour>        Bookings            { get; set; }
        public DbSet<Review>             Reviews             { get; set; }
        public DbSet<GeoCoordinateCache> GeoCoordinateCaches { get; set; }
        public DbSet<PersonalizedDrive>  PersonalizedDrives  { get; set; }
        public DbSet<Driver>             Drivers             { get; set; }
        public DbSet<TourPost>           TourPosts           { get; set; }
        public DbSet<TourPostImage>      TourPostImages      { get; set; }
        public DbSet<TourComment>        TourComments        { get; set; }
        public DbSet<FavouriteTour>      Favourites          { get; set; }
        public DbSet<WeatherCache>       WeatherCaches       { get; set; }
        public DbSet<ReferralCode>       ReferralCodes       { get; set; }
        public DbSet<ReferralUsage>      ReferralUsages      { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Prevent multiple cascade paths through TourPost → User and TourComment → User
            builder.Entity<TourPost>()
                .HasOne(p => p.Author)
                .WithMany()
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TourComment>()
                .HasOne(c => c.CommentAuthor)
                .WithMany()
                .HasForeignKey(c => c.CommentAuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TourComment>()
                .HasOne(c => c.TourPost)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.TourPostId)
                .OnDelete(DeleteBehavior.Cascade);

            // Prevent self-referencing cascade on TourComment (parent/child)
            builder.Entity<TourComment>()
                .HasOne(c => c.ParentComment)
                .WithMany()
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Tour>()
                .Property(t => t.Price)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Tour>().Property(t => t.Slug).IsRequired(false);
            builder.Entity<Tour>().HasIndex(t => t.Slug).IsUnique().HasFilter("[Slug] IS NOT NULL");

            // These fields are optional in practice despite being non-nullable reference types
            builder.Entity<Tour>().Property(t => t.YouTubeLink).IsRequired(false);
            builder.Entity<Tour>().Property(t => t.PhotoProfileUrl).IsRequired(false);
            builder.Entity<Tour>().Property(t => t.RouteStopsText).IsRequired(false);
            builder.Entity<Tour>().Property(t => t.CulturalHighlights).IsRequired(false);
            builder.Entity<Tour>().Property(t => t.StartingPoint).IsRequired(false);
            builder.Entity<Tour>().Property(t => t.EndingPoint).IsRequired(false);

            builder.Entity<Review>().Property(r => r.ImageUrl).IsRequired(false);
            builder.Entity<Review>().Property(r => r.ReviewerName).IsRequired(false);
            builder.Entity<Review>().Property(r => r.Text).IsRequired(false);

            builder.Entity<TourPost>().Property(p => p.AuthorId).IsRequired(false);
            builder.Entity<TourPost>().Property(p => p.CoverImage).IsRequired(false);
            builder.Entity<TourPost>().Property(p => p.Slug).IsRequired(false);
            builder.Entity<TourPost>().Property(p => p.Tags).IsRequired(false);
            builder.Entity<TourPost>().Property(p => p.TourDestination).IsRequired(false);

            builder.Entity<TourComment>().Property(c => c.CommentAuthorId).IsRequired(false);

            builder.Entity<Driver>().Property(d => d.Email).IsRequired(false);
            builder.Entity<Driver>().Property(d => d.Bio).IsRequired(false);
            builder.Entity<Driver>().Property(d => d.PhotoProfileUrl).IsRequired(false);
            builder.Entity<Driver>().Property(d => d.Languages).IsRequired(false);
            builder.Entity<Driver>().Property(d => d.VehicleType).IsRequired(false);
            builder.Entity<Driver>().Property(d => d.CoverageAreas).IsRequired(false);
            builder.Entity<Driver>().Property(d => d.Specialties).IsRequired(false);
            builder.Entity<Driver>().PrimitiveCollection(d => d.PictureUrls).IsRequired(false);

            builder.Entity<PersonalizedDrive>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            // FavouriteTour — cascade delete from both User and Tour
            builder.Entity<FavouriteTour>()
                .HasOne(f => f.User).WithMany().HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<FavouriteTour>()
                .HasOne(f => f.Tour).WithMany().HasForeignKey(f => f.TourId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<FavouriteTour>()
                .HasIndex(f => new { f.UserId, f.TourId }).IsUnique();

            // ReferralCode
            builder.Entity<ReferralCode>()
                .HasOne(r => r.Owner).WithMany().HasForeignKey(r => r.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<ReferralCode>()
                .Property(r => r.DiscountPercent).HasColumnType("decimal(5,2)");
            builder.Entity<ReferralCode>()
                .HasIndex(r => r.Code).IsUnique();

            // ReferralUsage
            builder.Entity<ReferralUsage>()
                .HasOne(u => u.ReferralCode).WithMany(r => r.Usages).HasForeignKey(u => u.ReferralCodeId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ReferralUsage>()
                .HasOne(u => u.UsedByUser).WithMany().HasForeignKey(u => u.UsedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<ReferralUsage>()
                .HasOne(u => u.Booking).WithMany().HasForeignKey(u => u.BookingId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            builder.Entity<ReferralUsage>()
                .Property(u => u.DiscountAmount).HasColumnType("decimal(18,2)");
            builder.Entity<ReferralUsage>()
                .HasIndex(u => new { u.ReferralCodeId, u.UsedByUserId }).IsUnique();

            // BookingTour — discount amount and optional referral FK
            builder.Entity<BookingTour>()
                .Property(b => b.DiscountAmount).HasColumnType("decimal(18,2)");
            builder.Entity<BookingTour>()
                .HasOne(b => b.ReferralCode).WithMany().HasForeignKey(b => b.ReferralCodeId)
                .IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        }
    }
}
