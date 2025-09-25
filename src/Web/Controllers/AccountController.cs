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
using Microsoft.EntityFrameworkCore;
using Charter.Reporter.Domain.Approvals;

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
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager, 
        UserManager<ApplicationUser> userManager, 
        AppDbContext dbContext, 
        IEmailSender emailSender, 
        IOptionsMonitor<AutoApproveOptions> autoApprove,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _dbContext = dbContext;
        _emailSender = emailSender;
        _autoApprove = autoApprove;
        _logger = logger;
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

        // Pre-check: if already locked, short-circuit and inform user
        var wasLockedBeforeAttempt = await _userManager.IsLockedOutAsync(user);

        // Enforce approval: user must be approved or have at least one assigned role
        var roles = await _userManager.GetRolesAsync(user);
        var latestApproval = await _dbContext.ApprovalRequests
            .Where(a => a.Email == user.Email)
            .OrderByDescending(a => a.CreatedUtc)
            .FirstOrDefaultAsync();

        var isApproved = roles.Any() || (latestApproval?.Status == ApprovalStatus.Approved);
        if (!isApproved)
        {
            if (latestApproval == null || latestApproval.Status == ApprovalStatus.Pending)
            {
                ModelState.AddModelError(string.Empty, "Your account is pending approval. Please contact a Charter administrator to get access.");
                ViewBag.ErrorMessage = "Your account is pending approval. Please contact a Charter administrator to get access.";
            }
            else if (latestApproval.Status == ApprovalStatus.Rejected)
            {
                var reason = string.IsNullOrWhiteSpace(latestApproval.DecisionReason) ? "Please contact a Charter administrator." : latestApproval.DecisionReason;
                ModelState.AddModelError(string.Empty, $"Your account was not approved. {reason}");
                ViewBag.ErrorMessage = $"Your account was not approved. {reason}";
            }
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
            // If lockout occurred due to this attempt (not already locked before), send email notification
            if (!wasLockedBeforeAttempt)
            {
                try
                {
                    var html = $@"
                        <h2>Account Locked</h2>
                        <p>Hello {user.FirstName},</p>
                        <p>Your Charter Reporter account has been locked due to multiple unsuccessful login attempts.</p>
                        <p>For security, you cannot sign in even with the correct password until an administrator unlocks your profile.</p>
                        <p>Please contact a Charter administrator to get unblocked.</p>
                        <br/>
                        <p>Best regards,<br/>Charter Reporter Team</p>
                    ";
                    await _emailSender.SendAsync(user.Email!, "Charter Reporter - Your account has been locked", html);
                }
                catch (Exception)
                {
                    // Do not reveal email failures to the user on the login screen
                }
            }

            ModelState.AddModelError(string.Empty, "Your account has been locked due to multiple failed login attempts. Please contact a Charter administrator to get unblocked.");
            ViewBag.ErrorMessage = "Your account has been locked due to multiple failed login attempts. Please contact a Charter administrator to get unblocked.";
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
                    RequestedRole = model.Role,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Organization = model.Organization,
                    IdNumber = model.IdNumber,
                    Cell = model.Cell
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

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordVm());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordVm model)
    {
        if (!ModelState.IsValid)
        {
            this.AddErrorNotification("Please correct the errors and try again.", "Invalid Request");
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
        {
            // Don't reveal that the user does not exist or is not confirmed for security
            this.AddSuccessNotification(
                "If an account exists with this email address, you will receive a password reset link shortly. Please check your email.",
                "Password Reset Requested"
            );
            return RedirectToAction(LoginAction);
        }

        // Generate password reset token and create reset link
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = Url.Action("ResetPassword", "Account", new { email = model.Email, token }, Request.Scheme)!;
        
        var html = $@"
            <h2>Password Reset Request</h2>
            <p>Hello {user.FirstName},</p>
            <p>You have requested to reset your password for Charter Reporter.</p>
            <p>Please click the link below to reset your password:</p>
            <p><a href=""{resetUrl}"">Reset Password</a></p>
            <p>If you did not request this password reset, please ignore this email and your password will remain unchanged.</p>
            <p>This link will expire in 24 hours for security reasons.</p>
            <br/>
            <p>Best regards,<br/>Charter Reporter Team</p>
        ";

        try
        {
            await _emailSender.SendAsync(user.Email!, "Charter Reporter - Password Reset Request", html);
            _logger.LogInformation("Password reset email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            this.AddErrorNotification(
                "We couldn't send the password reset email. Please try again later or contact support.",
                "Email Send Failed"
            );
            return View(model);
        }

        // Show success message regardless for security
        this.AddSuccessNotification(
            "If an account exists with this email address, you will receive a password reset link shortly. Please check your email.",
            "Password Reset Requested"
        );

        // In development, show the reset link for testing
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            this.AddInfoNotification($"Development mode: <a href='{resetUrl}' class='alert-link'>Click here to reset password</a>", "Development Mode");
        }

        return RedirectToAction(LoginAction);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string email, string token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            this.AddErrorNotification("Invalid password reset link. Please request a new password reset.", "Invalid Link");
            return RedirectToAction(LoginAction);
        }

        return View(new ResetPasswordVm { Email = email, Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordVm model)
    {
        if (!ModelState.IsValid)
        {
            this.AddErrorNotification("Please correct the errors and try again.", "Invalid Request");
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            this.AddErrorNotification("Password reset failed. Please try again or request a new password reset.", "Reset Failed");
            return RedirectToAction(LoginAction);
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded)
        {
            this.AddSuccessNotification(
                $"Your password has been successfully reset, {user.FirstName}! You can now log in with your new password.",
                "Password Reset Successful"
            );
            return RedirectToAction(LoginAction);
        }

        // Handle password reset errors
        foreach (var error in result.Errors)
        {
            var friendlyError = GetFriendlyErrorMessage(error);
            ModelState.AddModelError(string.Empty, friendlyError);
        }

        this.AddErrorNotification(
            "Password reset failed. The link may have expired or been used already. Please request a new password reset.",
            "Reset Failed"
        );
        return View(model);
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

    public class ForgotPasswordVm
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordVm
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}


