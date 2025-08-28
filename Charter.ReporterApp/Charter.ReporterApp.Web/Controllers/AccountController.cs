using Charter.ReporterApp.Application.DTOs;
using Charter.ReporterApp.Application.Interfaces;
using Charter.ReporterApp.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Charter.ReporterApp.Web.Controllers;

/// <summary>
/// Account controller for authentication and registration
/// </summary>
[AllowAnonymous]
public class AccountController : BaseController
{
    private readonly IAuthenticationService _authService;
    private readonly IRegistrationService _registrationService;
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    public AccountController(
        ILogger<AccountController> logger,
        IAuditService auditService,
        ISecurityValidationService securityService,
        IAuthenticationService authService,
        IRegistrationService registrationService,
        SignInManager<User> signInManager,
        UserManager<User> userManager)
        : base(logger, auditService, securityService)
    {
        _authService = authService;
        _registrationService = registrationService;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Rate limiting
            if (await _securityService.IsRateLimitExceededAsync(model.Email, "Login"))
            {
                SetErrorMessage("Too many login attempts. Please try again later.");
                await LogUserActionAsync("LoginRateLimited", new { Email = model.Email });
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, 
                model.Password, 
                model.RememberMe, 
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    await LogUserActionAsync("LoginSuccessful", new { Email = model.Email });
                    
                    // Update last login time
                    user.LastLoginAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    SetSuccessMessage($"Welcome back, {user.FullName}!");
                    
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    
                    return RedirectToRoleDashboard();
                }
            }

            if (result.IsLockedOut)
            {
                await LogUserActionAsync("LoginLockedOut", new { Email = model.Email });
                SetErrorMessage("Account is locked due to multiple failed login attempts. Please try again later.");
                return View(model);
            }

            if (result.IsNotAllowed)
            {
                await LogUserActionAsync("LoginNotAllowed", new { Email = model.Email });
                SetErrorMessage("Please confirm your email before logging in.");
                return View(model);
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToAction("LoginWith2fa", new { returnUrl, model.RememberMe });
            }

            await LogUserActionAsync("LoginFailed", new { Email = model.Email });
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }
        catch (Exception ex)
        {
            return await HandleExceptionAsync(ex, "Login");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        try
        {
            var model = new RegisterUserDto();
            ViewBag.Roles = await GetRoleSelectListAsync();
            return View(model);
        }
        catch (Exception ex)
        {
            return await HandleExceptionAsync(ex, "Register");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterUserDto model)
    {
        try
        {
            ViewBag.Roles = await GetRoleSelectListAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Rate limiting
            if (await _securityService.IsRateLimitExceededAsync(model.Email, "Register"))
            {
                SetErrorMessage("Too many registration attempts. Please try again later.");
                return View(model);
            }

            // Additional security validation
            if (!_securityService.ValidateInputData(model))
            {
                ModelState.AddModelError(string.Empty, "Invalid input detected. Please check your data.");
                return View(model);
            }

            var result = await _registrationService.RegisterUserAsync(model);
            if (result.Succeeded)
            {
                await LogUserActionAsync("RegistrationSubmitted", new { Email = model.Email, Role = model.RequestedRole });
                SetSuccessMessage("Registration submitted successfully! Please check your email to confirm your account. Once confirmed, your request will be reviewed by an administrator.");
                return RedirectToAction("Login");
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Registration failed. Please try again.");
            return View(model);
        }
        catch (Exception ex)
        {
            return await HandleExceptionAsync(ex, "Register");
        }
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            SetErrorMessage("Invalid email confirmation link.");
            return RedirectToAction("Login");
        }

        try
        {
            var result = await _authService.ConfirmEmailAsync(userId, token);
            if (result.Succeeded)
            {
                await LogUserActionAsync("EmailConfirmed", new { UserId = userId });
                SetSuccessMessage("Email confirmed successfully! Your account is now pending administrator approval.");
            }
            else
            {
                SetErrorMessage(result.ErrorMessage ?? "Email confirmation failed.");
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex, "ConfirmEmail");
        }

        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        if (string.IsNullOrEmpty(email) || !_securityService.ValidateEmailFormat(email))
        {
            ModelState.AddModelError(nameof(email), "Please enter a valid email address.");
            return View();
        }

        try
        {
            // Rate limiting
            if (await _securityService.IsRateLimitExceededAsync(email, "PasswordReset"))
            {
                SetErrorMessage("Too many password reset attempts. Please try again later.");
                return View();
            }

            var token = await _authService.GeneratePasswordResetTokenAsync(email);
            if (!string.IsNullOrEmpty(token))
            {
                await LogUserActionAsync("PasswordResetRequested", new { Email = email });
                SetSuccessMessage("If an account with that email exists, a password reset link has been sent.");
            }
            else
            {
                // Don't reveal whether the email exists or not
                SetSuccessMessage("If an account with that email exists, a password reset link has been sent.");
            }

            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            return await HandleExceptionAsync(ex, "ForgotPassword");
        }
    }

    [HttpGet]
    public IActionResult ResetPassword(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            SetErrorMessage("Invalid password reset link.");
            return RedirectToAction("Login");
        }

        var model = new ResetPasswordDto { UserId = userId, Token = token };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _authService.ResetPasswordAsync(model.UserId, model.Token, model.Password);
            if (result.Succeeded)
            {
                await LogUserActionAsync("PasswordReset", new { UserId = model.UserId });
                SetSuccessMessage("Password reset successfully! You can now log in with your new password.");
                return RedirectToAction("Login");
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Password reset failed.");
            return View(model);
        }
        catch (Exception ex)
        {
            return await HandleExceptionAsync(ex, "ResetPassword");
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        try
        {
            await LogUserActionAsync("Logout");
            await _signInManager.SignOutAsync();
            SetSuccessMessage("You have been logged out successfully.");
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            return await HandleExceptionAsync(ex, "Logout");
        }
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        SetWarningMessage("You don't have permission to access this resource.");
        return View();
    }

    private async Task<IEnumerable<SelectListItem>> GetRoleSelectListAsync()
    {
        try
        {
            var roles = await _registrationService.GetAvailableRolesAsync();
            return roles.Select(r => new SelectListItem
            {
                Value = r.Name,
                Text = r.Description ?? r.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role select list");
            return Enumerable.Empty<SelectListItem>();
        }
    }
}

/// <summary>
/// Reset password DTO
/// </summary>
public class ResetPasswordDto
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}