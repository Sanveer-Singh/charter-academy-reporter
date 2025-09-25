using Charter.Reporter.Infrastructure.Identity;
using Charter.Reporter.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Charter.Reporter.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ExportLog> ExportLogs => Set<ExportLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApprovalRequest>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Email).IsRequired();
            b.Property(x => x.RequestedRole).IsRequired();
            b.Property(x => x.Status).IsRequired();
            b.HasIndex(x => new { x.Email, x.Status });
        });

        builder.Entity<ExportLog>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.RequestedByUserId);
        });

        builder.Entity<AuditLog>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.CreatedUtc);
        });
    }
}


