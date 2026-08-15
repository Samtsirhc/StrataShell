using System.Diagnostics;
using System.Runtime.InteropServices;

if (args.Length != 1 || !int.TryParse(args[0], out int parentProcessId) || parentProcessId <= 0)
{
    return 2;
}

try
{
    using Process parent = Process.GetProcessById(parentProcessId);
    parent.WaitForExit();
}
catch (ArgumentException)
{
    // The parent already exited; recovery is still required.
}
catch (InvalidOperationException)
{
    // The parent already exited; recovery is still required.
}

RestoreExplorerTaskbars();
return 0;

static void RestoreExplorerTaskbars()
{
    const int SwShow = 5;
    nint primary = FindWindow("Shell_TrayWnd", null);
    if (primary != 0)
    {
        ShowWindow(primary, SwShow);
    }

    nint current = 0;
    while ((current = FindWindowEx(0, current, "Shell_SecondaryTrayWnd", null)) != 0)
    {
        ShowWindow(current, SwShow);
    }
}

[DllImport("user32.dll", CharSet = CharSet.Unicode)]
static extern nint FindWindow(string? className, string? windowName);

[DllImport("user32.dll", CharSet = CharSet.Unicode)]
static extern nint FindWindowEx(nint parent, nint childAfter, string? className, string? windowName);

[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool ShowWindow(nint hWnd, int command);
