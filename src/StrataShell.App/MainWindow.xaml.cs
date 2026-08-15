using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using StrataShell.Core.Configuration;

namespace StrataShell.App;

/// <summary>Configuration surface for all user-facing StrataShell features.</summary>
public partial class MainWindow : Window
{
    private StrataSettings settings;

    /// <summary>Creates the settings window.</summary>
    public MainWindow(StrataSettings settings, string settingsPath)
    {
        this.settings = settings;
        InitializeComponent();
        SettingsPathTextBox.Text = settingsPath;
        UpdateSettings(settings);
    }

    /// <summary>Raised when a validated settings snapshot should be persisted and applied.</summary>
    public event EventHandler<StrataSettings>? SettingsApplied;

    /// <summary>Raised when the panel preview is requested.</summary>
    public event EventHandler? PreviewRequested;

    /// <summary>Gets or sets whether the settings window may be destroyed.</summary>
    public bool AllowClose { get; set; }

    /// <summary>Copies a configuration snapshot into the controls.</summary>
    public void UpdateSettings(StrataSettings value)
    {
        settings = value;
        PanelEnabledCheckBox.IsChecked = value.Panel.Enabled;
        WindowsKeyCheckBox.IsChecked = value.Panel.ToggleWithWindowsKey;
        TileSizeSlider.Value = value.Panel.TileSize;
        PanelOpacitySlider.Value = value.Panel.Opacity;
        MotionComboBox.SelectedIndex = (int)value.Panel.Motion;

        TaskbarEnabledCheckBox.IsChecked = value.Taskbar.Enabled;
        AutoHideCheckBox.IsChecked = value.Taskbar.AutoHide;
        QuickLaunchCheckBox.IsChecked = value.Taskbar.ShowQuickLaunch;
        NotificationAreaCheckBox.IsChecked = value.Taskbar.ShowNotificationArea;
        TaskbarHeightSlider.Value = value.Taskbar.Height;
        TaskbarRowsSlider.Value = value.Taskbar.Rows;
        TaskbarIconSizeSlider.Value = value.Taskbar.IconSize;

        RunAtStartupCheckBox.IsChecked = value.Lifecycle.RunAtStartup;
        StartQuietlyCheckBox.IsChecked = value.Lifecycle.StartQuietly;
        UpdateSummary();
    }

    /// <summary>Shows a non-blocking operation result.</summary>
    public void ShowStatus(string message, bool isError)
    {
        StatusTextBlock.Text = message;
        StatusTextBlock.Foreground = new SolidColorBrush(
            isError ? System.Windows.Media.Color.FromRgb(255, 140, 140) : System.Windows.Media.Color.FromRgb(116, 231, 184));
    }

    private StrataSettings CollectSettings() => new()
    {
        Panel = new PanelSettings
        {
            Enabled = PanelEnabledCheckBox.IsChecked == true,
            ToggleWithWindowsKey = WindowsKeyCheckBox.IsChecked == true,
            ShowRecent = settings.Panel.ShowRecent,
            TileSize = TileSizeSlider.Value,
            Opacity = PanelOpacitySlider.Value,
            ColorMode = settings.Panel.ColorMode,
            Motion = (MotionLevel)Math.Max(0, MotionComboBox.SelectedIndex),
        },
        Taskbar = new TaskbarSettings
        {
            Enabled = TaskbarEnabledCheckBox.IsChecked == true,
            AutoHide = AutoHideCheckBox.IsChecked == true,
            ShowQuickLaunch = QuickLaunchCheckBox.IsChecked == true,
            ShowNotificationArea = NotificationAreaCheckBox.IsChecked == true,
            Height = TaskbarHeightSlider.Value,
            Rows = (int)Math.Round(TaskbarRowsSlider.Value),
            IconSize = TaskbarIconSizeSlider.Value,
            QuickLaunchPaths = settings.Taskbar.QuickLaunchPaths,
        },
        Lifecycle = new LifecycleSettings
        {
            RunAtStartup = RunAtStartupCheckBox.IsChecked == true,
            StartQuietly = StartQuietlyCheckBox.IsChecked == true,
        },
    };

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        settings = SettingsNormalizer.Normalize(CollectSettings());
        SettingsApplied?.Invoke(this, settings);
        UpdateSummary();
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e) =>
        PreviewRequested?.Invoke(this, EventArgs.Empty);

    private void HideButton_Click(object sender, RoutedEventArgs e) => Hide();

    private void OpenDiagnosticsButton_Click(object sender, RoutedEventArgs e)
    {
        string diagnosticsPath = Path.Combine(Path.GetDirectoryName(SettingsPathTextBox.Text)!, "diagnostics.log");
        Process.Start(new ProcessStartInfo("explorer.exe")
        {
            UseShellExecute = true,
            Arguments = $"/select,\"{diagnosticsPath}\"",
        });
    }

    private void CheckReleasesButton_Click(object sender, RoutedEventArgs e) =>
        Process.Start(new ProcessStartInfo("https://github.com/Samtsirhc/StrataShell/releases")
        {
            UseShellExecute = true,
        });

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBoxResult result = System.Windows.MessageBox.Show(
            "Reset the panel and taskbar to safe defaults? The custom taskbar will be disabled.",
            "Reset StrataShell", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        settings = new StrataSettings();
        UpdateSettings(settings);
        SettingsApplied?.Invoke(this, settings);
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void UpdateSummary()
    {
        string panel = settings.Panel.Enabled ? "Panel enabled" : "Panel disabled";
        string key = settings.Panel.ToggleWithWindowsKey ? "bare Windows key active" : "Windows key unchanged";
        string taskbar = settings.Taskbar.Enabled
            ? $"custom taskbar: {settings.Taskbar.Height:F0}px / {settings.Taskbar.Rows} rows"
            : "Explorer taskbar retained";
        OverviewSummaryText.Text = $"{panel} • {key} • {taskbar}";
    }
}
