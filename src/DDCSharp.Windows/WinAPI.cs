using System.Runtime.InteropServices;
using System.Text;

namespace DDCSharp.Windows;

internal static class WinAPI
{
    private const string Dxva2 = "dxva2.dll";
    private const string User32 = "user32.dll";

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct PHYSICAL_MONITOR
    {
        public nint hPhysicalMonitor;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szPhysicalMonitorDescription;
    }

    internal delegate bool MonitorEnumProc(nint hMonitor, nint hdc, ref RECT lprcMonitor, nint dwData);

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT { public int left, top, right, bottom; }

    [DllImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumDisplayMonitors(nint hdc, nint lprcClip, MonitorEnumProc lpfnEnum, nint dwData);

    [DllImport(Dxva2, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(nint hMonitor, out uint pdwNumberOfPhysicalMonitors);

    [DllImport(Dxva2, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetPhysicalMonitorsFromHMONITOR(nint hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport(Dxva2, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyPhysicalMonitor(nint hMonitor);

    [DllImport(Dxva2, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport(Dxva2, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetVCPFeatureAndVCPFeatureReply(
        nint hMonitor,
        byte bVCPCode,
        out uint pvct,
        out uint pdwCurrentValue,
        out uint pdwMaximumValue);

    [DllImport(Dxva2, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetVCPFeature(nint hMonitor, byte bVCPCode, uint dwNewValue);

    [DllImport(Dxva2, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCapabilitiesStringLength(nint hMonitor, out uint pdwCapabilitiesStringLengthInCharacters);

    [DllImport(Dxva2, SetLastError = true, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CapabilitiesRequestAndCapabilitiesReply(nint hMonitor, StringBuilder pszASCIICapabilitiesString, uint dwCapabilitiesStringLengthInCharacters);

    internal static PHYSICAL_MONITOR[] GetPhysicalMonitors(nint hMonitor)
    {
        if (!GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out var count) || count == 0)
            return [];
        var arr = new PHYSICAL_MONITOR[count];
        if (!GetPhysicalMonitorsFromHMONITOR(hMonitor, count, arr))
            return [];
        return arr;
    }

    internal static IEnumerable<nint> EnumerateHMonitors()
    {
        var list = new List<nint>();
        EnumDisplayMonitors(0, 0, (nint hMonitor, nint hdc, ref RECT r, nint data) => { list.Add(hMonitor); return true; }, 0);
        return list;
    }
}