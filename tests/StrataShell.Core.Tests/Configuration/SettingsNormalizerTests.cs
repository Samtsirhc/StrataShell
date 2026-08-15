using StrataShell.Core.Configuration;

namespace StrataShell.Core.Tests.Configuration;

public sealed class SettingsNormalizerTests
{
    [Fact]
    public void Normalize_Null_ReturnsSafeDefaults()
    {
        StrataSettings settings = SettingsNormalizer.Normalize(null);

        Assert.True(settings.Panel.Enabled);
        Assert.False(settings.Taskbar.Enabled);
        Assert.Equal(2, settings.Taskbar.Rows);
        Assert.Equal(StrataSettings.CurrentSchemaVersion, settings.SchemaVersion);
    }

    [Fact]
    public void Normalize_ClampsUnsafeLayoutValues()
    {
        StrataSettings input = new()
        {
            SchemaVersion = -4,
            Panel = new PanelSettings { TileSize = double.PositiveInfinity, Opacity = -10 },
            Taskbar = new TaskbarSettings
            {
                Height = 999,
                Rows = 99,
                IconSize = 4,
                QuickLaunchPaths = ["", "C:\\One.lnk", "c:\\one.lnk", "C:\\Two.lnk"],
            },
        };

        StrataSettings result = SettingsNormalizer.Normalize(input);

        Assert.Equal(108, result.Panel.TileSize);
        Assert.Equal(0.72, result.Panel.Opacity);
        Assert.Equal(SettingsNormalizer.MaximumTaskbarHeight, result.Taskbar.Height);
        Assert.Equal(SettingsNormalizer.MaximumTaskbarRows, result.Taskbar.Rows);
        Assert.Equal(16, result.Taskbar.IconSize);
        Assert.Equal(["C:\\One.lnk", "C:\\Two.lnk"], result.Taskbar.QuickLaunchPaths);
        Assert.Equal(StrataSettings.CurrentSchemaVersion, result.SchemaVersion);
    }

    [Fact]
    public void Normalize_PreservesFeatureChoices()
    {
        StrataSettings input = new()
        {
            Panel = new PanelSettings
            {
                Enabled = false,
                ColorMode = ColorMode.Light,
                Motion = MotionLevel.Reduced,
            },
            Taskbar = new TaskbarSettings { Enabled = true, AutoHide = true },
            Lifecycle = new LifecycleSettings { RunAtStartup = true },
        };

        StrataSettings result = SettingsNormalizer.Normalize(input);

        Assert.False(result.Panel.Enabled);
        Assert.Equal(ColorMode.Light, result.Panel.ColorMode);
        Assert.Equal(MotionLevel.Reduced, result.Panel.Motion);
        Assert.True(result.Taskbar.Enabled);
        Assert.True(result.Taskbar.AutoHide);
        Assert.True(result.Lifecycle.RunAtStartup);
    }
}
