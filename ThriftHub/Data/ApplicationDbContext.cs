using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Models;

namespace ThriftHub.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


        // ============================================================
        // DATABASE TABLES
        // ============================================================

        public DbSet<Product> Products { get; set; }

        public DbSet<Seller> Sellers { get; set; }

        public DbSet<Message> Messages { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<SellerSubscription> SellerSubscriptions { get; set; }

        public DbSet<Notification> Notifications { get; set; }


        // ============================================================
        // WISHLIST / FAVORITES
        // ============================================================

        public DbSet<Favorite> Favorites { get; set; }

        public DbSet<ProductView> ProductViews { get; set; }


        // ============================================================
        // SAFETY
        // ============================================================

        public DbSet<BlockedUser> BlockedUsers { get; set; }

        public DbSet<Report> Reports { get; set; }


        // ============================================================
        // DATABASE CONFIGURATION
        // ============================================================

        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            // IMPORTANT:
            // Keep this so ASP.NET Core Identity continues working.

            base.OnModelCreating(builder);


            // ========================================================
            // APPLICATION USER
            // ========================================================

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.IdCardType)
                    .HasMaxLength(50);

                entity.Property(u => u.IdCardNumber)
                    .HasMaxLength(100);

                entity.Property(u => u.IdCardFrontUrl)
                    .HasMaxLength(500);

                entity.Property(u => u.IdCardBackUrl)
                    .HasMaxLength(500);

                entity.Property(u => u.IdCardVerified)
                    .IsRequired();

                entity.Property(u => u.IdCardVerificationStatus)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(u => u.IdCardArchiveFrontUrl)
                    .HasMaxLength(500);

                entity.Property(u => u.IdCardArchiveBackUrl)
                    .HasMaxLength(500);

                entity.Property(u => u.UserType)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(u => u.VerificationStatus)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(u => u.FullName)
                    .HasMaxLength(150);

                entity.Property(u => u.Country)
                    .HasMaxLength(100);

                entity.Property(u => u.City)
                    .HasMaxLength(100);

                entity.Property(u => u.ProfileImageUrl)
                    .HasMaxLength(500);

                entity.Property(u => u.InstagramUrl)
                    .HasMaxLength(500);

                entity.Property(u => u.TikTokUrl)
                    .HasMaxLength(500);

                entity.Property(u => u.FacebookUrl)
                    .HasMaxLength(500);

                entity.Property(u => u.XUrl)
                    .HasMaxLength(500);

                entity.Property(u => u.WhatsAppUrl)
                    .HasMaxLength(500);

                entity.Property(u => u.YouTubeUrl)
                    .HasMaxLength(500);

                entity.Property(u => u.WebsiteUrl)
                    .HasMaxLength(500);

                entity.Property(u => u.EmailVerificationCode)
                    .HasMaxLength(100);

                entity.Property(u => u.SuspensionReason)
                    .HasMaxLength(1000);
            });


            // ========================================================
            // PRODUCT
            // ========================================================

            builder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Price)
                    .HasPrecision(18, 2);

                entity.Property(p => p.SellerId)
                    .IsRequired();

                entity.Property(p => p.Name)
                    .IsRequired();

                entity.Property(p => p.Category)
                    .IsRequired();

                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(p => p.SellerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(p => p.ViewCount)
                    .HasDefaultValue(0);
            });


            // ========================================================
            // PRODUCT VIEWS
            // ========================================================

            builder.Entity<ProductView>(entity =>
            {
                entity.HasKey(view => view.Id);

                entity.Property(view => view.ViewerKey)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(view => view.FirstViewedAt)
                    .IsRequired();

                entity.HasIndex(view => new
                {
                    view.ProductId,
                    view.ViewerKey
                })
                .IsUnique();

                entity.HasOne<Product>()
                    .WithMany()
                    .HasForeignKey(view => view.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            // ========================================================
            // SELLER
            // ========================================================

            builder.Entity<Seller>(entity =>
            {
                entity.HasKey(s => s.Id);
            });


            // ========================================================
            // MESSAGE
            // ========================================================

            builder.Entity<Message>(entity =>
            {
                entity.HasKey(m => m.Id);
            });


            // ========================================================
            // ORDER
            // ========================================================

            builder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);

                entity.Property(o => o.ProductPrice)
                    .HasPrecision(18, 2);

                entity.Property(o => o.CommissionPercentage)
                    .HasPrecision(5, 2);

                entity.Property(o => o.CommissionAmount)
                    .HasPrecision(18, 2);

                entity.Property(o => o.SellerAmount)
                    .HasPrecision(18, 2);

                entity.Property(o => o.TotalAmount)
                    .HasPrecision(18, 2);
            });


            // ========================================================
            // SELLER SUBSCRIPTION
            // ========================================================

            builder.Entity<SellerSubscription>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.Property(s => s.SellerId)
                    .IsRequired();

                entity.Property(s => s.PlanName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(s => s.Amount)
                    .HasPrecision(18, 2);

                entity.Property(s => s.DurationMonths)
                    .IsRequired();

                entity.Property(s => s.Status)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(s => s.PaymentStatus)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(s => s.StartDate)
                    .IsRequired();

                entity.Property(s => s.EndDate)
                    .IsRequired();

                entity.Property(s => s.CreatedAt)
                    .IsRequired();
            });


            // ========================================================
            // NOTIFICATION
            // ========================================================

            builder.Entity<Notification>(entity =>
            {
                entity.HasKey(n => n.Id);

                entity.Property(n => n.UserId)
                    .IsRequired();

                entity.Property(n => n.Message)
                    .IsRequired();

                entity.Property(n => n.Link)
                    .HasMaxLength(500);

                entity.Property(n => n.IsRead)
                    .IsRequired();

                entity.Property(n => n.CreatedAt)
                    .IsRequired();
            });


            // ========================================================
            // FAVORITES / WISHLIST
            // ========================================================

            builder.Entity<Favorite>(entity =>
            {
                // Primary key
                entity.HasKey(f => f.Id);


                // User who added the product
                entity.Property(f => f.UserId)
                    .IsRequired();


                // Product that was added
                entity.Property(f => f.ProductId)
                    .IsRequired();


                // Date added
                entity.Property(f => f.CreatedAt)
                    .IsRequired();


                // ====================================================
                // PREVENT DUPLICATE WISHLIST ITEMS
                // ====================================================

                entity.HasIndex(f => new
                {
                    f.UserId,
                    f.ProductId
                })
                .IsUnique();


                // ====================================================
                // FAVORITE → PRODUCT
                // ====================================================

                entity.HasOne<Product>()
                    .WithMany()
                    .HasForeignKey(f => f.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            // ========================================================
            // BLOCKED USER
            // ========================================================

            builder.Entity<BlockedUser>(entity =>
            {
                entity.HasKey(b => b.Id);

                entity.Property(b => b.BlockerId)
                    .IsRequired();

                entity.Property(b => b.BlockedUserId)
                    .IsRequired();

                entity.Property(b => b.CreatedAt)
                    .IsRequired();


                entity.HasIndex(b => new
                {
                    b.BlockerId,
                    b.BlockedUserId
                })
                .IsUnique();
            });


            // ========================================================
            // REPORT
            // ========================================================

            builder.Entity<Report>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.ReporterId)
                    .IsRequired();

                entity.Property(r => r.Reason)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(r => r.Description)
                    .HasMaxLength(1000);

                entity.Property(r => r.Status)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(r => r.AdminResponse)
                    .HasMaxLength(1000);

                entity.Property(r => r.CreatedAt)
                    .IsRequired();
            });
        }
    }
}