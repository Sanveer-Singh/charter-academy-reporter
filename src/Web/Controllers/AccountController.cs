using Charter.Reporter.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Charter.Reporter.Infrastructure.Data;
using Charter.Reporter.Infrastructure.Data.Entities;
using Charter.Reporter.Shared.Email;
using Microsoft.Extensions.Options;
using Charter.Reporter.Shared.Config;

namespace Charter.Reporter.Web.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _dbContext;
    private readonly IEmailSender _emailSender;
    private readonly IOptionsMonitor<AutoApproveOptions> _autoApprove;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, AppDbContext dbContext, IEmailSender emailSender, IOptionsMonitor<AutoApproveOptions> autoApprove)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _dbContext = dbContext;
        _emailSender = emailSender;
        _autoApprove = autoApprove;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginVm { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return LocalRedirect(model.ReturnUrl ?? Url.Action("Index", "Home")!);
        }

        if (result.IsNotAllowed)
        {
            ModelState.AddModelError(string.Empty, "Email not confirmed.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Organization = model.Organization,
            IdNumber = model.IdNumber,
            Cell = model.Cell,
            Address = model.Address,
            RequestedRole = model.Role
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            if (_autoApprove.CurrentValue.CharterAdmins.Contains(user.Email!, StringComparer.OrdinalIgnoreCase))
            {
                // Auto confirm and grant Charter Admin
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                await _userManager.ConfirmEmailAsync(user, token);
                await _userManager.AddToRoleAsync(user, Charter.Reporter.Domain.Roles.AppRoles.CharterAdmin);
            }
            else
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, Request.Scheme)!;
                var html = $"<p>Please confirm your email by clicking <a href=\"{confirmUrl}\">this link</a>.</p>";
                await _emailSender.SendAsync(user.Email!, "Confirm your email", html);
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    HttpContext.Items["DevConfirmLink"] = confirmUrl;
                }
            }

            // Queue approval request for role assignment
            if (!string.IsNullOrWhiteSpace(model.Role))
            {
                _dbContext.ApprovalRequests.Add(new ApprovalRequest
                {
                    Email = model.Email,
                    RequestedRole = model.Role
                });
                await _dbContext.SaveChangesAsync();
            }
            // Email confirmation is required; in Phase 1 we leave email sending as a stub.
            // Redirect to login with notice.
            return RedirectToAction("Login");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();
        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
        {
            return RedirectToAction("Login");
        }
        return BadRequest("Invalid token");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public class LoginVm
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class RegisterVm
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        public string LastName { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Organization { get; set; } = string.Empty;
        [Required]
        public string Role { get; set; } = string.Empty;
        [Required]
        public string IdNumber { get; set; } = string.Empty;
        [Required]
        public string Cell { get; set; } = string.Empty;
        [Required]
        public string Address { get; set; } = string.Empty;
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}


