using Charter.ReporterApp.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Charter.ReporterApp.Infrastructure.Data;

/// <summary>
/// Main application database context
/// </summary>
public class AppDbContext : IdentityDbContext<User, Role, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<RegistrationRequest> RegistrationRequests { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure User entity
        builder.Entity<User>(entity =>
        {
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Organization)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.IdNumber)
                .HasMaxLength(13)
                .IsRequired();

            entity.Property(e => e.Address)
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.IdNumber)
                .IsUnique();

            entity.HasMany(e => e.AuditLogs)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Role entity
        builder.Entity<Role>(entity =>
        {
            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure RegistrationRequest entity
        builder.Entity<RegistrationRequest>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Organization)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.RequestedRole)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.IdNumber)
                .HasMaxLength(13)
                .IsRequired();

            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.Address)
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(e => e.RejectionReason)
                .HasMaxLength(1000);

            entity.Property(e => e.ApprovedBy)
                .HasMaxLength(255);

            entity.Property(e => e.RejectedBy)
                .HasMaxLength(255);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.HasIndex(e => e.IdNumber)
                .IsUnique();

            entity.HasIndex(e => e.Status);
        });

        // Configure AuditLog entity
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .HasMaxLength(450);

            entity.Property(e => e.UserName)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.EntityType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.EntityId)
                .HasMaxLength(50);

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .IsRequired();

            entity.Property(e => e.UserAgent)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EventType);
        });

        // Seed roles
        SeedRoles(builder);
    }

    private static void SeedRoles(ModelBuilder builder)
    {
        var roles = new[]
        {
            new Role
            {
                Id = "1",
                Name = "Charter-Admin",
                NormalizedName = "CHARTER-ADMIN",
                Description = "Charter Administrator with full system access",
                CreatedAt = DateTime.UtcNow
            },
            new Role
            {
                Id = "2",
                Name = "Rebosa-Admin",
                NormalizedName = "REBOSA-ADMIN",
                Description = "Rebosa Administrator with organization-specific access",
                CreatedAt = DateTime.UtcNow
            },
            new Role
            {
                Id = "3",
                Name = "PPRA-Admin",
                NormalizedName = "PPRA-ADMIN",
                Description = "PPRA Administrator with regulatory access",
                CreatedAt = DateTime.UtcNow
            }
        };

        builder.Entity<Role>().HasData(roles);
    }
}