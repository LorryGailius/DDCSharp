namespace DDCSharp.Core.Capabilities;

/// <summary>
/// Common MCCS / VESA VCP feature codes (subset).
/// </summary>
public enum VCPFeature : byte
{
    /// <summary>Unknown or not in the known subset.</summary>
    Unknown = 0x00,

    // Control values
    /// <summary>New control value.</summary>
    NewControlValue = 0x02,
    /// <summary>Restore full factory defaults.</summary>
    RestoreFactoryDefaults = 0x04,
    /// <summary>Restore factory defaults for luminance and contrast.</summary>
    RestoreFactoryLuminanceContrastDefaults = 0x05,
    /// <summary>Restore factory color defaults.</summary>
    RestoreFactoryColorDefaults = 0x08,

    // Image settings
    /// <summary>Brightness (luminance).</summary>
    Brightness = 0x10, // Luminance
    /// <summary>Contrast.</summary>
    Contrast = 0x12,
    /// <summary>Select color preset.</summary>
    SelectColorPreset = 0x14,
    /// <summary>Red video gain.</summary>
    RedVideoGain = 0x16,
    /// <summary>Green video gain.</summary>
    GreenVideoGain = 0x18,
    /// <summary>Blue video gain.</summary>
    BlueVideoGain = 0x1A,

    // Active control
    /// <summary>Active control.</summary>
    ActiveControl = 0x52,

    // Input / power / configuration
    /// <summary>Input source.</summary>
    InputSource = 0x60,
    /// <summary>Power mode.</summary>
    PowerMode = 0xD6,
    /// <summary>Display application.</summary>
    DisplayApplication = 0xDC,
    /// <summary>MCCS version.</summary>
    Version = 0xDF,

    // Sharpness
    /// <summary>Sharpness.</summary>
    Sharpness = 0x87,

    // Orientation and frequencies
    /// <summary>Screen orientation.</summary>
    ScreenOrientation = 0xAA,
    /// <summary>Horizontal frequency.</summary>
    HorizontalFrequency = 0xAC,
    /// <summary>Vertical frequency.</summary>
    VerticalFrequency = 0xAE,

    // Display panel information
    /// <summary>Flat panel sub pixel layout.</summary>
    FlatPanelSubPixelLayout = 0xB2,
    /// <summary>Display technology type.</summary>
    DisplayTechnologyType = 0xB6,
    /// <summary>Display usage time.</summary>
    DisplayUsageTime = 0xC6,
    /// <summary>Display controller ID.</summary>
    DisplayControllerId = 0xC8,
    /// <summary>Display firmware level.</summary>
    DisplayFirmwareLevel = 0xC9,

    // On-Screen Display (OSD)
    /// <summary>OSD button level control.</summary>
    OsdButtonLevelControl = 0xCA,
    /// <summary>OSD language.</summary>
    OSDLanguage = 0xCC,

    // Audio
    /// <summary>Speaker volume.</summary>
    SpeakerVolume = 0x62,
    /// <summary>Audio mute.</summary>
    AudioMute = 0x8D,

    // Geometry / color
    /// <summary>Red video black level.</summary>
    RedVideoBlackLevel = 0x6C,
    /// <summary>Green video black level.</summary>
    GreenVideoBlackLevel = 0x6E,
    /// <summary>Blue video black level.</summary>
    BlueVideoBlackLevel = 0x70,

    // Manufacturer specific range
    /// <summary>Start of manufacturer specific range.</summary>
    ManufacturerSpecificStart = 0xE0,
    /// <summary>End of manufacturer specific range.</summary>
    ManufacturerSpecificEnd = 0xFF,
}