using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DDCSharp.Windows;

internal static unsafe partial class NativeMethods
{
    private const string Dxva2 = "dxva2.dll";
    private const string User32 = "user32.dll";

    // MC_VCP_CODE_TYPE
    internal const uint MCMomentary = 0;

    internal const uint EDDGetDeviceInterfaceName = 0x00000001;
    internal const uint DisplayDeviceActive = 0x00000001;

    internal const int ErrorGraphicsDDCCIVCPNotSupported = unchecked((int)0xC0262584);

    [StructLayout(LayoutKind.Sequential)]
    internal struct PhysicalMonitor
    {
        public nint Handle;
        public fixed char Description[128];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MonitorInfoEx
    {
        public uint Size;
        public Rect Monitor;
        public Rect WorkArea;
        public uint Flags;
        public fixed char DeviceName[32];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DisplayDevice
    {
        public uint Size;
        public fixed char DeviceName[32];
        public fixed char DeviceString[128];
        public uint StateFlags;
        public fixed char DeviceId[128];
        public fixed char DeviceKey[128];
    }

    [LibraryImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumDisplayMonitors(
        nint hdc,
        nint clip,
        delegate* unmanaged<nint, nint, Rect*, nint, int> callback,
        nint data);

    [LibraryImport(User32, EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMonitorInfo(nint monitor, MonitorInfoEx* info);

    [LibraryImport(User32, EntryPoint = "EnumDisplayDevicesW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumDisplayDevices(string? device, uint index, DisplayDevice* displayDevice, uint flags);

    [LibraryImport(Dxva2, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetNumberOfPhysicalMonitorsFromHMONITOR(nint monitor, out uint count);

    [LibraryImport(Dxva2, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetPhysicalMonitorsFromHMONITOR(nint monitor, uint count, PhysicalMonitor* monitors);

    [LibraryImport(Dxva2, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyPhysicalMonitor(nint physicalMonitor);

    [LibraryImport(Dxva2, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetVCPFeatureAndVCPFeatureReply(
        PhysicalMonitorHandle physicalMonitor,
        byte code,
        out uint codeType,
        out uint currentValue,
        out uint maximumValue);

    [LibraryImport(Dxva2, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetVCPFeature(PhysicalMonitorHandle physicalMonitor, byte code, uint value);

    [LibraryImport(Dxva2, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetCapabilitiesStringLength(PhysicalMonitorHandle physicalMonitor, out uint length);

    [LibraryImport(Dxva2, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CapabilitiesRequestAndCapabilitiesReply(
        PhysicalMonitorHandle physicalMonitor,
        byte* buffer,
        uint length);

    internal static List<nint> EnumerateMonitors()
    {
        var monitors = new List<nint>();
        var handle = GCHandle.Alloc(monitors);
        try
        {
            EnumDisplayMonitors(0, 0, &CollectMonitor, GCHandle.ToIntPtr(handle));
        }
        finally
        {
            handle.Free();
        }
        return monitors;
    }

    [UnmanagedCallersOnly]
    private static int CollectMonitor(nint monitor, nint hdc, Rect* rect, nint data)
    {
        ((List<nint>)GCHandle.FromIntPtr(data).Target!).Add(monitor);
        return 1;
    }

    /// <summary>Returns the physical monitors behind a logical monitor as (handle, description) pairs.</summary>
    internal static List<(nint Handle, string Description)> GetPhysicalMonitors(nint monitor)
    {
        if (!GetNumberOfPhysicalMonitorsFromHMONITOR(monitor, out var count) || count == 0)
        {
            return [];
        }

        var buffer = new PhysicalMonitor[count];
        fixed (PhysicalMonitor* pointer = buffer)
        {
            if (!GetPhysicalMonitorsFromHMONITOR(monitor, count, pointer))
            {
                return [];
            }

            var result = new List<(nint, string)>((int)count);
            for (var i = 0; i < count; i++)
            {
                result.Add((pointer[i].Handle, new string(pointer[i].Description).Trim()));
            }
            return result;
        }
    }

    /// <summary>Returns the GDI device name of a logical monitor, e.g. <c>\\.\DISPLAY1</c>.</summary>
    internal static string? GetMonitorDeviceName(nint monitor)
    {
        var info = new MonitorInfoEx { Size = (uint)sizeof(MonitorInfoEx) };
        return GetMonitorInfo(monitor, &info) ? new string(info.DeviceName) : null;
    }

    /// <summary>
    /// Returns the device interface paths of the active monitors attached to a GDI display device, e.g.
    /// <c>\\?\DISPLAY#DELF167#5&amp;2a8b4c3e&amp;0&amp;UID4352#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}</c>.
    /// </summary>
    internal static List<string> GetMonitorInterfacePaths(string deviceName)
    {
        var paths = new List<string>();
        for (uint index = 0; ; index++)
        {
            var device = new DisplayDevice { Size = (uint)sizeof(DisplayDevice) };
            if (!EnumDisplayDevices(deviceName, index, &device, EDDGetDeviceInterfaceName))
            {
                break;
            }
            if ((device.StateFlags & DisplayDeviceActive) != 0)
            {
                paths.Add(new string(device.DeviceId));
            }
        }
        return paths;
    }

    internal static string DescribeError(int error)
    {
        var message = new Win32Exception(error).Message;
        return $"Win32 error 0x{error:X8}: {message}";
    }
}
