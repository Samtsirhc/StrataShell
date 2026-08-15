using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using StrataShell.App.ViewModels;
using StrataShell.Core.Configuration;
using StrataShell.Windows.Apps;
using Forms = System.Windows.Forms;

namespace StrataShell.App;

/// <summary>A monitor-sized, keyboard-accessible application launcher.</summary>
public partial class PanelWindow : Window
{
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private static readonly nint HwndTopmost = new(-1);
    private readonly ObservableCollection<AppTileViewModel> apps = [];
    private readonly DispatcherTimer clockTimer;
    private ICollectionView? appsView;
    private PanelSettings settings;
    private bool loadedApps;
    private bool closingImmediately;
    private HashSet<string> pinnedPaths = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates the full-screen panel.</summary>
    public PanelWindow(PanelSettings settings)
    {
        this.settings = settings;
        InitializeComponent();
        AppsList.ItemsSource = apps;
        clockTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background,
            (_, _) => UpdateClock(), Dispatcher);
        Loaded += OnLoaded;
        Deactivated += (_, _) => HidePanel();
        Closing += OnClosing;
        UpdateClock();
    }

    /// <summary>Raised when the settings affordance is invoked.</summary>
    public event EventHandler? SettingsRequested;

    /// <summary>Raised when the user pins or unpins an app from quick launch.</summary>
    public event Action<string>? QuickLaunchToggleRequested;

    /// <summary>Applies panel appearance and layout settings.</summary>
    public void ApplySettings(PanelSettings value)
    {
        settings = value;
        RootGrid.Opacity = value.Opacity;
        double size = Math.Clamp(value.TileSize + 24, 88, 180);
        Resources["TileExtent"] = size;
    }

    /// <summary>Updates the pinned state shown by app tiles.</summary>
    public void ApplyPinnedPaths(IEnumerable<string> paths)
    {
        pinnedPaths = new HashSet<string>(paths, StringComparer.OrdinalIgnoreCase);
        foreach (AppTileViewModel app in apps)
        {
            app.IsPinned = pinnedPaths.Contains(app.Shortcut.Path);
        }
    }

    /// <summary>Sizes the panel to the entire pointer monitor and brings it forward.</summary>
    public void ShowPanel(bool usePrimaryScreen = false, int? screenIndex = null)
    {
        System.Drawing.Point cursor = Forms.Cursor.Position;
        Forms.Screen target = screenIndex is int index
            ? Forms.Screen.AllScreens[Math.Clamp(index, 0, Forms.Screen.AllScreens.Length - 1)]
            : usePrimaryScreen
                ? Forms.Screen.PrimaryScreen ?? Forms.Screen.FromPoint(cursor)
                : Forms.Screen.FromPoint(cursor);
        System.Drawing.Rectangle bounds = target.Bounds;
        Show();
        nint handle = new WindowInteropHelper(this).Handle;
        if (!SetWindowPos(handle, HwndTopmost, bounds.Left, bounds.Top, bounds.Width, bounds.Height,
            SwpNoActivate | SwpShowWindow))
        {
            // Fall back to WPF sizing; the keyboard hook must remain fail-open.
            Left = bounds.Left;
            Top = bounds.Top;
            Width = bounds.Width;
            Height = bounds.Height;
        }
        Activate();
        SearchBox.Clear();
        SearchBox.Focus();
        RootGrid.BeginAnimation(OpacityProperty, null);

        if (settings.Motion == MotionLevel.Full)
        {
            RootGrid.Opacity = 0;
            RootGrid.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, settings.Opacity, TimeSpan.FromMilliseconds(170))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        }
        else
        {
            RootGrid.Opacity = settings.Opacity;
        }
    }

    /// <summary>Applies a search query, including before the asynchronous catalog has finished loading.</summary>
    public void SetSearchQuery(string query)
    {
        SearchBox.Text = query;
        SearchBox.CaretIndex = SearchBox.Text.Length;
        UpdateFilter();
    }

    /// <summary>Hides without destroying the loaded app catalog.</summary>
    public void HidePanel()
    {
        if (IsVisible)
        {
            Hide();
        }
    }

    /// <summary>Allows application shutdown to destroy this normally persistent window.</summary>
    public void CloseImmediately()
    {
        closingImmediately = true;
        Close();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        clockTimer.Start();
        if (loadedApps)
        {
            return;
        }

        loadedApps = true;
        var shortcuts = await Task.Run(StartMenuCatalog.GetShortcuts);
        foreach (var shortcut in shortcuts)
        {
            apps.Add(new AppTileViewModel(shortcut)
            {
                IsPinned = pinnedPaths.Contains(shortcut.Path),
            });
        }

        appsView = CollectionViewSource.GetDefaultView(apps);
        LoadingText.Visibility = Visibility.Collapsed;
        UpdateFilter();
        _ = LoadIconsAsync();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!closingImmediately)
        {
            e.Cancel = true;
            HidePanel();
        }
        else
        {
            clockTimer.Stop();
        }
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => UpdateFilter();

    private void UpdateFilter()
    {
        if (appsView is null)
        {
            return;
        }

        string query = SearchBox.Text.Trim();
        appsView.Filter = item => item is AppTileViewModel app &&
            (query.Length == 0 || app.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase));
        AppCountText.Text = $"{appsView.Cast<object>().Count()} apps";
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            HidePanel();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            AppTileViewModel? app = AppsList.SelectedItem as AppTileViewModel
                ?? appsView?.Cast<AppTileViewModel>().FirstOrDefault();
            if (app is not null)
            {
                Launch(app);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Down && AppsList.Items.Count > 0)
        {
            AppsList.Focus();
            AppsList.SelectedIndex = Math.Max(0, AppsList.SelectedIndex);
        }
    }

    private void AppsList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source &&
            System.Windows.Controls.ItemsControl.ContainerFromElement(AppsList, source) is
                System.Windows.Controls.ListBoxItem { DataContext: AppTileViewModel app })
        {
            Launch(app);
        }
    }

    private void AppsList_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source ||
            System.Windows.Controls.ItemsControl.ContainerFromElement(AppsList, source) is not
                System.Windows.Controls.ListBoxItem { DataContext: AppTileViewModel app } item)
        {
            return;
        }

        bool currentlyPinned = pinnedPaths.Contains(app.Shortcut.Path);
        System.Windows.Controls.MenuItem command = new()
        {
            Header = currentlyPinned ? "Unpin from quick launch" : "Pin to quick launch",
        };
        command.Click += (_, _) => QuickLaunchToggleRequested?.Invoke(app.Shortcut.Path);
        item.ContextMenu = new System.Windows.Controls.ContextMenu
        {
            Items = { command },
        };
        item.ContextMenu.IsOpen = true;
        e.Handled = true;
    }

    private void Launch(AppTileViewModel app)
    {
        if (ShellLauncher.TryLaunch(app.Shortcut, out string? error))
        {
            HidePanel();
            return;
        }

        System.Windows.MessageBox.Show(
            $"{app.Name} could not be opened.\n\n{error}",
            "StrataShell launch error",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        HidePanel();
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => HidePanel();

    private void UpdateClock()
    {
        DateTime now = DateTime.Now;
        ClockText.Text = now.ToString("t", CultureInfo.CurrentCulture);
        DateText.Text = now.ToString("dddd, MMMM d", CultureInfo.CurrentCulture);
    }

    private async Task LoadIconsAsync()
    {
        await Parallel.ForEachAsync(apps, new ParallelOptions { MaxDegreeOfParallelism = 4 },
            async (app, cancellationToken) =>
            {
                var icon = await Task.Run(
                    () => ShellIconService.GetLargeIcon(app.Shortcut.Path), cancellationToken);
                await Dispatcher.InvokeAsync(() => app.Icon = icon, DispatcherPriority.Background,
                    cancellationToken);
            });
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        nint hWnd,
        nint hWndInsertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}
