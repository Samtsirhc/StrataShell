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
        Assert.True(settings.Taskbar.ShowOnAllMonitors);
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

    [Fact]
    public async Task LoadAsync_CorruptJson_PreservesOriginalAndReturnsDefaults()
    {
        string path = Path.Combine(directory, "settings.json");
        Directory.CreateDirectory(directory);
        const string corruptContent = "{ this is not valid json";
        await File.WriteAllTextAsync(path, corruptContent);
        JsonSettingsStore store = new(path);

        StrataSettings settings = await store.LoadAsync();

        Assert.True(settings.Panel.Enabled);
        Assert.False(File.Exists(path));
        string backup = Assert.Single(Directory.GetFiles(directory, "settings.json.corrupt-*"));
        Assert.Equal(corruptContent, await File.ReadAllTextAsync(backup));
    }

    [Fact]
    public async Task SaveAsync_ConcurrentCalls_LeaveOneCompleteValidSnapshot()
    {
        JsonSettingsStore store = new(Path.Combine(directory, "settings.json"));
        Task[] saves = Enumerable.Range(1, 24)
            .Select(index => store.SaveAsync(new StrataSettings
            {
                Taskbar = new TaskbarSettings { Height = 40 + index, Rows = (index % 4) + 1 },
            }))
            .ToArray();

        await Task.WhenAll(saves);
        StrataSettings loaded = await store.LoadAsync();

        Assert.InRange(loaded.Taskbar.Height, 41, 64);
        Assert.InRange(loaded.Taskbar.Rows, 1, 4);
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
