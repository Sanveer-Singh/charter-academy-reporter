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
            _logger.LogWarning("SendGrid API key missing; suppressing email to {ToEmail}", toEmail);
            return;
        }

        SendGridClient client;
        if (!string.IsNullOrWhiteSpace(_dataResidency))
        {
            var options = new SendGridClientOptions
            {
                ApiKey = _apiKey
            };
            options.SetDataResidency(_dataResidency);
            client = new SendGridClient(options);
        }
        else
        {
            client = new SendGridClient(_apiKey);
        }

        var from = new EmailAddress(_settings.FromAddress, _settings.FromName);
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, htmlBody, htmlBody);

        try
        {
            var response = await client.SendEmailAsync(msg, cancellationToken);
            if ((int)response.StatusCode >= 400)
            {
                var body = await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogError("SendGrid failed with {StatusCode}: {Body}", response.StatusCode, body);
                if (string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Suppressing SendGrid exception in Development environment.");
                    return;
                }
                throw new InvalidOperationException($"SendGrid send failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} via SendGrid", toEmail);
            if (string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Suppressing SendGrid exception in Development environment.");
                return;
            }
            throw;
        }
    }
}


