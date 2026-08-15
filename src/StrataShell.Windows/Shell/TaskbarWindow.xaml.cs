using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
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

    /// <summary>Creates a primary-monitor taskbar using the supplied settings.</summary>
    public TaskbarWindow(ShellRuntime runtime, TaskbarSettings settings)
        : base(
            runtime.Manager.AppBarManager,
            runtime.Manager.ExplorerHelper,
            runtime.Manager.FullScreenHelper,
            AppBarScreen.FromPrimaryScreen(),
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

    /// <summary>Gets per-button height used to create the requested rows.</summary>
    public double TaskButtonHeight { get; }

    /// <summary>Gets the configured task and shortcut icon size.</summary>
    public double TaskIconSize { get; }

    /// <summary>Gets quick-launch visibility.</summary>
    public Visibility QuickLaunchVisibility { get; }

    /// <summary>Gets notification-area visibility.</summary>
    public Visibility NotificationAreaVisibility { get; }

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
            ShellLauncher.Launch(item.Shortcut);
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

    private void TrayIcon_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton { DataContext: TrayNotifyIcon icon })
        {
            uint mousePosition = MouseHelper.GetCursorPositionParam();
            int doubleClickTime = System.Windows.Forms.SystemInformation.DoubleClickTime;
            icon.IconMouseDown(MouseButton.Left, mousePosition, doubleClickTime);
            icon.IconMouseUp(MouseButton.Left, mousePosition, doubleClickTime);
        }
    }

    private void TrayIcon_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is WpfButton { DataContext: TrayNotifyIcon icon })
        {
            uint mousePosition = MouseHelper.GetCursorPositionParam();
            int doubleClickTime = System.Windows.Forms.SystemInformation.DoubleClickTime;
            icon.IconMouseDown(MouseButton.Right, mousePosition, doubleClickTime);
            icon.IconMouseUp(MouseButton.Right, mousePosition, doubleClickTime);
            e.Handled = true;
        }
    }

    private void ClockButton_Click(object sender, RoutedEventArgs e) =>
        ClockRequested?.Invoke(this, EventArgs.Empty);

    private void OnClockTick(object? sender, EventArgs e) => UpdateClock();

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
