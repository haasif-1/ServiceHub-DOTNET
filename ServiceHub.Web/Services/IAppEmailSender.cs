namespace ServiceHub.Web.Services;

public interface IAppEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
