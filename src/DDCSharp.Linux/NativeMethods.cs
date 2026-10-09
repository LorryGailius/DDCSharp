using System.Runtime.InteropServices;

namespace DDCSharp.Linux;

internal static unsafe partial class NativeMethods
{
    private const string LibC = "libc";

    internal const int OpenReadWrite = 0x0002;
    internal const int OpenCloseOnExec = 0x80000;

    // linux/i2c-dev.h and linux/i2c.h
    internal const nuint I2CReadWrite = 0x0707;
    internal const ushort I2CMessageRead = 0x0001;

    [StructLayout(LayoutKind.Sequential)]
    internal struct I2CMessage
    {
        public ushort Address;
        public ushort Flags;
        public ushort Length;
        public byte* Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct I2CReadWriteData
    {
        public I2CMessage* Messages;
        public uint Count;
    }

    [LibraryImport(LibC, EntryPoint = "open", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int Open(string path, int flags);

    [LibraryImport(LibC, EntryPoint = "close", SetLastError = true)]
    internal static partial int Close(int fileDescriptor);

    [LibraryImport(LibC, EntryPoint = "ioctl", SetLastError = true)]
    internal static partial int Ioctl(FileDescriptorHandle fileDescriptor, nuint request, void* argument);

    [LibraryImport(LibC, EntryPoint = "realpath", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint RealPath(string path, nint resolvedPath);

    [LibraryImport(LibC, EntryPoint = "free")]
    private static partial void Free(nint pointer);

    /// <summary>Resolves every symbolic link and <c>..</c> in a path, or returns null if it does not exist.</summary>
    internal static string? ResolvePath(string path)
    {
        var resolved = RealPath(path, 0);
        if (resolved == 0)
        {
            return null;
        }
        try
        {
            return Marshal.PtrToStringUTF8(resolved);
        }
        finally
        {
            Free(resolved);
        }
    }

    internal static string DescribeError(int errno) => $"{Marshal.GetPInvokeErrorMessage(errno)} (errno {errno})";
}

internal sealed class FileDescriptorHandle : SafeHandle
{
    public FileDescriptorHandle() : base(-1, ownsHandle: true)
    {
    }

    public FileDescriptorHandle(int fileDescriptor) : this()
    {
        SetHandle(fileDescriptor);
    }

    public override bool IsInvalid => handle < 0;

    protected override bool ReleaseHandle() => NativeMethods.Close((int)handle) == 0;
}
