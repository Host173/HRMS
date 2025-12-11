using Microsoft.EntityFrameworkCore;
using HRMS.Models;

namespace HRMS.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Employee entity
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employee");
            entity.HasKey(e => e.EmployeeId);
            entity.Property(e => e.EmployeeId)
                .HasColumnName("employee_id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(100);

            entity.Property(e => e.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(201);

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(200);

            entity.Property(e => e.Phone)
                .HasColumnName("phone")
                .HasMaxLength(50);

            entity.Property(e => e.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(255);

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.AccountStatus)
                .HasColumnName("account_status")
                .HasMaxLength(50);

            // Ignore properties that are not in the database table
            entity.Ignore(e => e.NationalId);
            entity.Ignore(e => e.DateOfBirth);
            entity.Ignore(e => e.Address);

            // Index for email lookup (for login)
            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasFilter("[email] IS NOT NULL");
        });
    }
}

