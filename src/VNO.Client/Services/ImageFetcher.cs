using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;

namespace VNO.Client.Services;

/// <summary>
/// Default image fetcher over the local file system and http
/// </summary>
/// <remarks>
/// Local file urls and paths load directly. Remote urls are fetched over http
/// with guards, only http and https are allowed, the response is capped so a
/// huge body cannot exhaust memory, and a short timeout stops a slow host from
/// hanging the stage. Any failure returns null rather than throwing
/// </remarks>
public sealed class ImageFetcher : IImageFetcher
{
    private const long MaxBytes = 8 * 1024 * 1024;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly HttpClient _http;
    private readonly ILogger<ImageFetcher> _logger;

    /// <summary>
    /// Creates the fetcher with its own http client
    /// </summary>
    public ImageFetcher(ILogger<ImageFetcher> logger)
    {
        _http = new HttpClient { Timeout = Timeout, MaxResponseContentBufferSize = MaxBytes };
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Bitmap?> FetchAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        try
        {
            // a local file or file url loads straight from disk
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.IsFile)
            {
                var path = uri?.IsFile == true ? uri.LocalPath : url;
                return File.Exists(path) ? new Bitmap(path) : null;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                _logger.LogWarning("Refused to fetch image from unsupported scheme {Scheme}", uri.Scheme);
                return null;
            }

            using var response = await _http
                .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength is > MaxBytes)
            {
                _logger.LogWarning("Refused image larger than the cap from {Url}", url);
                return null;
            }

            await using var network = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var buffer = new MemoryStream();
            await CopyCappedAsync(network, buffer, cancellationToken).ConfigureAwait(false);
            buffer.Position = 0;
            return new Bitmap(buffer);
        }
        catch (Exception ex)
        {
            // a bad or hostile url must never crash or hang the stage
            _logger.LogWarning(ex, "Failed to fetch image {Url}", url);
            return null;
        }
    }

    private static async Task CopyCappedAsync(Stream source, Stream destination, CancellationToken cancellationToken)
    {
        var chunk = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > MaxBytes)
            {
                throw new InvalidOperationException("image exceeded the size cap");
            }
            await destination.WriteAsync(chunk.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
    }
}
