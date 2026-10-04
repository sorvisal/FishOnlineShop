using FishOnlineShop.Models;
using Microsoft.EntityFrameworkCore;

namespace FishOnlineShop.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories => Set<Category>();

        public DbSet<FishProduct> FishProducts => Set<FishProduct>();

        public DbSet<User> Users => Set<User>();

        public DbSet<Order> Orders => Set<Order>();

        public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Categories");

                entity.HasKey(c => c.CategoryId);

                entity.Property(c => c.CategoryId)
                      .ValueGeneratedOnAdd();

                entity.Property(c => c.CategoryName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasIndex(c => c.CategoryName)
                      .IsUnique()
                      .HasDatabaseName("IX_Categories_CategoryName");

                entity.Property(c => c.Description)
                      .HasMaxLength(255);

                entity.Property(c => c.CreatedAt)
                      .IsRequired()
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.HasMany(c => c.FishProducts)
                      .WithOne(p => p.Category)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FishProduct>(entity =>
            {
                entity.ToTable("FishProducts");

                entity.HasKey(p => p.ProductId);

                entity.Property(p => p.ProductId)
                      .ValueGeneratedOnAdd();

                entity.Property(p => p.CategoryId)
                      .IsRequired();

                entity.Property(p => p.FishName)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(p => p.Price)
                      .IsRequired()
                      .HasColumnType("decimal(10,2)");

                entity.Property(p => p.Stock)
                      .IsRequired();

                entity.Property(p => p.Weight)
                      .HasColumnType("decimal(6,2)");

                entity.Property(p => p.ImageUrl)
                      .HasMaxLength(255);

                entity.Property(p => p.Description)
                      .HasColumnType("nvarchar(max)");

                entity.Property(p => p.CreatedAt)
                      .IsRequired()
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(p => p.CategoryId);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");

                entity.HasKey(u => u.UserId);

                entity.Property(u => u.UserId)
                      .ValueGeneratedOnAdd();

                entity.Property(u => u.FullName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(u => u.Email)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasIndex(u => u.Email)
                      .IsUnique()
                      .HasDatabaseName("IX_Users_Email");

                entity.Property(u => u.PasswordHash)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(u => u.Role)
                      .IsRequired()
                      .HasMaxLength(20)
                      .HasDefaultValue(UserRoles.Customer);

                entity.Property(u => u.Phone)
                      .HasMaxLength(20);

                entity.Property(u => u.Address)
                      .HasMaxLength(255);

                entity.Property(u => u.CreatedAt)
                      .IsRequired()
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.HasMany(u => u.Orders)
                      .WithOne(o => o.User)
                      .HasForeignKey(o => o.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");

                entity.HasKey(o => o.OrderId);

                entity.Property(o => o.OrderId)
                      .ValueGeneratedOnAdd();

                entity.Property(o => o.UserId)
                      .IsRequired();

                entity.Property(o => o.ReceiverName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(o => o.Phone)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(o => o.ShippingAddress)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(o => o.PaymentMethod)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(o => o.OrderDate)
                      .IsRequired()
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(o => o.TotalAmount)
                      .IsRequired()
                      .HasColumnType("decimal(10,2)");

                entity.Property(o => o.OrderStatus)
                      .IsRequired()
                      .HasMaxLength(30)
                      .HasDefaultValue("Pending");

                entity.HasIndex(o => o.UserId);
                entity.HasIndex(o => o.OrderDate);

                entity.HasMany(o => o.OrderDetails)
                      .WithOne(d => d.Order)
                      .HasForeignKey(d => d.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderDetail>(entity =>
            {
                entity.ToTable("OrderDetails");

                entity.HasKey(d => d.OrderDetailId);

                entity.Property(d => d.OrderDetailId)
                      .ValueGeneratedOnAdd();

                entity.Property(d => d.OrderId)
                      .IsRequired();

                entity.Property(d => d.ProductId)
                      .IsRequired();

                entity.Property(d => d.Quantity)
                      .IsRequired();

                entity.Property(d => d.UnitPrice)
                      .IsRequired()
                      .HasColumnType("decimal(10,2)");

                entity.Property(d => d.SubTotal)
                      .IsRequired()
                      .HasColumnType("decimal(10,2)");

                entity.HasIndex(d => d.OrderId);
                entity.HasIndex(d => d.ProductId);

                entity.HasOne(d => d.Product)
                      .WithMany(p => p.OrderDetails)
                      .HasForeignKey(d => d.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            SeedCategories(modelBuilder);
        }

        private static void SeedCategories(ModelBuilder modelBuilder)
        {
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    CategoryId = 1,
                    CategoryName = "Aquarium",
                    Description = "Aquariums, tanks and aquarium furniture.",
                    CreatedAt = now
                },
                new Category
                {
                    CategoryId = 2,
                    CategoryName = "Aquarium Fish",
                    Description = "Live freshwater and saltwater fish.",
                    CreatedAt = now
                },
                new Category
                {
                    CategoryId = 3,
                    CategoryName = "Fish Food",
                    Description = "Fish food, flakes, pellets and supplements.",
                    CreatedAt = now
                }
            );
        }
    }
}
