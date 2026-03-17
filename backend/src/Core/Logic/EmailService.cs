using Resend;
using System.Threading.Tasks;

namespace Core.Logic
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlBody, string? from = null);
    }

    public class ResendEmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly string _defaultFrom;

        public ResendEmailService(IResend resend, string defaultFrom)
        {
            _resend = resend;
            _defaultFrom = defaultFrom;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody, string? from = null)
        {
            var message = new EmailMessage
            {
                From = from ?? _defaultFrom,
                Subject = subject,
                HtmlBody = htmlBody
            };
            message.To.Add(to);
            await _resend.EmailSendAsync(message);
        }
    }
}
