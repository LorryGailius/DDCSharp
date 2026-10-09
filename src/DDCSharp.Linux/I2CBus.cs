using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace DDCSharp.Linux;

/// <summary>An open <c>/dev/i2c-N</c> character device.</summary>
internal sealed unsafe class I2CBus : IDisposable
{
    private readonly FileDescriptorHandle _handle;

    private I2CBus(string path, FileDescriptorHandle handle)
    {
        Path = path;
        _handle = handle;
    }

    public string Path { get; }

    public static bool TryOpen(string path, [NotNullWhen(true)] out I2CBus? bus, [NotNullWhen(false)] out string? error)
    {
        var fileDescriptor = NativeMethods.Open(path, NativeMethods.OpenReadWrite | NativeMethods.OpenCloseOnExec);
        if (fileDescriptor < 0)
        {
            bus = null;
            error = $"Cannot open {path}: {NativeMethods.DescribeError(Marshal.GetLastPInvokeError())}";
            return false;
        }

        bus = new I2CBus(path, new FileDescriptorHandle(fileDescriptor));
        error = null;
        return true;
    }

    public bool TryWrite(ushort address, ReadOnlySpan<byte> data, [NotNullWhen(false)] out string? error)
    {
        fixed (byte* pointer = data)
        {
            var message = new NativeMethods.I2CMessage
            {
                Address = address,
                Length = (ushort)data.Length,
                Buffer = pointer
            };
            return Transfer(&message, 1, out error);
        }
    }

    public bool TryRead(ushort address, Span<byte> buffer, [NotNullWhen(false)] out string? error)
    {
        fixed (byte* pointer = buffer)
        {
            var message = new NativeMethods.I2CMessage
            {
                Address = address,
                Flags = NativeMethods.I2CMessageRead,
                Length = (ushort)buffer.Length,
                Buffer = pointer
            };
            return Transfer(&message, 1, out error);
        }
    }

    /// <summary>
    /// Writes and then reads in one combined transaction (repeated start). DisplayPort MST hubs
    /// ignore a register offset sent as a separate write, so EDID reads must use this.
    /// </summary>
    public bool TryWriteRead(
        ushort address,
        ReadOnlySpan<byte> data,
        Span<byte> buffer,
        [NotNullWhen(false)] out string? error)
    {
        fixed (byte* writePointer = data)
        fixed (byte* readPointer = buffer)
        {
            var messages = stackalloc NativeMethods.I2CMessage[2];
            messages[0] = new NativeMethods.I2CMessage
            {
                Address = address,
                Length = (ushort)data.Length,
                Buffer = writePointer
            };
            messages[1] = new NativeMethods.I2CMessage
            {
                Address = address,
                Flags = NativeMethods.I2CMessageRead,
                Length = (ushort)buffer.Length,
                Buffer = readPointer
            };
            return Transfer(messages, 2, out error);
        }
    }

    private bool Transfer(NativeMethods.I2CMessage* messages, uint count, [NotNullWhen(false)] out string? error)
    {
        var request = new NativeMethods.I2CReadWriteData { Messages = messages, Count = count };
        if (NativeMethods.Ioctl(_handle, NativeMethods.I2CReadWrite, &request) < 0)
        {
            error = $"I2C transfer on {Path} failed: {NativeMethods.DescribeError(Marshal.GetLastPInvokeError())}";
            return false;
        }

        error = null;
        return true;
    }

    public void Dispose() => _handle.Dispose();
}
