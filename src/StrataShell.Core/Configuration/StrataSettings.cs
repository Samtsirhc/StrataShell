namespace StrataShell.Core.Configuration;

/// <summary>
/// Versioned user configuration persisted by StrataShell.
/// </summary>
public sealed record StrataSettings
{
    /// <summary>The current on-disk schema version.</summary>
    public const int CurrentSchemaVersion = 2;

    /// <summary>Gets the schema version used to serialize this instance.</summary>
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>Gets panel behavior and appearance settings.</summary>
    public PanelSettings Panel { get; init; } = new();

    /// <summary>Gets taskbar behavior and layout settings.</summary>
    public TaskbarSettings Taskbar { get; init; } = new();

    /// <summary>Gets application lifecycle settings.</summary>
    public LifecycleSettings Lifecycle { get; init; } = new();
}

/// <summary>Full-screen panel settings.</summary>
public sealed record PanelSettings
{
    /// <summary>Gets whether the panel feature is enabled.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Gets whether a bare Windows-key gesture toggles the panel.</summary>
    public bool ToggleWithWindowsKey { get; init; } = true;

    /// <summary>Gets whether the panel shows recently used content.</summary>
    public bool ShowRecent { get; init; } = true;

    /// <summary>Gets the preferred tile width and height in logical pixels.</summary>
    public double TileSize { get; init; } = 108;

    /// <summary>Gets the panel background opacity.</summary>
    public double Opacity { get; init; } = 0.94;

    /// <summary>Gets the requested color mode.</summary>
    public ColorMode ColorMode { get; init; } = ColorMode.System;

    /// <summary>Gets the requested motion level.</summary>
    public MotionLevel Motion { get; init; } = MotionLevel.Full;
}

/// <summary>Custom taskbar settings.</summary>
public sealed record TaskbarSettings
{
    /// <summary>
    /// Gets whether the custom taskbar is enabled. It is disabled by default
    /// until the recovery path has been accepted and verified.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>Gets the taskbar height in logical pixels.</summary>
    public double Height { get; init; } = 96;

    /// <summary>Gets the number of task-button rows.</summary>
    public int Rows { get; init; } = 2;

    /// <summary>Gets the task and shortcut icon size in logical pixels.</summary>
    public double IconSize { get; init; } = 30;

    /// <summary>Gets whether the taskbar automatically hides.</summary>
    public bool AutoHide { get; init; }

    /// <summary>Gets whether the quick-launch area is shown.</summary>
    public bool ShowQuickLaunch { get; init; } = true;

    /// <summary>Gets whether the notification area is shown.</summary>
    public bool ShowNotificationArea { get; init; } = true;

    /// <summary>Gets the Start-menu shortcut paths pinned to quick launch.</summary>
    public string[] QuickLaunchPaths { get; init; } = [];
}

/// <summary>Application lifecycle settings.</summary>
public sealed record LifecycleSettings
{
    /// <summary>Gets whether StrataShell starts at user sign-in.</summary>
    public bool RunAtStartup { get; init; }

    /// <summary>Gets whether minimized startup suppresses the settings window.</summary>
    public bool StartQuietly { get; init; } = true;
}

/// <summary>Supported user color modes.</summary>
public enum ColorMode
{
    /// <summary>Follow the Windows application theme.</summary>
    System,

    /// <summary>Use the light palette.</summary>
    Light,

    /// <summary>Use the dark palette.</summary>
    Dark,
}

/// <summary>Supported animation levels.</summary>
public enum MotionLevel
{
    /// <summary>Disable non-essential motion.</summary>
    Reduced,

    /// <summary>Use the normal short, interruptible transitions.</summary>
    Full,
}
