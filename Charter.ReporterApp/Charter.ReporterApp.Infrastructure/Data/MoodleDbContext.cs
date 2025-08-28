using Microsoft.EntityFrameworkCore;

namespace Charter.ReporterApp.Infrastructure.Data;

/// <summary>
/// Moodle database context for external data access
/// </summary>
public class MoodleDbContext : DbContext
{
    public MoodleDbContext(DbContextOptions<MoodleDbContext> options) : base(options)
    {
    }

    public DbSet<MoodleUser> Users { get; set; }
    public DbSet<MoodleCourse> Courses { get; set; }
    public DbSet<MoodleEnrollment> Enrollments { get; set; }
    public DbSet<MoodleCompletion> Completions { get; set; }
    public DbSet<MoodleCategory> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Moodle entities (read-only)
        builder.Entity<MoodleUser>(entity =>
        {
            entity.ToTable("mdl_user");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Username).HasColumnName("username");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.FirstName).HasColumnName("firstname");
            entity.Property(e => e.LastName).HasColumnName("lastname");
            entity.Property(e => e.TimeCreated).HasColumnName("timecreated");
            entity.Property(e => e.TimeModified).HasColumnName("timemodified");
        });

        builder.Entity<MoodleCourse>(entity =>
        {
            entity.ToTable("mdl_course");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ShortName).HasColumnName("shortname");
            entity.Property(e => e.FullName).HasColumnName("fullname");
            entity.Property(e => e.CategoryId).HasColumnName("category");
            entity.Property(e => e.TimeCreated).HasColumnName("timecreated");
            entity.Property(e => e.TimeModified).HasColumnName("timemodified");
            entity.Property(e => e.Visible).HasColumnName("visible");
        });

        builder.Entity<MoodleCategory>(entity =>
        {
            entity.ToTable("mdl_course_categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Parent).HasColumnName("parent");
            entity.Property(e => e.SortOrder).HasColumnName("sortorder");
            entity.Property(e => e.TimeCreated).HasColumnName("timecreated");
            entity.Property(e => e.TimeModified).HasColumnName("timemodified");
        });

        builder.Entity<MoodleEnrollment>(entity =>
        {
            entity.ToTable("mdl_user_enrolments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.Property(e => e.EnrolId).HasColumnName("enrolid");
            entity.Property(e => e.TimeStart).HasColumnName("timestart");
            entity.Property(e => e.TimeEnd).HasColumnName("timeend");
            entity.Property(e => e.TimeCreated).HasColumnName("timecreated");
            entity.Property(e => e.TimeModified).HasColumnName("timemodified");
            entity.Property(e => e.Status).HasColumnName("status");
        });

        builder.Entity<MoodleCompletion>(entity =>
        {
            entity.ToTable("mdl_course_completions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("userid");
            entity.Property(e => e.CourseId).HasColumnName("course");
            entity.Property(e => e.TimeCompleted).HasColumnName("timecompleted");
            entity.Property(e => e.TimeStarted).HasColumnName("timestarted");
        });
    }
}

/// <summary>
/// Moodle user entity (read-only)
/// </summary>
public class MoodleUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public long TimeCreated { get; set; }
    public long TimeModified { get; set; }
}

/// <summary>
/// Moodle course entity (read-only)
/// </summary>
public class MoodleCourse
{
    public int Id { get; set; }
    public string ShortName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public long TimeCreated { get; set; }
    public long TimeModified { get; set; }
    public int Visible { get; set; }
    
    public virtual MoodleCategory? Category { get; set; }
}

/// <summary>
/// Moodle course category entity (read-only)
/// </summary>
public class MoodleCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Parent { get; set; }
    public int SortOrder { get; set; }
    public long TimeCreated { get; set; }
    public long TimeModified { get; set; }
}

/// <summary>
/// Moodle enrollment entity (read-only)
/// </summary>
public class MoodleEnrollment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int EnrolId { get; set; }
    public long TimeStart { get; set; }
    public long TimeEnd { get; set; }
    public long TimeCreated { get; set; }
    public long TimeModified { get; set; }
    public int Status { get; set; }
    
    public virtual MoodleUser? User { get; set; }
}

/// <summary>
/// Moodle course completion entity (read-only)
/// </summary>
public class MoodleCompletion
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CourseId { get; set; }
    public long? TimeCompleted { get; set; }
    public long? TimeStarted { get; set; }
    
    public virtual MoodleUser? User { get; set; }
    public virtual MoodleCourse? Course { get; set; }
}