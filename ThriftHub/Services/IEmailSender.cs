using System.Threading.Tasks;

namespace ThriftHub.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(
            string email,
            string subject,
            string message);
    }
}