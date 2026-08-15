using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Windows;
using StrataShell.Core.Configuration;
using StrataShell.Windows.Input;
using StrataShell.Windows.Lifecycle;
using StrataShell.Windows.Shell;
using Forms = System.Windows.Forms;

namespace StrataShell.App;

/// <summary>Owns the panel, settings, tray icon, keyboard hook, and taskbar runtime.</summary>
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
    Justification = "WPF Application owns process-lifetime resources and deterministically disposes them in OnExit.")]
public partial class App : System.Windows.Application
{
    private const string MutexName = @"Local\StrataShell.Application";
    private Mutex? instanceMutex;
    private JsonSettingsStore? settingsStore;
    private StrataSettings settings = new();
    private MainWindow? settingsWindow;
    private PanelWindow? panelWindow;
    private Forms.NotifyIcon? trayIcon;
    private WindowsKeyInterceptor? keyInterceptor;
    private ShellRuntime? shellRuntime;
    private TaskbarWindow? taskbarWindow;
    private Process? watchdogProcess;
    private string? diagnosticsPath;
    private string runtimeStatus = "Ready.";
    private bool runtimeStatusIsError;
    private Forms.ToolStripMenuItem? panelEnabledMenuItem;
    private Forms.ToolStripMenuItem? startupMenuItem;
    private bool restartRequested;

