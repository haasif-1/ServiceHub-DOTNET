using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServiceHub.Web.Models;

namespace ServiceHub.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProviderService> ProviderServices => Set<ProviderService>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FullName)
                .HasMaxLength(100);
        });

        builder.Entity<ProviderService>(entity =>
        {
            entity.ToTable("ProviderServices", table =>
            {
                table.HasCheckConstraint(
                    "CK_ProviderServices_Price_NonNegative",
                    "`Price` >= 0");
                table.HasCheckConstraint(
                    "CK_ProviderServices_Duration_Positive",
                    "`EstimatedDurationMinutes` > 0");
            });

            entity.HasKey(service => service.Id);
            entity.Property(service => service.ServiceName)
                .HasMaxLength(120)
                .IsRequired();
            entity.Property(service => service.Description)
                .HasMaxLength(2000);
            entity.Property(service => service.Price)
                .HasPrecision(10, 2);
            entity.Property(service => service.IsActive)
                .HasDefaultValue(true);
            entity.Property(service => service.CreatedAtUtc)
                .HasColumnType("datetime(6)");
            entity.Property(service => service.UpdatedAtUtc)
                .HasColumnType("datetime(6)");
            entity.HasIndex(service => new { service.ProviderUserId, service.IsActive });
            entity.HasOne(service => service.Provider)
                .WithMany()
                .HasForeignKey(service => service.ProviderUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
