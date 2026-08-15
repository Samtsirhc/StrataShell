using System.Diagnostics;
using StrataShell.Core.Apps;

namespace StrataShell.Windows.Apps;

/// <summary>Launches Start-menu shortcuts through the Windows shell.</summary>
public static class ShellLauncher
{
    /// <summary>Attempts to launch a shortcut without allowing shell failures to terminate the UI process.</summary>
    public static bool TryLaunch(AppShortcut shortcut, out string? error)
    {
        ArgumentNullException.ThrowIfNull(shortcut);
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = shortcut.Path,
                UseShellExecute = true,
            });
            error = null;
            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or System.IO.FileNotFoundException)
        {
            error = exception.Message;
            return false;
        }
    }

    /// <summary>Launches a shortcut with normal shell association behavior.</summary>
    public static void Launch(AppShortcut shortcut)
    {
        if (!TryLaunch(shortcut, out string? error))
        {
            throw new InvalidOperationException(error);
        }
    }
}
