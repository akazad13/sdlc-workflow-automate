using NanoLink.Api.Services;
using Xunit;

namespace NanoLink.Tests;

public class QrCodeTests
{
    private readonly QrCodeService _service = new();

    [Fact]
    public void GenerateSvg_ProducesValidSvgXml()
    {
        string url = "https://nano.link/xyz123";
        string svg = _service.GenerateSvgQrCode(url, 300);

        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
        Assert.Contains("xyz123", svg);
    }

    [Fact]
    public void GeneratePng_ProducesValidPngHeaderBytes()
    {
        byte[] bytes = _service.GeneratePngMockQrCode("https://nano.link/abc");

        Assert.NotNull(bytes);
        Assert.True(bytes.Length >= 8);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal(0x50, bytes[1]); // 'P'
        Assert.Equal(0x4E, bytes[2]); // 'N'
        Assert.Equal(0x47, bytes[3]); // 'G'
    }
}