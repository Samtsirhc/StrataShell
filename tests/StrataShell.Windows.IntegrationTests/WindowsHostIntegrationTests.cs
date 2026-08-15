using System.IO;
using Microsoft.Win32;
using StrataShell.Windows.Apps;
using StrataShell.Windows.Input;
using StrataShell.Windows.Lifecycle;

namespace StrataShell.Windows.IntegrationTests;

public sealed class WindowsHostIntegrationTests
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "StrataShell";

    [Fact]
    public void StartupManager_EnableDisable_RoundTripsWithoutElevation()
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        object? previousValue = key.GetValue(RunValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        RegistryValueKind? previousKind = previousValue is null ? null : key.GetValueKind(RunValueName);
        string executable = Path.Combine(Path.GetTempPath(), "StrataShell QA.exe");

        try
        {
            StartupManager.SetEnabled(true, executable);
            Assert.True(StartupManager.IsEnabled());
            Assert.Equal($"\"{Path.GetFullPath(executable)}\" --background", key.GetValue(RunValueName));

            StartupManager.SetEnabled(false, executable);
            Assert.False(StartupManager.IsEnabled());
        }
        finally
        {
            if (previousValue is null)
            {
                key.DeleteValue(RunValueName, throwOnMissingValue: false);
            }
            else
            {
                key.SetValue(RunValueName, previousValue, previousKind!.Value);
            }
        }
    }

    [Fact]
    public void StartMenuCatalog_DiscoversUniqueExistingShortcuts()
    {
        var shortcuts = StartMenuCatalog.GetShortcuts();

        Assert.NotEmpty(shortcuts);
        Assert.All(shortcuts, shortcut => Assert.True(File.Exists(shortcut.Path), shortcut.Path));
        Assert.Equal(shortcuts.Count,
            shortcuts.Select(shortcut => shortcut.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void WindowsKeyInterceptor_CanInstallAndRemoveNativeHook()
    {
        using WindowsKeyInterceptor interceptor = new();

        interceptor.Enable();
        Assert.True(interceptor.IsEnabled);

        interceptor.Disable();
        Assert.False(interceptor.IsEnabled);
    }
}
