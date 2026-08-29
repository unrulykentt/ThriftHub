namespace ThriftHub.Services
{
    public interface IWhatsAppOtpSender
    {
        Task SendVerificationCodeAsync(
            string phoneNumber,
            string code,
            CancellationToken cancellationToken = default);

        bool IsConfigured { get; }
    }
}
