using Charter.ReporterApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Charter.ReporterApp.Infrastructure.Services;

/// <summary>
/// Email service implementation for sending notifications
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly SmtpClient _smtpClient;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        var smtpServer = _configuration["EmailSettings:SmtpServer"];
        var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
        var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");
        var username = _configuration["EmailSettings:Username"];
        var password = _configuration["EmailSettings:Password"];

        _smtpClient = new SmtpClient(smtpServer, smtpPort)
        {
            EnableSsl = enableSsl,
            Credentials = new NetworkCredential(username, password)
        };
    }

    public async Task<bool> SendVerificationEmailAsync(string email, string verificationToken)
    {
        try
        {
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var fromName = _configuration["EmailSettings:FromName"];
            var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:5001";
            
            var verificationUrl = $"{baseUrl}/Account/ConfirmEmail?userId={email}&token={verificationToken}";
            
            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail ?? "", fromName ?? "Charter Institute"),
                Subject = "Verify your email address - Charter Reporter",
                Body = GetVerificationEmailBody(verificationUrl),
                IsBodyHtml = true
            };
            
            mailMessage.To.Add(email);
            
            await _smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Verification email sent to {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendApprovalNotificationAsync(string email, string userName)
    {
        try
        {
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var fromName = _configuration["EmailSettings:FromName"];
            var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:5001";
            
            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail ?? "", fromName ?? "Charter Institute"),
                Subject = "Registration Approved - Charter Reporter",
                Body = GetApprovalEmailBody(userName, $"{baseUrl}/Account/Login"),
                IsBodyHtml = true
            };
            
            mailMessage.To.Add(email);
            
            await _smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Approval notification sent to {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send approval notification to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendRejectionNotificationAsync(string email, string userName, string reason)
    {
        try
        {
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var fromName = _configuration["EmailSettings:FromName"];
            
            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail ?? "", fromName ?? "Charter Institute"),
                Subject = "Registration Update - Charter Reporter",
                Body = GetRejectionEmailBody(userName, reason),
                IsBodyHtml = true
            };
            
            mailMessage.To.Add(email);
            
            await _smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Rejection notification sent to {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send rejection notification to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetEmailAsync(string email, string resetToken)
    {
        try
        {
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var fromName = _configuration["EmailSettings:FromName"];
            var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:5001";
            
            var resetUrl = $"{baseUrl}/Account/ResetPassword?userId={email}&token={resetToken}";
            
            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail ?? "", fromName ?? "Charter Institute"),
                Subject = "Password Reset - Charter Reporter",
                Body = GetPasswordResetEmailBody(resetUrl),
                IsBodyHtml = true
            };
            
            mailMessage.To.Add(email);
            
            await _smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Password reset email sent to {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendWelcomeEmailAsync(string email, string userName, string temporaryPassword)
    {
        try
        {
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var fromName = _configuration["EmailSettings:FromName"];
            var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:5001";
            
            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail ?? "", fromName ?? "Charter Institute"),
                Subject = "Welcome to Charter Reporter",
                Body = GetWelcomeEmailBody(userName, temporaryPassword, $"{baseUrl}/Account/Login"),
                IsBodyHtml = true
            };
            
            mailMessage.To.Add(email);
            
            await _smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Welcome email sent to {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", email);
            return false;
        }
    }

    private static string GetVerificationEmailBody(string verificationUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Verify your email</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #1e3a8a;'>Verify Your Email Address</h2>
        <p>Thank you for registering with Charter Reporter. Please click the button below to verify your email address:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{verificationUrl}' style='background: #1e3a8a; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>Verify Email</a>
        </div>
        <p>If the button doesn't work, copy and paste this link into your browser:</p>
        <p style='word-break: break-all;'>{verificationUrl}</p>
        <p><small>This link will expire in 24 hours for security reasons.</small></p>
        <hr style='margin: 30px 0;'>
        <p><small>If you didn't create an account, please ignore this email.</small></p>
    </div>
</body>
</html>";
    }

    private static string GetApprovalEmailBody(string userName, string loginUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Registration Approved</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #10b981;'>Registration Approved!</h2>
        <p>Dear {userName},</p>
        <p>Great news! Your registration for Charter Reporter has been approved. You can now access the system using your registered email address.</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{loginUrl}' style='background: #10b981; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>Login Now</a>
        </div>
        <p>Welcome to the Charter Institute community!</p>
        <hr style='margin: 30px 0;'>
        <p><small>Charter Institute - Professional Development Excellence</small></p>
    </div>
</body>
</html>";
    }

    private static string GetRejectionEmailBody(string userName, string reason)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Registration Update</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #ef4444;'>Registration Update</h2>
        <p>Dear {userName},</p>
        <p>Thank you for your interest in Charter Reporter. Unfortunately, we are unable to approve your registration at this time.</p>
        <p><strong>Reason:</strong> {reason}</p>
        <p>If you believe this is an error or would like to appeal this decision, please contact our support team.</p>
        <hr style='margin: 30px 0;'>
        <p><small>Charter Institute - Professional Development Excellence</small></p>
    </div>
</body>
</html>";
    }

    private static string GetPasswordResetEmailBody(string resetUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Password Reset</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #1e3a8a;'>Password Reset Request</h2>
        <p>You requested a password reset for your Charter Reporter account. Click the button below to reset your password:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{resetUrl}' style='background: #1e3a8a; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>Reset Password</a>
        </div>
        <p>If the button doesn't work, copy and paste this link into your browser:</p>
        <p style='word-break: break-all;'>{resetUrl}</p>
        <p><small>This link will expire in 1 hour for security reasons.</small></p>
        <hr style='margin: 30px 0;'>
        <p><small>If you didn't request a password reset, please ignore this email.</small></p>
    </div>
</body>
</html>";
    }

    private static string GetWelcomeEmailBody(string userName, string temporaryPassword, string loginUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Welcome to Charter Reporter</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #1e3a8a;'>Welcome to Charter Reporter!</h2>
        <p>Dear {userName},</p>
        <p>Your account has been created successfully. Here are your login credentials:</p>
        <div style='background: #f8f9fc; padding: 15px; border-radius: 5px; margin: 20px 0;'>
            <p><strong>Temporary Password:</strong> {temporaryPassword}</p>
        </div>
        <p><strong>Important:</strong> Please change your password after your first login for security reasons.</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{loginUrl}' style='background: #1e3a8a; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>Login Now</a>
        </div>
        <p>Welcome to the Charter Institute community!</p>
        <hr style='margin: 30px 0;'>
        <p><small>Charter Institute - Professional Development Excellence</small></p>
    </div>
</body>
</html>";
    }

    public void Dispose()
    {
        _smtpClient?.Dispose();
    }
}