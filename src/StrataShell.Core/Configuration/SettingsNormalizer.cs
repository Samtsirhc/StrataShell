namespace StrataShell.Core.Configuration;

/// <summary>
/// Converts untrusted or old settings into a bounded configuration that the UI
/// and shell integration can safely consume.
/// </summary>
public static class SettingsNormalizer
{
    /// <summary>Minimum supported panel tile size.</summary>
    public const double MinimumTileSize = 56;

    /// <summary>Maximum supported panel tile size.</summary>
    public const double MaximumTileSize = 144;

    /// <summary>Minimum supported taskbar height.</summary>
    public const double MinimumTaskbarHeight = 40;

    /// <summary>Maximum supported taskbar height.</summary>
    public const double MaximumTaskbarHeight = 240;

    /// <summary>Maximum supported number of taskbar rows.</summary>
    public const int MaximumTaskbarRows = 4;

    /// <summary>Normalizes every bounded field and updates the schema version.</summary>
    /// <param name="settings">Settings to normalize; null yields defaults.</param>
    /// <returns>A valid current-schema settings instance.</returns>
    public static StrataSettings Normalize(StrataSettings? settings)
    {
        settings ??= new StrataSettings();
        PanelSettings panel = settings.Panel ?? new PanelSettings();
        TaskbarSettings taskbar = settings.Taskbar ?? new TaskbarSettings();
        LifecycleSettings lifecycle = settings.Lifecycle ?? new LifecycleSettings();

        return settings with
        {
            SchemaVersion = StrataSettings.CurrentSchemaVersion,
            Panel = panel with
            {
                TileSize = ClampFinite(panel.TileSize, MinimumTileSize, MaximumTileSize, 108),
                Opacity = ClampFinite(panel.Opacity, 0.72, 1, 0.94),
            },
            Taskbar = taskbar with
            {
                Height = ClampFinite(taskbar.Height, MinimumTaskbarHeight, MaximumTaskbarHeight, 96),
                Rows = Math.Clamp(taskbar.Rows, 1, MaximumTaskbarRows),
                IconSize = ClampFinite(taskbar.IconSize, 16, 64, 30),
                QuickLaunchPaths = (taskbar.QuickLaunchPaths ?? [])
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(24)
                    .ToArray(),
            },
            Lifecycle = lifecycle,
        };
    }

    private static double ClampFinite(double value, double minimum, double maximum, double fallback)
    {
        return double.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : fallback;
    }
}
