using Charter.Reporter.Shared.Email;
using Microsoft.Extensions.Logging;

namespace Charter.Reporter.Infrastructure.Email;

public class DevNoopEmailSender : IEmailSender
{
    private readonly ILogger<DevNoopEmailSender> _logger;

    public DevNoopEmailSender(ILogger<DevNoopEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV] Email suppressed. To: {To} | Subject: {Subject} | Body: {Body}", toEmail, subject, htmlBody);
        return Task.CompletedTask;
    }
}


