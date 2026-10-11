namespace DDCSharp.Core.Capabilities;

/// <summary>
/// MCCS / VESA VCP feature codes from the standard range (0x01-0xDF).
/// Codes 0xE0-0xFF are manufacturer specific, so their meaning depends on the vendor and model.
/// </summary>
public enum VCPFeature : byte
{
    /// <summary>Unknown or not in the standard range.</summary>
    Unknown = 0x00,

    /// <summary>Degauss a CRT.</summary>
    Degauss = 0x01,
    /// <summary>New control value.</summary>
    NewControlValue = 0x02,
    /// <summary>Soft controls.</summary>
    SoftControls = 0x03,
    /// <summary>Restore full factory defaults.</summary>
    RestoreFactoryDefaults = 0x04,
    /// <summary>Restore factory defaults for luminance and contrast.</summary>
    RestoreFactoryLuminanceContrastDefaults = 0x05,
    /// <summary>Restore factory geometry defaults.</summary>
    RestoreFactoryGeometryDefaults = 0x06,
    /// <summary>Restore factory color defaults.</summary>
    RestoreFactoryColorDefaults = 0x08,
    /// <summary>Restore factory TV defaults.</summary>
    RestoreFactoryTvDefaults = 0x0A,
    /// <summary>Color temperature increment, in kelvin.</summary>
    ColorTemperatureIncrement = 0x0B,
    /// <summary>Color temperature request (3000 K plus value times the increment).</summary>
    ColorTemperatureRequest = 0x0C,
    /// <summary>Sampling clock frequency.</summary>
    Clock = 0x0E,

    /// <summary>Brightness (luminance).</summary>
    Brightness = 0x10,
    /// <summary>Flesh tone enhancement.</summary>
    FleshToneEnhancement = 0x11,
    /// <summary>Contrast.</summary>
    Contrast = 0x12,
    /// <summary>Backlight control (deprecated in MCCS 2.2).</summary>
    BacklightControl = 0x13,
    /// <summary>Select color preset.</summary>
    SelectColorPreset = 0x14,
    /// <summary>Red video gain.</summary>
    RedVideoGain = 0x16,
    /// <summary>User color vision compensation.</summary>
    UserColorVisionCompensation = 0x17,
    /// <summary>Green video gain.</summary>
    GreenVideoGain = 0x18,
    /// <summary>Blue video gain.</summary>
    BlueVideoGain = 0x1A,
    /// <summary>Focus.</summary>
    Focus = 0x1C,
    /// <summary>Auto setup.</summary>
    AutoSetup = 0x1E,
    /// <summary>Auto color setup.</summary>
    AutoColorSetup = 0x1F,

    /// <summary>Horizontal position (phase).</summary>
    HorizontalPosition = 0x20,
    /// <summary>Horizontal size.</summary>
    HorizontalSize = 0x22,
    /// <summary>Horizontal pincushion.</summary>
    HorizontalPincushion = 0x24,
    /// <summary>Horizontal pincushion balance.</summary>
    HorizontalPincushionBalance = 0x26,
    /// <summary>Horizontal convergence, red/blue.</summary>
    HorizontalConvergenceRedBlue = 0x28,
    /// <summary>Horizontal convergence, magenta/green.</summary>
    HorizontalConvergenceMagentaGreen = 0x29,
    /// <summary>Horizontal linearity.</summary>
    HorizontalLinearity = 0x2A,
    /// <summary>Horizontal linearity balance.</summary>
    HorizontalLinearityBalance = 0x2C,
    /// <summary>Gray scale expansion.</summary>
    GrayScaleExpansion = 0x2E,

    /// <summary>Vertical position (phase).</summary>
    VerticalPosition = 0x30,
    /// <summary>Vertical size.</summary>
    VerticalSize = 0x32,
    /// <summary>Vertical pincushion.</summary>
    VerticalPincushion = 0x34,
    /// <summary>Vertical pincushion balance.</summary>
    VerticalPincushionBalance = 0x36,
    /// <summary>Vertical convergence, red/blue.</summary>
    VerticalConvergenceRedBlue = 0x38,
    /// <summary>Vertical convergence, magenta/green.</summary>
    VerticalConvergenceMagentaGreen = 0x39,
    /// <summary>Vertical linearity.</summary>
    VerticalLinearity = 0x3A,
    /// <summary>Vertical linearity balance.</summary>
    VerticalLinearityBalance = 0x3C,
    /// <summary>Clock phase.</summary>
    ClockPhase = 0x3E,

    /// <summary>Horizontal parallelogram.</summary>
    HorizontalParallelogram = 0x40,
    /// <summary>Vertical parallelogram.</summary>
    VerticalParallelogram = 0x41,
    /// <summary>Horizontal keystone.</summary>
    HorizontalKeystone = 0x42,
    /// <summary>Vertical keystone.</summary>
    VerticalKeystone = 0x43,
    /// <summary>Rotation (tilt).</summary>
    Rotation = 0x44,
    /// <summary>Top corner flare.</summary>
    TopCornerFlare = 0x46,
    /// <summary>Top corner hook.</summary>
    TopCornerHook = 0x48,
    /// <summary>Bottom corner flare.</summary>
    BottomCornerFlare = 0x4A,
    /// <summary>Bottom corner hook.</summary>
    BottomCornerHook = 0x4C,

