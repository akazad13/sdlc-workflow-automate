using NanoLink.Api.Services;
using Xunit;

namespace NanoLink.Tests;

public class feattagsAddcustomtagTests
{
    private readonly feattagsAddcustomtagService _service = new();

    [Fact]
    public void ExecuteFeature_ValidInput_ReturnsSuccess()
    {
        bool ok = _service.ExecuteFeature("sample-input", out string result);
        Assert.True(ok);
        Assert.Equal("Processed: sample-input", result);
    }
}
