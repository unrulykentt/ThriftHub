using QRCoder;

namespace ThriftHub.Services
{
    public class QrCodeService
    {
        public byte[] CreatePng(
            string content,
            int pixelsPerModule = 12)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException(
                    "QR content is required.",
                    nameof(content));
            }

            using var generator = new QRCodeGenerator();

            using var data = generator.CreateQrCode(
                content.Trim(),
                QRCodeGenerator.ECCLevel.Q);

            using var qrCode = new PngByteQRCode(data);

            return qrCode.GetGraphic(pixelsPerModule);
        }
    }
}
