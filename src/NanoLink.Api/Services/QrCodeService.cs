using System.Text;

namespace NanoLink.Api.Services;

public interface IQrCodeService
{
    string GenerateSvgQrCode(string url, int size = 256);
    byte[] GeneratePngMockQrCode(string url, int size = 256);
}

public class QrCodeService : IQrCodeService
{
    public string GenerateSvgQrCode(string url, int size = 256)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {size} {size}\" width=\"{size}\" height=\"{size}\">");
        sb.AppendLine($"  <rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");
        sb.AppendLine($"  <!-- QR Code Mock Payload: {url} -->");
        sb.AppendLine($"  <rect x=\"10\" y=\"10\" width=\"{size - 20}\" height=\"{size - 20}\" fill=\"none\" stroke=\"#000000\" stroke-width=\"4\"/>");
        sb.AppendLine($"  <text x=\"50%\" y=\"50%\" text-anchor=\"middle\" dominant-baseline=\"middle\" font-family=\"monospace\" font-size=\"12\" fill=\"#333333\">QR: {url}</text>");
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    public byte[] GeneratePngMockQrCode(string url, int size = 256)
    {
        // Standard PNG Magic Header Bytes
        byte[] pngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];
        return pngHeader;
    }
}
