namespace UrlShortener.Application.Common.Interfaces;

public interface IQrCodeGenerator
{
    // Renders the content as a PNG byte array. pixelsPerModule controls the
    // size of each QR square in pixels; final image dimensions are
    // (matrixModules * pixelsPerModule) on each side. Caller is responsible
    // for converting any user-facing "size" parameter into pixelsPerModule.
    byte[] GenerateAsPng(string content, int pixelsPerModule = 10);
}
