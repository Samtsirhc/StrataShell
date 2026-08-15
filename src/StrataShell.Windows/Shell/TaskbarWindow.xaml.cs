using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ManagedShell.AppBar;
using ManagedShell.Common.Helpers;
using ManagedShell.WindowsTasks;
using ManagedShell.WindowsTray;
using StrataShell.Core.Apps;
using StrataShell.Core.Configuration;
using StrataShell.Windows.Apps;
using TrayNotifyIcon = ManagedShell.WindowsTray.NotifyIcon;
using WpfButton = System.Windows.Controls.Button;

namespace StrataShell.Windows.Shell;

/// <summary>Adjustable-height, wrapping taskbar hosted as a documented AppBar.</summary>
public partial class TaskbarWindow : AppBarWindow
{
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private static readonly nint HwndTopmost = new(-1);
    private readonly ShellRuntime runtime;
    private readonly DispatcherTimer clockTimer;
    private string timeText = string.Empty;
    private string dateText = string.Empty;

    /// <summary>Creates a taskbar on the supplied monitor using the supplied settings.</summary>
    public TaskbarWindow(ShellRuntime runtime, TaskbarSettings settings, System.Windows.Forms.Screen screen)
        : base(
            runtime.Manager.AppBarManager,
            runtime.Manager.ExplorerHelper,
            runtime.Manager.FullScreenHelper,
            AppBarScreen.FromScreen(screen),
            AppBarEdge.Bottom,
            settings.AutoHide ? AppBarMode.AutoHide : AppBarMode.Normal,
            settings.Height)
    {
        this.runtime = runtime;
        Settings = settings;
        TasksView = runtime.TasksView;
        PinnedTrayIcons = runtime.Manager.NotificationArea.PinnedIcons;
        TaskButtonHeight = Math.Max(32, (settings.Height - 10) / settings.Rows);
        TaskIconSize = settings.IconSize;
        QuickLaunchVisibility = settings.ShowQuickLaunch ? Visibility.Visible : Visibility.Collapsed;
        NotificationAreaVisibility = settings.ShowNotificationArea ? Visibility.Visible : Visibility.Collapsed;

        IReadOnlyList<AppShortcut> catalog = StartMenuCatalog.GetShortcuts();
        IEnumerable<AppShortcut> quickLaunch = settings.QuickLaunchPaths.Length == 0
            ? catalog.Take(6)
            : settings.QuickLaunchPaths
                .Select(path => catalog.FirstOrDefault(shortcut =>
                    string.Equals(shortcut.Path, path, StringComparison.OrdinalIgnoreCase)))
                .Where(shortcut => shortcut is not null)
                .Cast<AppShortcut>();
        foreach (AppShortcut shortcut in quickLaunch)
        {
            QuickLaunchItems.Add(new TaskbarShortcutItem(shortcut, ShellIconService.GetLargeIcon(shortcut.Path)));
        }

        InitializeComponent();
        DataContext = this;
        Loaded += OnLoaded;
        Closed += OnClosed;

        clockTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, OnClockTick, Dispatcher);
        UpdateClock();
    }

    /// <summary>Raised when the taskbar Start button is invoked.</summary>
    public event EventHandler? StartRequested;

    /// <summary>Raised when the clock is invoked.</summary>
    public event EventHandler? ClockRequested;

    /// <summary>Gets the settings snapshot used to create this taskbar.</summary>
    public TaskbarSettings Settings { get; }

    /// <summary>Gets the live task-window view.</summary>
    public ICollectionView TasksView { get; }

    /// <summary>Gets currently pinned notification icons.</summary>
    public ICollectionView PinnedTrayIcons { get; }

    /// <summary>Gets quick-launch items.</summary>
    public ObservableCollection<TaskbarShortcutItem> QuickLaunchItems { get; } = [];

    /// <summary>Gets the bounded quick-launch subset rendered directly on the bar.</summary>
    public IReadOnlyList<TaskbarShortcutItem> VisibleQuickLaunchItems => [.. QuickLaunchItems.Take(6)];

    /// <summary>Gets per-button height used to create the requested rows.</summary>
    public double TaskButtonHeight { get; }

    /// <summary>Gets the configured task and shortcut icon size.</summary>
    public double TaskIconSize { get; }

    /// <summary>Gets quick-launch visibility.</summary>
    public Visibility QuickLaunchVisibility { get; }

    /// <summary>Gets notification-area visibility.</summary>
    public Visibility NotificationAreaVisibility { get; }

    /// <summary>Returns render state used by diagnostics and release QA.</summary>
    public string GetVisualDiagnosticState() =>
        $"windowOpacity={Opacity:F2}, windowVisibility={Visibility}, background={Background}, " +
        $"root={TaskbarRoot.ActualWidth:F1}x{TaskbarRoot.ActualHeight:F1}, rootOpacity={TaskbarRoot.Opacity:F2}, " +
        $"rootVisibility={TaskbarRoot.Visibility}, topmost={Topmost}";

    /// <summary>Returns item counts served by the three overflow menus.</summary>
    public string GetOverflowDiagnosticState() =>
        $"quickLaunch={QuickLaunchItems.Count}, tasks={TasksView.Cast<object>().OfType<ApplicationWindow>().Count()}, " +
        $"tray={PinnedTrayIcons.Cast<object>().OfType<TrayNotifyIcon>().Count()}";

    /// <summary>Renders the live WPF visual tree to a PNG for deterministic visual regression.</summary>
    public void SaveVisualSnapshot(string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        int width = Math.Max(1, (int)Math.Ceiling(ActualWidth * dpi.DpiScaleX));
        int height = Math.Max(1, (int)Math.Ceiling(ActualHeight * dpi.DpiScaleY));
        RenderTargetBitmap bitmap = new(width, height, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(this);

        string fullPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(fullPath);
        encoder.Save(stream);
    }

    /// <summary>Gets the current short time string.</summary>
    public string TimeText
    {
        get => timeText;
        private set => SetField(ref timeText, value);
    }

    /// <summary>Gets the current short date string.</summary>
    public string DateText
    {
        get => dateText;
        private set => SetField(ref dateText, value);
    }

    /// <summary>Closes the taskbar and immediately restores Explorer's taskbar.</summary>
    public void Shutdown()
    {
        runtime.Manager.ExplorerHelper.HideExplorerTaskbar = false;
        AllowClose = true;
        Close();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        runtime.Manager.ExplorerHelper.HideExplorerTaskbar = true;
        Topmost = true;
        nint handle = new WindowInteropHelper(this).Handle;
        SetWindowPos(handle, HwndTopmost, 0, 0, 0, 0,
            SwpNoMove | SwpNoSize | SwpNoActivate | SwpShowWindow);
        clockTimer.Start();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        clockTimer.Stop();
        runtime.Manager.ExplorerHelper.HideExplorerTaskbar = false;
    }

    /// <summary>
    /// Concedes z-order only to the foreground full-screen app. ManagedShell's
    /// global collection can contain inactive full-screen windows, which must
    /// not hide the taskbar while the user is working elsewhere.
    /// </summary>
    protected override void OnFullScreenEnter(FullScreenApp app)
    {
        bool coversMonitor = app.rect.Top == app.screen.Bounds.Top &&
            app.rect.Left == app.screen.Bounds.Left &&
            app.rect.Bottom == app.screen.Bounds.Bottom &&
            app.rect.Right == app.screen.Bounds.Right;

        if (coversMonitor && app.hWnd == GetForegroundWindow())
        {
            base.OnFullScreenEnter(app);
        }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e) =>
        StartRequested?.Invoke(this, EventArgs.Empty);

    private void QuickLaunchButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton { DataContext: TaskbarShortcutItem item })
        {
            if (!ShellLauncher.TryLaunch(item.Shortcut, out string? error))
            {
                System.Windows.MessageBox.Show(
                    $"{item.Shortcut.Name} could not be opened.\n\n{error}",
                    "StrataShell launch error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }

    private void TaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not WpfButton { DataContext: ApplicationWindow window })
        {
            return;
        }

        if (window.State == ApplicationWindow.WindowState.Active)
        {
            window.Minimize();
        }
        else
        {
            window.BringToFront();
        }
    }

    private void QuickLaunchOverflowButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not WpfButton button)
        {
            return;
        }

        System.Windows.Controls.ContextMenu menu = CreateOverflowMenu("Quick launch", QuickLaunchItems.Select(item =>
            (item.Shortcut.Name, (Action)(() => LaunchQuickShortcut(item)))));
        OpenOverflowMenu(button, menu);
    }

    private void TaskOverflowButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not WpfButton button)
        {
            return;
        }

        System.Windows.Controls.ContextMenu menu = CreateOverflowMenu("Running windows", TasksView
            .Cast<object>()
            .OfType<ApplicationWindow>()
            .Select(window => (string.IsNullOrWhiteSpace(window.Title) ? "Untitled window" : window.Title,
                (Action)(() => ToggleTaskWindow(window)))));
        OpenOverflowMenu(button, menu);
    }

    private void TrayOverflowButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not WpfButton button)
        {
            return;
        }

        System.Windows.Controls.ContextMenu menu = CreateOverflowMenu("Notification icons", PinnedTrayIcons
            .Cast<object>()
            .OfType<TrayNotifyIcon>()
            .Select(icon => (string.IsNullOrWhiteSpace(icon.Title) ? "Notification icon" : icon.Title,
                (Action)(() => InvokeTrayIcon(icon, MouseButton.Left)))));
        OpenOverflowMenu(button, menu);
    }

    private static System.Windows.Controls.ContextMenu CreateOverflowMenu(
        string heading,
        IEnumerable<(string Label, Action Invoke)> entries)
    {
        System.Windows.Controls.ContextMenu menu = new();
        menu.Items.Add(new System.Windows.Controls.MenuItem { Header = heading, IsEnabled = false });
        menu.Items.Add(new System.Windows.Controls.Separator());
        int count = 0;
        foreach ((string label, Action invoke) in entries.Take(100))
        {
            System.Windows.Controls.MenuItem item = new() { Header = label };
            item.Click += (_, _) => invoke();
            menu.Items.Add(item);
            count++;
        }

        if (count == 0)
        {
            menu.Items.Add(new System.Windows.Controls.MenuItem { Header = "No items", IsEnabled = false });
        }

        return menu;
    }

    private static void OpenOverflowMenu(WpfButton button, System.Windows.Controls.ContextMenu menu)
    {
        menu.PlacementTarget = button;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;
        menu.IsOpen = true;
    }

    private static void ToggleTaskWindow(ApplicationWindow window)
    {
        if (window.State == ApplicationWindow.WindowState.Active)
        {
            window.Minimize();
        }
        else
        {
            window.BringToFront();
        }
    }

    private static void InvokeTrayIcon(TrayNotifyIcon icon, MouseButton button)
    {
        uint mousePosition = MouseHelper.GetCursorPositionParam();
        int doubleClickTime = System.Windows.Forms.SystemInformation.DoubleClickTime;
        icon.IconMouseDown(button, mousePosition, doubleClickTime);
        icon.IconMouseUp(button, mousePosition, doubleClickTime);
    }

    private static void LaunchQuickShortcut(TaskbarShortcutItem item)
    {
        if (!ShellLauncher.TryLaunch(item.Shortcut, out string? error))
        {
            System.Windows.MessageBox.Show(
                $"{item.Shortcut.Name} could not be opened.\n\n{error}",
                "StrataShell launch error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void TrayIcon_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton { DataContext: TrayNotifyIcon icon })
        {
            InvokeTrayIcon(icon, MouseButton.Left);
        }
    }

    private void TrayIcon_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is WpfButton { DataContext: TrayNotifyIcon icon })
        {
            InvokeTrayIcon(icon, MouseButton.Right);
            e.Handled = true;
        }
    }

    private void ClockButton_Click(object sender, RoutedEventArgs e) =>
        ClockRequested?.Invoke(this, EventArgs.Empty);

    private void OnClockTick(object? sender, EventArgs e)
    {
        UpdateClock();
    }

    private void UpdateClock()
    {
        DateTime now = DateTime.Now;
        TimeText = now.ToString("t", CultureInfo.CurrentCulture);
        DateText = now.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName ?? string.Empty);
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

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

}
