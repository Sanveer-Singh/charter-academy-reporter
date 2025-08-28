using Microsoft.EntityFrameworkCore;

namespace Charter.ReporterApp.Infrastructure.Data;

/// <summary>
/// WooCommerce database context for external data access
/// </summary>
public class WooCommerceDbContext : DbContext
{
    public WooCommerceDbContext(DbContextOptions<WooCommerceDbContext> options) : base(options)
    {
    }

    public DbSet<WooOrder> Orders { get; set; }
    public DbSet<WooOrderItem> OrderItems { get; set; }
    public DbSet<WooProduct> Products { get; set; }
    public DbSet<WooCustomer> Customers { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure WooCommerce entities (read-only)
        builder.Entity<WooOrder>(entity =>
        {
            entity.ToTable("wp_wc_orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Currency).HasColumnName("currency");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount").HasColumnType("decimal(10,2)");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.BillingEmail).HasColumnName("billing_email");
            entity.Property(e => e.DateCreated).HasColumnName("date_created_gmt");
            entity.Property(e => e.DateModified).HasColumnName("date_updated_gmt");
        });

        builder.Entity<WooOrderItem>(entity =>
        {
            entity.ToTable("wp_woocommerce_order_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("order_item_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderItemName).HasColumnName("order_item_name");
            entity.Property(e => e.OrderItemType).HasColumnName("order_item_type");
        });

        builder.Entity<WooProduct>(entity =>
        {
            entity.ToTable("wp_posts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.PostTitle).HasColumnName("post_title");
            entity.Property(e => e.PostName).HasColumnName("post_name");
            entity.Property(e => e.PostStatus).HasColumnName("post_status");
            entity.Property(e => e.PostType).HasColumnName("post_type");
            entity.Property(e => e.PostDate).HasColumnName("post_date_gmt");
            entity.Property(e => e.PostModified).HasColumnName("post_modified_gmt");
        });

        builder.Entity<WooCustomer>(entity =>
        {
            entity.ToTable("wp_users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.UserLogin).HasColumnName("user_login");
            entity.Property(e => e.UserEmail).HasColumnName("user_email");
            entity.Property(e => e.UserRegistered).HasColumnName("user_registered");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
        });

        // Set up relationships
        builder.Entity<WooOrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId);

        builder.Entity<WooOrder>()
            .HasOne(o => o.Customer)
            .WithMany()
            .HasForeignKey(o => o.CustomerId);
    }
}

/// <summary>
/// WooCommerce order entity (read-only)
/// </summary>
public class WooOrder
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int? CustomerId { get; set; }
    public string BillingEmail { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
    
    public virtual WooCustomer? Customer { get; set; }
    public virtual ICollection<WooOrderItem> OrderItems { get; set; } = new List<WooOrderItem>();
}

/// <summary>
/// WooCommerce order item entity (read-only)
/// </summary>
public class WooOrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string OrderItemName { get; set; } = string.Empty;
    public string OrderItemType { get; set; } = string.Empty;
    
    public virtual WooOrder? Order { get; set; }
}

/// <summary>
/// WooCommerce product entity (read-only)
/// </summary>
public class WooProduct
{
    public int Id { get; set; }
    public string PostTitle { get; set; } = string.Empty;
    public string PostName { get; set; } = string.Empty;
    public string PostStatus { get; set; } = string.Empty;
    public string PostType { get; set; } = string.Empty;
    public DateTime PostDate { get; set; }
    public DateTime PostModified { get; set; }
}

/// <summary>
/// WooCommerce customer entity (read-only)
/// </summary>
public class WooCustomer
{
    public int Id { get; set; }
    public string UserLogin { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public DateTime UserRegistered { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}