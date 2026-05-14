using QRCoder;
using UrlShortener.Application.Common.Interfaces;

namespace UrlShortener.Infrastructure.QrCodes;

public class QrCodeGenerator : IQrCodeGenerator
{
    public byte[] GenerateAsPng(string content, int pixelsPerModule = 10)
    {
        // ECC level Q tolerates ~25% damage and works well for short URLs
        // printed at small sizes; the size cost over the default M is minimal.
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(data);
        return qrCode.GetGraphic(pixelsPerModule);
    }
}
