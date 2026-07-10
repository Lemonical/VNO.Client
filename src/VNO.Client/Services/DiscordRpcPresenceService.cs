using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VNO.Client.Services;

/// <summary>
/// Sends Rich Presence to Discord's local desktop IPC endpoint.
/// </summary>
/// <remarks>
/// Discord's direct Rich Presence flow requires only a public application id. This
/// client never authenticates a Discord user and never carries OAuth or bot secrets.
/// Activity payloads intentionally omit parties, buttons, invites, and join secrets.
/// </remarks>
public sealed class DiscordRpcPresenceService : IDiscordPresenceService, IAsyncDisposable
{
    private const int HandshakeOpcode = 0;
    private const int FrameOpcode = 1;
    private const int MaximumFrameBytes = 64 * 1024;
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(250);

    private readonly string _applicationId;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Stream? _stream;

    /// <summary>
    /// Creates the local RPC client. An empty application id leaves it inactive.
    /// </summary>
    public DiscordRpcPresenceService(string applicationId)
    {
        var candidate = applicationId?.Trim() ?? string.Empty;
        _applicationId = ulong.TryParse(candidate, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0
            ? candidate
            : string.Empty;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(DiscordPresence presence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(presence);
        if (_applicationId.Length == 0)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var stream = await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
            await SendCommandAsync(stream, new
            {
                cmd = "SET_ACTIVITY",
                args = new
                {
                    pid = Environment.ProcessId,
                    activity = new
                    {
                        type = 0,
                        details = presence.Details,
                        state = presence.State,
                        instance = false,
                    },
                },
                nonce = Guid.NewGuid().ToString("D"),
            }, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            DisposeConnection();
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (_applicationId.Length == 0)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var stream = _stream;
            if (stream is null)
            {
                return;
            }

            await SendCommandAsync(stream, new
            {
                cmd = "SET_ACTIVITY",
                args = new { pid = Environment.ProcessId, activity = (object?)null },
                nonce = Guid.NewGuid().ToString("D"),
            }, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            DisposeConnection();
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            DisposeConnection();
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    private async Task<Stream> EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_stream is not null)
        {
            return _stream;
        }

        _stream = OperatingSystem.IsWindows()
            ? await ConnectWindowsAsync(cancellationToken).ConfigureAwait(false)
            : await ConnectUnixAsync(cancellationToken).ConfigureAwait(false);

        await WriteFrameAsync(
            _stream,
            HandshakeOpcode,
            JsonSerializer.SerializeToUtf8Bytes(new { v = 1, client_id = _applicationId }),
            cancellationToken).ConfigureAwait(false);
        await ReadFrameAsync(_stream, cancellationToken).ConfigureAwait(false);
        return _stream;
    }

    private static async Task<Stream> ConnectWindowsAsync(CancellationToken cancellationToken)
    {
        for (var channel = 0; channel < 10; channel++)
        {
            var pipe = new NamedPipeClientStream(
                ".", $"discord-ipc-{channel}", PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(ConnectTimeout);
                await pipe.ConnectAsync(timeout.Token).ConfigureAwait(false);
                return pipe;
            }
            catch (Exception exception) when (
                exception is IOException or OperationCanceledException or UnauthorizedAccessException)
            {
                pipe.Dispose();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        throw new IOException("The Discord desktop IPC endpoint is unavailable");
    }

    private static async Task<Stream> ConnectUnixAsync(CancellationToken cancellationToken)
    {
        foreach (var path in GetUnixPaths())
        {
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(ConnectTimeout);
                await socket.ConnectAsync(new UnixDomainSocketEndPoint(path), timeout.Token).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception exception) when (
                exception is SocketException or OperationCanceledException or UnauthorizedAccessException)
            {
                socket.Dispose();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        throw new IOException("The Discord desktop IPC endpoint is unavailable");
    }

    private static IEnumerable<string> GetUnixPaths()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string?[] roots =
        [
            Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR"),
            Environment.GetEnvironmentVariable("TMPDIR"),
            Environment.GetEnvironmentVariable("TMP"),
            Environment.GetEnvironmentVariable("TEMP"),
            "/tmp",
        ];

        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            for (var channel = 0; channel < 10; channel++)
            {
                var path = Path.Combine(root, $"discord-ipc-{channel}");
                if (seen.Add(path))
                {
                    yield return path;
                }
            }
        }
    }

    private static async Task SendCommandAsync(Stream stream, object payload, CancellationToken cancellationToken)
    {
        await WriteFrameAsync(
            stream,
            FrameOpcode,
            JsonSerializer.SerializeToUtf8Bytes(payload),
            cancellationToken).ConfigureAwait(false);
        await ReadFrameAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteFrameAsync(
        Stream stream,
        int opcode,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        var header = new byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0, 4), opcode);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4, 4), payload.Length);
        await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ReadFrameAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[8];
        await ReadExactlyAsync(stream, header, cancellationToken).ConfigureAwait(false);
        var length = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4, 4));
        if (length is < 0 or > MaximumFrameBytes)
        {
            throw new InvalidDataException("Discord returned an invalid IPC frame length");
        }

        var payload = new byte[length];
        await ReadExactlyAsync(stream, payload, cancellationToken).ConfigureAwait(false);
        using var document = JsonDocument.Parse(payload);
        if (document.RootElement.TryGetProperty("evt", out var evt) &&
            string.Equals(evt.GetString(), "ERROR", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Discord rejected the Rich Presence update");
        }
    }

    private static async Task ReadExactlyAsync(
        Stream stream,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                throw new EndOfStreamException("Discord closed the IPC connection");
            }
            offset += read;
        }
    }

    private void DisposeConnection()
    {
        _stream?.Dispose();
        _stream = null;
    }
}