    /// <summary>Gets the active configuration snapshot.</summary>
    public StrataSettings Settings => settings;

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        instanceMutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            System.Windows.MessageBox.Show("StrataShell is already running.", "StrataShell",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        string settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StrataShell", "settings.json");
        settingsStore = new JsonSettingsStore(settingsPath);
        diagnosticsPath = Path.Combine(Path.GetDirectoryName(settingsPath)!, "diagnostics.log");
        WriteDiagnostic("INFO", $"Starting StrataShell {typeof(App).Assembly.GetName().Version} on {Environment.OSVersion}.");
        try
        {
            settings = await settingsStore.LoadAsync();
        }
        catch (Exception exception)
        {
            System.Windows.MessageBox.Show($"Settings could not be loaded. Safe defaults will be used.\n\n{exception.Message}",
                "StrataShell recovery", MessageBoxButton.OK, MessageBoxImage.Warning);
            settings = new StrataSettings();
        }
        try
        {
            await settingsStore.SaveAsync(settings);
        }
        catch (Exception exception)
        {
            WriteDiagnostic("ERROR", "Normalized settings could not be persisted during startup.", exception);
        }

        CreateTrayIcon();
        ConfigureInputHook();
        ConfigureTaskbar();

        bool background = e.Args.Any(arg => string.Equals(arg, "--background", StringComparison.OrdinalIgnoreCase));
        bool panelPrimary = e.Args.Any(arg => string.Equals(arg, "--panel-primary", StringComparison.OrdinalIgnoreCase));
        string? searchQuery = e.Args
            .FirstOrDefault(arg => arg.StartsWith("--search=", StringComparison.OrdinalIgnoreCase))?
            ["--search=".Length..];
        if (panelPrimary)
        {
            panelWindow ??= CreatePanelWindow();
            panelWindow.ApplySettings(settings.Panel);
            panelWindow.ShowPanel(usePrimaryScreen: true);
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                panelWindow.SetSearchQuery(searchQuery);
            }
        }
        else if (!background || !settings.Lifecycle.StartQuietly)
        {
            ShowSettings();
        }
    }

    /// <summary>Shows or hides the full-screen application panel.</summary>
    public void TogglePanel()
    {
        if (!settings.Panel.Enabled)
        {
            ShowTrayMessage("Panel is disabled", "Enable it in StrataShell settings.");
            return;
        }

        if (panelWindow is { IsVisible: true })
        {
            panelWindow.HidePanel();
            return;
        }

        panelWindow ??= CreatePanelWindow();
        panelWindow.ApplySettings(settings.Panel);
        panelWindow.ShowPanel();
    }

    /// <summary>Shows the configuration window.</summary>
    public void ShowSettings()
    {
        if (settingsWindow is null)
        {
            settingsWindow = new MainWindow(settings, settingsStore!.FilePath);
            settingsWindow.SettingsApplied += OnSettingsApplied;
            settingsWindow.PreviewRequested += (_, _) => TogglePanel();
        }

        settingsWindow.UpdateSettings(settings);
        settingsWindow.Show();
        settingsWindow.WindowState = WindowState.Normal;
        settingsWindow.Activate();
    }

    private PanelWindow CreatePanelWindow()
    {
        PanelWindow window = new(settings.Panel);
        window.SettingsRequested += (_, _) => ShowSettings();
        window.ApplyPinnedPaths(settings.Taskbar.QuickLaunchPaths);
        window.QuickLaunchToggleRequested += path => _ = ToggleQuickLaunchAsync(path);
        return window;
    }

    private async Task ToggleQuickLaunchAsync(string path)
    {
        HashSet<string> pinned = new(settings.Taskbar.QuickLaunchPaths, StringComparer.OrdinalIgnoreCase);
        if (!pinned.Add(path))
        {
            pinned.Remove(path);
        }

        await ApplyTraySettingsSafelyAsync(settings with
        {
            Taskbar = settings.Taskbar with { QuickLaunchPaths = [.. pinned] },
        });
    }

    private async void OnSettingsApplied(object? sender, StrataSettings newSettings)
    {
        try
        {
            await ApplySettingsAsync(newSettings);
            settingsWindow?.ShowStatus(runtimeStatusIsError ? runtimeStatus : "Saved and applied.", runtimeStatusIsError);
        }
        catch (Exception exception)
        {
            settingsWindow?.ShowStatus($"Could not apply settings: {exception.Message}", isError: true);
        }
    }

    private async Task ApplySettingsAsync(StrataSettings newSettings)
    {
        settings = SettingsNormalizer.Normalize(newSettings);
        await settingsStore!.SaveAsync(settings);
        string executable = Environment.ProcessPath
            ?? Path.Combine(AppContext.BaseDirectory, "StrataShell.exe");
        StartupManager.SetEnabled(settings.Lifecycle.RunAtStartup, executable);
        ConfigureInputHook();
        ConfigureTaskbar();
        panelWindow?.ApplySettings(settings.Panel);
        panelWindow?.ApplyPinnedPaths(settings.Taskbar.QuickLaunchPaths);
        settingsWindow?.UpdateSettings(settings);
        UpdateTrayState();
    }

    private void ConfigureInputHook()
    {
        keyInterceptor?.Dispose();
        keyInterceptor = null;
        if (!settings.Panel.Enabled || !settings.Panel.ToggleWithWindowsKey)
        {
            return;
        }

        try
        {
            keyInterceptor = new WindowsKeyInterceptor();
            keyInterceptor.ToggleRequested += (_, _) => TogglePanel();
            keyInterceptor.Enable();
        }
        catch (Exception exception)
        {
            keyInterceptor?.Dispose();
            keyInterceptor = null;
            ShowTrayMessage("Windows-key hook unavailable", exception.Message);
            WriteDiagnostic("ERROR", "Windows-key hook initialization failed.", exception);
        }
    }

    private void ConfigureTaskbar()
    {
        StopTaskbar();
        runtimeStatus = "Ready.";
        runtimeStatusIsError = false;
        if (!settings.Taskbar.Enabled)
        {
            WriteDiagnostic("INFO", "Explorer taskbar retained.");
            return;
        }

        if (!EnsureWatchdog())
        {
            runtimeStatus = "The custom taskbar was not started because its crash-recovery watchdog is unavailable.";
            runtimeStatusIsError = true;
            ShowTrayMessage("Custom taskbar stayed disabled", runtimeStatus);
            return;
        }

        try
        {
            WriteDiagnostic("INFO", $"Starting custom taskbar: height={settings.Taskbar.Height}, rows={settings.Taskbar.Rows}, iconSize={settings.Taskbar.IconSize}.");
            shellRuntime = new ShellRuntime();
            taskbarWindow = new TaskbarWindow(shellRuntime, settings.Taskbar);
            taskbarWindow.StartRequested += (_, _) => TogglePanel();
            taskbarWindow.ClockRequested += (_, _) => ShowSettings();
            taskbarWindow.Loaded += (_, _) => WriteDiagnostic("INFO",
                $"Custom taskbar loaded at left={taskbarWindow.Left}, top={taskbarWindow.Top}, width={taskbarWindow.ActualWidth}, height={taskbarWindow.ActualHeight}, visible={taskbarWindow.IsVisible}.");
            taskbarWindow.Show();
            WriteDiagnostic("INFO", "Custom taskbar window shown.");
        }
        catch (Exception exception)
        {
            StopTaskbar();
            runtimeStatus = $"Custom taskbar could not start; Explorer was restored. {exception.Message}";
            runtimeStatusIsError = true;
            ShowTrayMessage("Custom taskbar recovered safely", exception.Message);
            WriteDiagnostic("ERROR", "Custom taskbar initialization failed; Explorer recovery executed.", exception);
        }
    }

    private void StopTaskbar()
    {
        try
        {
            taskbarWindow?.Shutdown();
        }
        catch
        {
            // ShellRuntime.Dispose below is the recovery authority.
        }

        taskbarWindow = null;
        shellRuntime?.Dispose();
        shellRuntime = null;
    }

    private bool EnsureWatchdog()
    {
        if (watchdogProcess is { HasExited: false })
        {
            return true;
        }

        watchdogProcess?.Dispose();
        watchdogProcess = null;
        string watchdogPath = Path.Combine(AppContext.BaseDirectory, "StrataShell.Watchdog.exe");
        if (!File.Exists(watchdogPath))
        {
            WriteDiagnostic("ERROR", $"Crash-recovery watchdog is missing: {watchdogPath}");
            return false;
        }

        try
        {
            ProcessStartInfo startInfo = new(watchdogPath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            startInfo.ArgumentList.Add(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            watchdogProcess = Process.Start(startInfo);
            WriteDiagnostic("INFO", $"Crash-recovery watchdog started with pid={watchdogProcess?.Id}.");
            return watchdogProcess is not null;
        }
        catch (Exception exception)
        {
            WriteDiagnostic("ERROR", "Crash-recovery watchdog could not start.", exception);
            return false;
        }
    }

    private void CreateTrayIcon()
    {
        Forms.ContextMenuStrip menu = new();
        menu.Items.Add("Open panel", null, (_, _) => Dispatcher.Invoke(TogglePanel));
        menu.Items.Add("Settings", null, (_, _) => Dispatcher.Invoke(ShowSettings));
        panelEnabledMenuItem = new Forms.ToolStripMenuItem("Panel enabled") { CheckOnClick = true };
        panelEnabledMenuItem.Click += async (_, _) =>
        {
            Task operation = await Dispatcher.InvokeAsync(() => ApplyTraySettingsSafelyAsync(settings with
            {
                Panel = settings.Panel with { Enabled = panelEnabledMenuItem.Checked },
            }));
            await operation;
        };
        menu.Items.Add(panelEnabledMenuItem);
        startupMenuItem = new Forms.ToolStripMenuItem("Run at sign-in") { CheckOnClick = true };
        startupMenuItem.Click += async (_, _) =>
        {
            Task operation = await Dispatcher.InvokeAsync(() => ApplyTraySettingsSafelyAsync(settings with
            {
                Lifecycle = settings.Lifecycle with { RunAtStartup = startupMenuItem.Checked },
            }));
            await operation;
        };
        menu.Items.Add(startupMenuItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Open diagnostics", null, (_, _) => Dispatcher.Invoke(OpenDiagnostics));
        menu.Items.Add("Restart", null, (_, _) => Dispatcher.Invoke(RestartApplication));
        menu.Items.Add("Exit", null, (_, _) => Dispatcher.Invoke(ExitApplication));

        trayIcon = new Forms.NotifyIcon
        {
            Text = "StrataShell is running",
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = menu,
        };
        trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(TogglePanel);
        menu.Opening += (_, _) => UpdateTrayState();
        UpdateTrayState();
    }

    private async Task ApplyTraySettingsSafelyAsync(StrataSettings newSettings)
    {
        try
        {
            await ApplySettingsAsync(newSettings);
        }
        catch (Exception exception)
        {
            ShowTrayMessage("Settings could not be applied", exception.Message);
            WriteDiagnostic("ERROR", "Tray setting operation failed.", exception);
            UpdateTrayState();
        }
    }

    private void UpdateTrayState()
    {
        if (panelEnabledMenuItem is not null)
        {
            panelEnabledMenuItem.Checked = settings.Panel.Enabled;
        }
        if (startupMenuItem is not null)
        {
            startupMenuItem.Checked = settings.Lifecycle.RunAtStartup;
        }
        if (trayIcon is not null)
        {
            string panel = settings.Panel.Enabled ? "panel on" : "panel off";
            string taskbar = settings.Taskbar.Enabled && !runtimeStatusIsError ? "taskbar on" : "Explorer taskbar";
            trayIcon.Text = $"StrataShell — {panel}, {taskbar}";
        }
    }

    private void OpenDiagnostics()
    {
        WriteDiagnostic("INFO", "Diagnostics opened from the tray menu.");
        if (diagnosticsPath is null)
        {
            return;
        }

        Process.Start(new ProcessStartInfo("explorer.exe")
        {
            UseShellExecute = true,
            Arguments = $"/select,\"{diagnosticsPath}\"",
        });
    }

    private void RestartApplication()
    {
        restartRequested = true;
        ExitApplication();
    }

    private void ShowTrayMessage(string title, string message) =>
        trayIcon?.ShowBalloonTip(4000, title, message, Forms.ToolTipIcon.Warning);

    private void ExitApplication()
    {
        if (settingsWindow is not null)
        {
            settingsWindow.AllowClose = true;
            settingsWindow.Close();
        }

        Shutdown();
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        WriteDiagnostic("INFO", "StrataShell is exiting and restoring shell-owned UI.");
        keyInterceptor?.Dispose();
        StopTaskbar();
        panelWindow?.CloseImmediately();
        if (trayIcon is not null)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
        }
        watchdogProcess?.Dispose();

        if (instanceMutex is not null)
        {
            try { instanceMutex.ReleaseMutex(); } catch (ApplicationException) { }
            instanceMutex.Dispose();
        }

        base.OnExit(e);
        if (restartRequested)
        {
            string executable = Environment.ProcessPath
                ?? Path.Combine(AppContext.BaseDirectory, "StrataShell.exe");
            Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
        }
    }

    private void WriteDiagnostic(string level, string message, Exception? exception = null)
    {
        if (string.IsNullOrWhiteSpace(diagnosticsPath))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(diagnosticsPath)!);
            string detail = exception is null ? string.Empty : $"{Environment.NewLine}{exception}";
            File.AppendAllText(diagnosticsPath,
                $"{DateTimeOffset.Now:O} [{level}] {message}{detail}{Environment.NewLine}");
        }
        catch
        {
            // Diagnostics must never prevent shell recovery or shutdown.
        }
    }
}
