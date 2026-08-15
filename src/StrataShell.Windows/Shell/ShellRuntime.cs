using System.Collections;
using System.ComponentModel;
using System.Windows.Data;
using ManagedShell;
using ManagedShell.Common.Enums;
using ManagedShell.WindowsTasks;

namespace StrataShell.Windows.Shell;

/// <summary>
/// Owns the ManagedShell services used by StrataShell while Explorer remains
/// the recoverable desktop shell.
/// </summary>
public sealed class ShellRuntime : IDisposable
{
    private bool disposed;

    /// <summary>Initializes task, tray, AppBar, and full-screen services.</summary>
    public ShellRuntime()
    {
        Manager = new ShellManager(new ShellConfig
        {
            EnableTasksService = true,
            AutoStartTasksService = true,
            MultiMonAwareTasksService = true,
            TaskIconSize = IconSize.Large,
            EnableTrayService = true,
            AutoStartTrayService = true,
            PinnedNotifyIcons = ManagedShell.WindowsTray.NotificationArea.DEFAULT_PINNED,
            EnableWin11DpiWorkaround = false,
        });

        IList source = (IList)Manager.Tasks.GroupedWindows.SourceCollection;
        TasksView = new ListCollectionView(source)
        {
            Filter = static item => item is ApplicationWindow window && window.ShowInTaskbar,
            IsLiveFiltering = true,
        };
        TasksView.LiveFilteringProperties.Add(nameof(ApplicationWindow.ShowInTaskbar));
    }

    /// <summary>Gets the underlying shell service owner.</summary>
    public ShellManager Manager { get; }

    /// <summary>Gets an ungrouped live view of visible taskbar windows.</summary>
    public ListCollectionView TasksView { get; }

    /// <summary>Restores Explorer visibility and disposes all services.</summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Manager.ExplorerHelper.HideExplorerTaskbar = false;
        Manager.Dispose();
        disposed = true;
    }
}