    /// <summary>Active control.</summary>
    ActiveControl = 0x52,
    /// <summary>Performance preservation.</summary>
    PerformancePreservation = 0x54,
    /// <summary>Horizontal moire.</summary>
    HorizontalMoire = 0x56,
    /// <summary>Vertical moire.</summary>
    VerticalMoire = 0x58,
    /// <summary>Six-axis saturation, red.</summary>
    SixAxisSaturationRed = 0x59,
    /// <summary>Six-axis saturation, yellow.</summary>
    SixAxisSaturationYellow = 0x5A,
    /// <summary>Six-axis saturation, green.</summary>
    SixAxisSaturationGreen = 0x5B,
    /// <summary>Six-axis saturation, cyan.</summary>
    SixAxisSaturationCyan = 0x5C,
    /// <summary>Six-axis saturation, blue.</summary>
    SixAxisSaturationBlue = 0x5D,
    /// <summary>Six-axis saturation, magenta.</summary>
    SixAxisSaturationMagenta = 0x5E,

    /// <summary>Input source.</summary>
    InputSource = 0x60,
    /// <summary>Speaker volume.</summary>
    SpeakerVolume = 0x62,
    /// <summary>Speaker select.</summary>
    SpeakerSelect = 0x63,
    /// <summary>Microphone volume.</summary>
    MicrophoneVolume = 0x64,
    /// <summary>Ambient light sensor.</summary>
    AmbientLightSensor = 0x66,
    /// <summary>White backlight level.</summary>
    WhiteBacklightLevel = 0x6B,
    /// <summary>Red video black level.</summary>
    RedVideoBlackLevel = 0x6C,
    /// <summary>Red backlight level.</summary>
    RedBacklightLevel = 0x6D,
    /// <summary>Green video black level.</summary>
    GreenVideoBlackLevel = 0x6E,
    /// <summary>Green backlight level.</summary>
    GreenBacklightLevel = 0x6F,
    /// <summary>Blue video black level.</summary>
    BlueVideoBlackLevel = 0x70,
    /// <summary>Blue backlight level.</summary>
    BlueBacklightLevel = 0x71,
    /// <summary>Gamma.</summary>
    Gamma = 0x72,
    /// <summary>Lookup table size.</summary>
    LutSize = 0x73,
    /// <summary>Single point lookup table operation.</summary>
    SinglePointLutOperation = 0x74,
    /// <summary>Block lookup table operation.</summary>
    BlockLutOperation = 0x75,
    /// <summary>Remote procedure call.</summary>
    RemoteProcedureCall = 0x76,
    /// <summary>Display identification (EDID) operation.</summary>
    DisplayIdentificationOperation = 0x78,
    /// <summary>Adjust focal plane (MCCS 2.0 only).</summary>
    AdjustFocalPlane = 0x7A,
    /// <summary>Adjust zoom.</summary>
    AdjustZoom = 0x7C,
    /// <summary>Trapezoid (MCCS 2.0 only).</summary>
    Trapezoid = 0x7E,

    /// <summary>Keystone (MCCS 2.0 only).</summary>
    Keystone = 0x80,
    /// <summary>Horizontal mirror (flip).</summary>
    HorizontalMirror = 0x82,
    /// <summary>Vertical mirror (flip).</summary>
    VerticalMirror = 0x84,
    /// <summary>Display scaling.</summary>
    DisplayScaling = 0x86,
    /// <summary>Sharpness.</summary>
    Sharpness = 0x87,
    /// <summary>Velocity scan modulation.</summary>
    VelocityScanModulation = 0x88,
    /// <summary>TV color saturation.</summary>
    TvColorSaturation = 0x8A,
    /// <summary>TV channel up/down.</summary>
    TvChannelUpDown = 0x8B,
    /// <summary>TV sharpness.</summary>
    TvSharpness = 0x8C,
    /// <summary>Audio mute (and screen blank in MCCS 2.2).</summary>
    AudioMute = 0x8D,
    /// <summary>TV contrast.</summary>
    TvContrast = 0x8E,
    /// <summary>Audio treble.</summary>
    AudioTreble = 0x8F,

