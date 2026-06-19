using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace VNO.Client.Services;

/// <summary>
/// Loads an image for the stage from a local path or a remote url
/// </summary>
/// <remarks>
/// Staff can stream an image url to the room. The legacy client warned before
/// downloading untrusted content, this service applies the same caution with a
/// scheme allow list, a size cap, and a timeout so a hostile url cannot hang or
/// exhaust the client
/// </remarks>
public interface IImageFetcher
{
    /// <summary>
    /// Loads the image at the url, null when it is missing, unsafe, or fails
    /// </summary>
    Task<Bitmap?> FetchAsync(string url, CancellationToken cancellationToken = default);
}
