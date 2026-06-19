using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using VNO.Client.Services;
using Xunit;

namespace VNO.Client.Tests;

/// <summary>
/// Covers the image fetcher's local loads and its safety guards
/// </summary>
public sealed class ImageFetcherTests
{
    private static ImageFetcher Build() => new(NullLogger<ImageFetcher>.Instance);

    [Fact]
    public async Task Missing_local_file_returns_null()
    {
        var fetcher = Build();
        var result = await fetcher.FetchAsync(Path.Combine(Path.GetTempPath(), "no-such-image-vno.png"));
        Assert.Null(result);
    }

    [Fact]
    public async Task Blank_url_returns_null()
    {
        var fetcher = Build();
        Assert.Null(await fetcher.FetchAsync(string.Empty));
        Assert.Null(await fetcher.FetchAsync("   "));
    }

    [Theory]
    [InlineData("ftp://example.com/pic.png")]
    [InlineData("data:image/png;base64,AAAA")]
    [InlineData("javascript:alert(1)")]
    public async Task Unsupported_schemes_return_null(string url)
    {
        // these must be rejected before any fetch or decode happens
        var fetcher = Build();
        Assert.Null(await fetcher.FetchAsync(url));
    }
}