    /// <summary>Hue.</summary>
    Hue = 0x90,
    /// <summary>Audio bass.</summary>
    AudioBass = 0x91,
    /// <summary>TV black level.</summary>
    TvBlackLevel = 0x92,
    /// <summary>Audio balance, left/right.</summary>
    AudioBalance = 0x93,
    /// <summary>Audio processor mode.</summary>
    AudioProcessorMode = 0x94,
    /// <summary>Window position, top left X.</summary>
    WindowPositionTopLeftX = 0x95,
    /// <summary>Window position, top left Y.</summary>
    WindowPositionTopLeftY = 0x96,
    /// <summary>Window position, bottom right X.</summary>
    WindowPositionBottomRightX = 0x97,
    /// <summary>Window position, bottom right Y.</summary>
    WindowPositionBottomRightY = 0x98,
    /// <summary>Window control on/off.</summary>
    WindowControl = 0x99,
    /// <summary>Window background.</summary>
    WindowBackground = 0x9A,
    /// <summary>Six-axis hue control, red.</summary>
    SixAxisHueRed = 0x9B,
    /// <summary>Six-axis hue control, yellow.</summary>
    SixAxisHueYellow = 0x9C,
    /// <summary>Six-axis hue control, green.</summary>
    SixAxisHueGreen = 0x9D,
    /// <summary>Six-axis hue control, cyan.</summary>
    SixAxisHueCyan = 0x9E,
    /// <summary>Six-axis hue control, blue.</summary>
    SixAxisHueBlue = 0x9F,
    /// <summary>Six-axis hue control, magenta.</summary>
    SixAxisHueMagenta = 0xA0,

    /// <summary>Auto setup on/off.</summary>
    AutoSetupOnOff = 0xA2,
    /// <summary>Window mask control.</summary>
    WindowMaskControl = 0xA4,
    /// <summary>Window select.</summary>
    WindowSelect = 0xA5,
    /// <summary>Screen orientation.</summary>
    ScreenOrientation = 0xAA,
    /// <summary>Horizontal frequency.</summary>
    HorizontalFrequency = 0xAC,
    /// <summary>Vertical frequency.</summary>
    VerticalFrequency = 0xAE,

    /// <summary>Store or restore the user saved values for the current mode.</summary>
    StoreRestoreSettings = 0xB0,
    /// <summary>Flat panel sub pixel layout.</summary>
    FlatPanelSubPixelLayout = 0xB2,
    /// <summary>Source timing mode.</summary>
    SourceTimingMode = 0xB4,
    /// <summary>Display technology type.</summary>
    DisplayTechnologyType = 0xB6,
    /// <summary>Monitor status (DPVL).</summary>
    MonitorStatus = 0xB7,
    /// <summary>Packet count (DPVL).</summary>
    PacketCount = 0xB8,
    /// <summary>Monitor X origin (DPVL).</summary>
    MonitorXOrigin = 0xB9,
    /// <summary>Monitor Y origin (DPVL).</summary>
    MonitorYOrigin = 0xBA,
    /// <summary>Header error count (DPVL).</summary>
    HeaderErrorCount = 0xBB,
    /// <summary>Body CRC error count (DPVL).</summary>
    BodyCrcErrorCount = 0xBC,
    /// <summary>Client ID (DPVL).</summary>
    ClientId = 0xBD,
    /// <summary>Link control (DPVL).</summary>
    LinkControl = 0xBE,

    /// <summary>Display usage time, in hours.</summary>
    DisplayUsageTime = 0xC0,
    /// <summary>Display descriptor length.</summary>
    DisplayDescriptorLength = 0xC2,
    /// <summary>Transmit display descriptor.</summary>
    TransmitDisplayDescriptor = 0xC3,
    /// <summary>Enable display of the display descriptor.</summary>
    EnableDisplayDescriptor = 0xC4,
    /// <summary>Application enable key.</summary>
    ApplicationEnableKey = 0xC6,
    /// <summary>Display controller type (controller manufacturer in the low byte).</summary>
    DisplayControllerId = 0xC8,
    /// <summary>Display firmware level.</summary>
    DisplayFirmwareLevel = 0xC9,
    /// <summary>OSD / button level control.</summary>
    OsdButtonLevelControl = 0xCA,
    /// <summary>OSD language.</summary>
    OSDLanguage = 0xCC,
    /// <summary>Status indicators.</summary>
    StatusIndicators = 0xCD,
    /// <summary>Auxiliary display size.</summary>
    AuxiliaryDisplaySize = 0xCE,
    /// <summary>Auxiliary display data.</summary>
    AuxiliaryDisplayData = 0xCF,

    /// <summary>Output select.</summary>
    OutputSelect = 0xD0,
    /// <summary>Asset tag.</summary>
    AssetTag = 0xD2,
    /// <summary>Stereo video mode.</summary>
    StereoVideoMode = 0xD4,
    /// <summary>Power mode.</summary>
    PowerMode = 0xD6,
    /// <summary>Auxiliary power output.</summary>
    AuxiliaryPowerOutput = 0xD7,
    /// <summary>Scan mode.</summary>
    ScanMode = 0xDA,
    /// <summary>Image mode.</summary>
    ImageMode = 0xDB,
    /// <summary>Display application (display mode in MCCS 2.0).</summary>
    DisplayApplication = 0xDC,
    /// <summary>Scratch pad.</summary>
    ScratchPad = 0xDE,
    /// <summary>MCCS version.</summary>
    Version = 0xDF,

    /// <summary>Start of the manufacturer specific range.</summary>
    ManufacturerSpecificStart = 0xE0,
    /// <summary>End of the manufacturer specific range.</summary>
    ManufacturerSpecificEnd = 0xFF,
}