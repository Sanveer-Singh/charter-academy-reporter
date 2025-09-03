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
using Charter.Reporter.Web.Extensions;

namespace Charter.Reporter.Web.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private const string LoginAction = "Login";
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
        // Debug: Log model state for troubleshooting
        if (!ModelState.IsValid) 
        {
            // Simple error without notification system for debugging
            ViewBag.ErrorMessage = "Please correct the errors and try again.";
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                ViewBag.ErrorMessage += " " + error.ErrorMessage;
            }
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Security: Don't reveal if user exists, but provide helpful guidance
            ModelState.AddModelError(string.Empty, "Invalid email address or password.");
            ViewBag.ErrorMessage = "Invalid email address or password. Please try again.";
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        
        if (result.Succeeded)
        {
            return LocalRedirect(model.ReturnUrl ?? Url.Action("Index", "Dashboard")!);
        }

        if (result.IsNotAllowed)
        {
            ModelState.AddModelError(string.Empty, "Please confirm your email address before logging in.");
            ViewBag.ErrorMessage = "Please confirm your email address before logging in. Check your email for a confirmation link.";
            return View(model);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Your account has been temporarily locked due to multiple failed login attempts.");
            ViewBag.ErrorMessage = "Your account has been temporarily locked. Please try again in a few minutes.";
            return View(model);
        }

        // Generic error for invalid password (security: don't reveal user exists)
        ModelState.AddModelError(string.Empty, "Invalid email address or password.");
        ViewBag.ErrorMessage = "Invalid email address or password. Please check your credentials and try again.";
        return View(model);
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm model)
    {
        if (!ModelState.IsValid) 
        {
            this.AddErrorNotification("Please correct the errors below and try again.", "Registration Error");
            return View(model);
        }

        // Check if user already exists (provide helpful feedback)
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email address already exists.");
            this.AddWarningNotification(
                "An account with this email address already exists. If this is your account, please <a href='" + Url.Action("Login") + "' class='alert-link'>login here</a>. If you forgot your password, please contact support.",
                "Account Already Exists"
            );
            return View(model);
        }

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
            bool isAutoApproved = false;
            
            if (_autoApprove.CurrentValue.CharterAdmins.Contains(user.Email!, StringComparer.OrdinalIgnoreCase))
            {
                // Auto confirm and grant Charter Admin
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                await _userManager.ConfirmEmailAsync(user, token);
                await _userManager.AddToRoleAsync(user, Charter.Reporter.Domain.Roles.AppRoles.CharterAdmin);
                isAutoApproved = true;
            }
            else
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, Request.Scheme)!;
                var html = $"<p>Please confirm your email by clicking <a href=\"{confirmUrl}\">this link</a>.</p>";
                
                try
                {
                    await _emailSender.SendAsync(user.Email!, "Charter Reporter - Confirm your email", html);
                }
                catch (Exception)
                {
                    // Email failed, but don't block registration
                    this.AddWarningNotification(
                        "Your account was created successfully, but we couldn't send the confirmation email. Please contact support to activate your account.",
                        "Email Send Failed"
                    );
                }
                
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    HttpContext.Items["DevConfirmLink"] = confirmUrl;
                    this.AddInfoNotification($"Development mode: <a href='{confirmUrl}' class='alert-link'>Click here to confirm your email</a>", "Development Mode");
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

            // Success messages based on account type
            if (isAutoApproved)
            {
                this.AddSuccessNotification(
                    $"Welcome to Charter Reporter, {user.FirstName}! Your account has been automatically approved and you can now log in.",
                    "Account Created & Approved"
                );
            }
            else
            {
                this.AddSuccessNotification(
                    $"Thank you for registering, {user.FirstName}! Please check your email to confirm your account, then wait for approval from a Charter administrator. We'll notify you once your account is approved.",
                    "Registration Successful"
                );
            }
            
            return RedirectToAction(LoginAction);
        }

        // Handle registration errors with helpful messages
        foreach (var error in result.Errors)
        {
            var friendlyError = GetFriendlyErrorMessage(error);
            ModelState.AddModelError(GetErrorField(error), friendlyError);
        }
        
        this.AddErrorNotification("Please fix the issues below and try again.", "Registration Failed");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            this.AddErrorNotification("Invalid confirmation link. Please check the link in your email or request a new confirmation email.", "Invalid Link");
            return RedirectToAction(LoginAction);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) 
        {
            this.AddErrorNotification("User not found. Please check the link in your email or contact support.", "User Not Found");
            return RedirectToAction(LoginAction);
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
        {
            this.AddSuccessNotification(
                $"Thank you, {user.FirstName}! Your email has been confirmed successfully. You can now log in to your account.",
                "Email Confirmed"
            );
            return RedirectToAction(LoginAction);
        }

        this.AddErrorNotification(
            "The confirmation link is invalid or has expired. Please request a new confirmation email or contact support.",
            "Confirmation Failed"
        );
        return RedirectToAction(LoginAction);
    }

    /// <summary>
    /// Converts Identity error messages to user-friendly messages
    /// </summary>
    private static string GetFriendlyErrorMessage(IdentityError error)
    {
        return error.Code switch
        {
            "DuplicateEmail" => "An account with this email address already exists.",
            "InvalidEmail" => "Please enter a valid email address.",
            "PasswordTooShort" => "Password must be at least 8 characters long.",
            "PasswordRequiresNonAlphanumeric" => "Password must contain at least one special character (e.g., !, @, #, $).",
            "PasswordRequiresDigit" => "Password must contain at least one number (0-9).",
            "PasswordRequiresLower" => "Password must contain at least one lowercase letter (a-z).",
            "PasswordRequiresUpper" => "Password must contain at least one uppercase letter (A-Z).",
            "PasswordRequiresUniqueChars" => "Password must contain at least 6 different characters.",
            "UserAlreadyHasPassword" => "A password has already been set for this account.",
            "InvalidToken" => "The security token is invalid or has expired.",
            "DuplicateUserName" => "This username is already taken.",
            _ => error.Description // Fallback to original description
        };
    }

    /// <summary>
    /// Maps Identity error codes to form field names for better UX
    /// </summary>
    private static string GetErrorField(IdentityError error)
    {
        return error.Code switch
        {
            "DuplicateEmail" or "InvalidEmail" => nameof(RegisterVm.Email),
            "PasswordTooShort" or "PasswordRequiresNonAlphanumeric" or "PasswordRequiresDigit" or 
            "PasswordRequiresLower" or "PasswordRequiresUpper" or "PasswordRequiresUniqueChars" => nameof(RegisterVm.Password),
            "DuplicateUserName" => nameof(RegisterVm.Email),
            _ => string.Empty // General error, not field-specific
        };
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


