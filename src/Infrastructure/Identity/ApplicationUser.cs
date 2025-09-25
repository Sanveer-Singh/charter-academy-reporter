using Microsoft.AspNetCore.Identity;

namespace Charter.Reporter.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Organization { get; set; }
    public string? IdNumber { get; set; }
    public string? Cell { get; set; }
    public string? Address { get; set; }
    public string? RequestedRole { get; set; }
}


