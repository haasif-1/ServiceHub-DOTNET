namespace ServiceHub.Web.Services;

public sealed class LoggingEmailSender : IAppEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Password-reset email queued for {Email}. Subject: {Subject}. Body: {Body}",
            toEmail,
            subject,
            body);

        return Task.CompletedTask;
    }
}
