namespace DDCSharp.Core;

/// <summary>
/// Known monitor input source codes (subset of MCCS values).
/// </summary>
public enum InputSource : byte
{
    /// <summary>Unknown / unsupported.</summary>
    Unknown = 0x00,

    // Analog
    /// <summary>VGA 1.</summary>
    VGA1 = 0x01,
    /// <summary>VGA 2.</summary>
    VGA2 = 0x02,

    // Digital DVI
    /// <summary>DVI 1.</summary>
    DVI1 = 0x03,
    /// <summary>DVI 2.</summary>
    DVI2 = 0x04,

    // DisplayPort
    /// <summary>DisplayPort 1.</summary>
    DisplayPort1 = 0x0F,
    /// <summary>DisplayPort 2.</summary>
    DisplayPort2 = 0x10,

    // HDMI
    /// <summary>HDMI 1.</summary>
    HDMI1 = 0x11,
    /// <summary>HDMI 2.</summary>
    HDMI2 = 0x12,
    /// <summary>HDMI 3.</summary>
    HDMI3 = 0x13,
    /// <summary>HDMI 4.</summary>
    HDMI4 = 0x14,

    /// <summary>USB-C / DisplayPort alternate mode.</summary>
    USB_C = 0x1B
}