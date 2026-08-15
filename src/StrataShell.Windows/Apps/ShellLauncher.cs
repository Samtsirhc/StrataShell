using System.Diagnostics;
using StrataShell.Core.Apps;

namespace StrataShell.Windows.Apps;

/// <summary>Launches Start-menu shortcuts through the Windows shell.</summary>
public static class ShellLauncher
{
    /// <summary>Launches a shortcut with normal shell association behavior.</summary>
    public static void Launch(AppShortcut shortcut)
    {
        ArgumentNullException.ThrowIfNull(shortcut);
        Process.Start(new ProcessStartInfo
        {
            FileName = shortcut.Path,
            UseShellExecute = true,
        });
    }
}
