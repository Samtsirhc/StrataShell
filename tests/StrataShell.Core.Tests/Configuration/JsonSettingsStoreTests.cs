using StrataShell.Core.Configuration;

namespace StrataShell.Core.Tests.Configuration;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "StrataShell.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task LoadAsync_MissingFile_ReturnsDefaults()
    {
        JsonSettingsStore store = new(Path.Combine(directory, "settings.json"));

        StrataSettings settings = await store.LoadAsync();

        Assert.True(settings.Panel.Enabled);
        Assert.False(settings.Taskbar.Enabled);
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsAndNormalizes()
    {
        JsonSettingsStore store = new(Path.Combine(directory, "settings.json"));
        StrataSettings input = new()
        {
            Panel = new PanelSettings { TileSize = 120, Opacity = 0.88 },
            Taskbar = new TaskbarSettings { Enabled = true, Height = 128, Rows = 3 },
            Lifecycle = new LifecycleSettings { RunAtStartup = true },
        };

        await store.SaveAsync(input);
        StrataSettings loaded = await store.LoadAsync();

        Assert.Equal(120, loaded.Panel.TileSize);
        Assert.Equal(0.88, loaded.Panel.Opacity);
        Assert.True(loaded.Taskbar.Enabled);
        Assert.Equal(128, loaded.Taskbar.Height);
        Assert.Equal(3, loaded.Taskbar.Rows);
        Assert.True(loaded.Lifecycle.RunAtStartup);
        Assert.False(File.Exists(store.FilePath + ".tmp"));
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }
}
