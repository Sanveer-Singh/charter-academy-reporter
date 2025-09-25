using Charter.Reporter.Shared.Config;
using Charter.Reporter.Shared.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Charter.Reporter.Infrastructure.Email;

public class SendGridEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SendGridEmailSender> _logger;
    private readonly string _apiKey;
    private readonly string? _dataResidency;

    public SendGridEmailSender(IOptions<EmailSettings> options, ILogger<SendGridEmailSender> logger)
    {
        _settings = options.Value;
        _logger = logger;
        _apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY") ?? string.Empty;
        _dataResidency = Environment.GetEnvironmentVariable("SENDGRID_DATA_RESIDENCY");
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("SendGrid API key is missing. Please set SENDGRID_API_KEY environment variable.");
            throw new InvalidOperationException("SendGrid API key is not configured. Please set the SENDGRID_API_KEY environment variable.");
        }

        SendGridClient client;
        client = new SendGridClient("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");

        var from = new EmailAddress(_settings.FromAddress, _settings.FromName);
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, htmlBody, htmlBody);

        try
        {
            _logger.LogInformation("Sending email to {ToEmail} via SendGrid", toEmail);
            var response = await client.SendEmailAsync(msg, cancellationToken);
            
            if ((int)response.StatusCode >= 400)
            {
                var body = await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogError("SendGrid failed with {StatusCode}: {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"SendGrid send failed with status {response.StatusCode}. Response: {body}");
            }
            
            _logger.LogInformation("Successfully sent email to {ToEmail} via SendGrid. Status: {StatusCode}", toEmail, response.StatusCode);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} via SendGrid", toEmail);
            throw new InvalidOperationException($"Failed to send email via SendGrid: {ex.Message}", ex);
        }
    }
}


