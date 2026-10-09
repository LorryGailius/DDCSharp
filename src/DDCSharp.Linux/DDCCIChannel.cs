using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using DDCSharp.Core.Capabilities;

namespace DDCSharp.Linux;

internal enum DDCStatus
{
    Ok,
    Unsupported,
    Failed
}

internal readonly record struct VCPReading(VCPFeatureType Type, ushort Current, ushort Maximum);

/// <summary>
/// DDC/CI message exchange with one display over an I2C bus.
/// </summary>
internal sealed class DDCCIChannel : IDisposable
{
    private const ushort DDCAddress = 0x37;
    private const ushort EDIDAddress = 0x50;

    // Bytes on the wire: host requests are checksummed with the display's write address (0x6E),
    // replies with the virtual host address (0x50)
    private const byte HostAddress = 0x51;
    private const byte DisplayAddress = 0x6E;
    private const byte ReplyChecksumSeed = 0x50;

    private const byte GetVCPRequest = 0x01;
    private const byte GetVCPReply = 0x02;
    private const byte SetVCPRequest = 0x03;
    private const byte CapabilitiesRequest = 0xF3;
    private const byte CapabilitiesReply = 0xE3;

    private const int GetVCPReplyLength = 11;
    private const int CapabilitiesReplyLength = 38;
    private const int MaxCapabilitiesLength = 8192;

    private static readonly ConcurrentDictionary<string, BusState> BusStates = new();

    private readonly I2CBus _bus;
    private readonly BusState _state;
    private readonly DDCTimings _timings;

    public DDCCIChannel(I2CBus bus, DDCTimings timings)
    {
        _bus = bus;
        _timings = timings;
        _state = BusStates.GetOrAdd(bus.Path, _ => new BusState());
    }

    public string BusPath => _bus.Path;

    public DDCStatus TryGetVCP(byte code, out VCPReading reading, out string? error)
    {
        Span<byte> request = [GetVCPRequest, code];
        Span<byte> reply = stackalloc byte[GetVCPReplyLength];
        reading = default;
        if (IsWriteInBackground(out error))
        {
            return DDCStatus.Failed;
        }

        lock (_state)
        {
            for (var attempt = 1; attempt <= _timings.MaxAttempts; attempt++)
            {
                if (!TryExchange(request, _timings.GetReplyDelay, reply, out var payload, out error))
                {
                    continue;
                }
                if (payload.Length != 8 || payload[0] != GetVCPReply || payload[2] != code)
                {
                    error = $"Unexpected Get VCP reply {Convert.ToHexString(payload)}";
                    Backoff();
                    continue;
                }
                if (payload[1] == 0x01)
                {
                    error = $"VCP code 0x{code:X2} is not supported by the display";
                    return DDCStatus.Unsupported;
                }
                if (payload[1] != 0x00)
                {
                    error = $"Get VCP reply has result code 0x{payload[1]:X2}";
                    Backoff();
                    continue;
                }

                var type = payload[3] == 0x01 ? VCPFeatureType.Momentary : VCPFeatureType.SetParameter;
                reading = new VCPReading(type, (ushort)(payload[6] << 8 | payload[7]), (ushort)(payload[4] << 8 | payload[5]));
                error = null;
                return DDCStatus.Ok;
            }
        }
        return DDCStatus.Failed;
    }

    public bool TrySetVCP(byte code, ushort value, [NotNullWhen(false)] out string? error) =>
        !IsWriteInBackground(out error) && WriteVCP(code, value, _timings.MaxAttempts, out error);

    /// <summary>
    /// Sends a Set VCP Feature write once and waits at most <paramref name="wait"/> for the bus to acknowledge it.
    /// </summary>
    /// <remarks>
    /// Switching the input away from this machine over DisplayPort MST makes the display drop the MST link right after
    /// it accepts the write, and the kernel then waits 4 seconds for a sideband reply that never comes. The write is
    /// left to finish in the background; until it does, every other command on this bus fails immediately so callers
    /// move on to the bus the display reappears on instead of queueing behind it.
    /// </remarks>
    /// <returns>True if the write was acknowledged or is still pending after <paramref name="wait"/>.</returns>
    public bool TrySetVCPInBackground(byte code, ushort value, TimeSpan wait, [NotNullWhen(false)] out string? error)
    {
        if (IsWriteInBackground(out error))
        {
            return false;
        }

        var write = new Task<(bool Written, string? Error)>(() => (WriteVCP(code, value, 1, out var writeError), writeError));
        _state.BackgroundWrite = write;
        write.Start();
        if (!write.Wait(wait))
        {
            error = null;
            return true;
        }

        if (write.Result.Written)
        {
            error = null;
            return true;
        }
        error = write.Result.Error ?? "Write failed";
        return false;
    }

    /// <summary>True while an input switch write started by <see cref="TrySetVCPInBackground"/> is still pending on the bus.</summary>
    public static bool IsWriteInBackground(string busPath) =>
        BusStates.TryGetValue(busPath, out var state) && state.BackgroundWrite is { IsCompleted: false };

    private bool IsWriteInBackground([NotNullWhen(true)] out string? error)
    {
        error = _state.BackgroundWrite is { IsCompleted: false }
            ? $"An input switch write is still pending on {_bus.Path}"
            : null;
        return error != null;
    }

