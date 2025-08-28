using Charter.ReporterApp.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Charter.ReporterApp.Infrastructure.Data;

/// <summary>
/// Data seeding service for initial application setup
/// </summary>
public static class SeedData
{
    public static async Task InitializeAsync(
        AppDbContext context,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        ILogger logger)
    {
        try
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed roles
            await SeedRolesAsync(roleManager, logger);

            // Seed default admin user
            await SeedAdminUserAsync(userManager, logger);

            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<Role> roleManager, ILogger logger)
    {
        var roles = new[]
        {
            new { Name = "Charter-Admin", Description = "Charter Administrator with full system access" },
            new { Name = "Rebosa-Admin", Description = "Rebosa Administrator with organization-specific access" },
            new { Name = "PPRA-Admin", Description = "PPRA Administrator with regulatory access" }
        };

        foreach (var roleInfo in roles)
        {
            var existingRole = await roleManager.FindByNameAsync(roleInfo.Name);
            if (existingRole == null)
            {
                var role = new Role
                {
                    Name = roleInfo.Name,
                    Description = roleInfo.Description,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    logger.LogInformation("Created role: {RoleName}", roleInfo.Name);
                }
                else
                {
                    logger.LogError("Failed to create role {RoleName}: {Errors}", 
                        roleInfo.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<User> userManager, ILogger logger)
    {
        const string adminEmail = "admin@charter.co.za";
        
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                Organization = "Charter Institute",
                IdNumber = "0000000000000", // Placeholder ID
                Address = "Cape Town, South Africa",
                PhoneNumber = "+27000000000",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ApprovedBy = "System",
                ApprovedAt = DateTime.UtcNow
            };

            const string defaultPassword = "Charter@2024!";
            var result = await userManager.CreateAsync(adminUser, defaultPassword);
            
            if (result.Succeeded)
            {
                // Add admin to Charter-Admin role
                var roleResult = await userManager.AddToRoleAsync(adminUser, "Charter-Admin");
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Created default admin user: {Email}", adminEmail);
                    logger.LogWarning("Default admin password is: {Password} - Please change this immediately!", defaultPassword);
                }
                else
                {
                    logger.LogError("Failed to assign role to admin user: {Errors}", 
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogError("Failed to create admin user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            logger.LogInformation("Admin user already exists: {Email}", adminEmail);
        }
    }

    public static async Task SeedTestDataAsync(AppDbContext context, ILogger logger)
    {
        // Seed some test registration requests for development
        if (!context.RegistrationRequests.Any())
        {
            var testRequests = new[]
            {
                new RegistrationRequest
                {
                    FullName = "John Doe",
                    Email = "john.doe@example.com",
                    Organization = "Test Organization",
                    RequestedRole = "PPRA-Admin",
                    IdNumber = "8001011234567",
                    PhoneNumber = "+27123456789",
                    Address = "123 Test Street, Cape Town",
                    Status = RegistrationStatus.Pending,
                    EmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new RegistrationRequest
                {
                    FullName = "Jane Smith",
                    Email = "jane.smith@example.com",
                    Organization = "Another Test Org",
                    RequestedRole = "Rebosa-Admin",
                    IdNumber = "9002021234567",
                    PhoneNumber = "+27987654321",
                    Address = "456 Another Street, Johannesburg",
                    Status = RegistrationStatus.Pending,
                    EmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            };

            context.RegistrationRequests.AddRange(testRequests);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} test registration requests", testRequests.Length);
        }
    }
}