    private bool WriteVCP(byte code, ushort value, int maxAttempts, [NotNullWhen(false)] out string? error)
    {
        Span<byte> request = [SetVCPRequest, code, (byte)(value >> 8), (byte)value];
        error = null;

        lock (_state)
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                WaitUntilReady();
                var written = _bus.TryWrite(DDCAddress, BuildRequest(request), out error);
                MarkBusy(written ? _timings.CommandInterval : _timings.RetryDelay);
                if (written)
                {
                    return true;
                }
            }
        }
        error ??= "No attempts were made";
        return false;
    }

    public string? TryGetCapabilities(out string? error)
    {
        var data = new List<byte>();
        var reply = new byte[CapabilitiesReplyLength];
        if (IsWriteInBackground(out error))
        {
            return null;
        }

        lock (_state)
        {
            while (true)
            {
                var offset = data.Count;
                Span<byte> request = [CapabilitiesRequest, (byte)(offset >> 8), (byte)offset];
                ReadOnlySpan<byte> fragment = default;
                var received = false;

                for (var attempt = 1; attempt <= _timings.MaxAttempts && !received; attempt++)
                {
                    if (!TryExchange(request, _timings.CapabilitiesReplyDelay, reply, out var payload, out error))
                    {
                        continue;
                    }
                    if (payload.Length < 3 || payload[0] != CapabilitiesReply || (payload[1] << 8 | payload[2]) != offset)
                    {
                        error = $"Unexpected capabilities reply at offset {offset}: {Convert.ToHexString(payload)}";
                        Backoff();
                        continue;
                    }
                    fragment = payload[3..];
                    received = true;
                }

                if (!received)
                {
                    return null;
                }
                if (fragment.IsEmpty)
                {
                    break;
                }

                data.AddRange(fragment);
                if (data.Count > MaxCapabilitiesLength)
                {
                    error = $"Capabilities string exceeds {MaxCapabilitiesLength} bytes";
                    return null;
                }
            }
        }

        error = null;
        return Encoding.ASCII.GetString(data.ToArray()).TrimEnd('\0');
    }

    /// <summary>Reads the 128-byte EDID base block with a combined write/read transaction.</summary>
    public static bool TryReadEDID(I2CBus bus, Span<byte> buffer, [NotNullWhen(false)] out string? error)
    {
        ReadOnlySpan<byte> offset = [0x00];
        return bus.TryWriteRead(EDIDAddress, offset, buffer[..128], out error);
    }

    /// <summary>
    /// Sends one request, waits for the display to prepare its reply and reads it back.
    /// On failure the bus is marked busy for the retry delay.
    /// </summary>
    private bool TryExchange(
        scoped ReadOnlySpan<byte> request,
        TimeSpan replyDelay,
        Span<byte> reply,
        out ReadOnlySpan<byte> payload,
        [NotNullWhen(false)] out string? error)
    {
        payload = default;
        WaitUntilReady();
        if (!_bus.TryWrite(DDCAddress, BuildRequest(request), out error))
        {
            Backoff();
            return false;
        }

        Thread.Sleep(replyDelay);
        if (!_bus.TryRead(DDCAddress, reply, out error))
        {
            Backoff();
            return false;
        }
        MarkBusy(_timings.CommandInterval);

        if (!TryParseReply(reply, out payload, out error))
        {
            Backoff();
            return false;
        }
        return true;
    }

    private static byte[] BuildRequest(ReadOnlySpan<byte> payload)
    {
        var message = new byte[payload.Length + 3];
        message[0] = HostAddress;
        message[1] = (byte)(0x80 | payload.Length);
        payload.CopyTo(message.AsSpan(2));
        message[^1] = Checksum(DisplayAddress, message.AsSpan(0, message.Length - 1));
        return message;
    }

    private static bool TryParseReply(
        ReadOnlySpan<byte> reply,
        out ReadOnlySpan<byte> payload,
        [NotNullWhen(false)] out string? error)
    {
        payload = default;
        if (reply[0] != DisplayAddress || (reply[1] & 0x80) == 0)
        {
            error = $"Invalid reply {Convert.ToHexString(reply)}";
            return false;
        }

        var length = reply[1] & 0x7F;
        if (length == 0)
        {
            error = "Display sent a null message (busy or command not supported)";
            return false;
        }
        if (length + 3 > reply.Length)
        {
            error = $"Reply length {length} does not fit in the read buffer: {Convert.ToHexString(reply)}";
            return false;
        }
        if (Checksum(ReplyChecksumSeed, reply[..(length + 2)]) != reply[length + 2])
        {
            error = $"Reply checksum mismatch: {Convert.ToHexString(reply[..(length + 3)])}";
            return false;
        }

        payload = reply.Slice(2, length);
        error = null;
        return true;
    }

    private static byte Checksum(byte seed, ReadOnlySpan<byte> data)
    {
        var checksum = seed;
        foreach (var value in data)
        {
            checksum ^= value;
        }
        return checksum;
    }

    private void WaitUntilReady()
    {
        var remaining = Stopwatch.GetElapsedTime(Stopwatch.GetTimestamp(), _state.ReadyAt);
        if (remaining > TimeSpan.Zero)
        {
            Thread.Sleep(remaining);
        }
    }

    private void Backoff() => MarkBusy(_timings.RetryDelay);

    private void MarkBusy(TimeSpan delay) =>
        _state.ReadyAt = Stopwatch.GetTimestamp() + (long)(delay.TotalSeconds * Stopwatch.Frequency);

    public void Dispose() => _bus.Dispose();

    /// <summary>Timing state shared by every channel on the same bus, also used as its lock.</summary>
    private sealed class BusState
    {
        public long ReadyAt;
        public volatile Task? BackgroundWrite;
    }
}
