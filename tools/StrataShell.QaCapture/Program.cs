using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;
using Forms = System.Windows.Forms;

bool dpiAwarenessApplied = SetProcessDpiAwarenessContext(new nint(-4));

if (args is ["--restore-taskbar"])
{
    int restored = RestoreExplorerTaskbars();
    Console.WriteLine(JsonSerializer.Serialize(new { restored }));
    return restored > 0 ? 0 : 3;
}

if (args is ["--capture-window-title", var title, var windowOutput])
{
    return CaptureWindow(title, windowOutput);
}

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: StrataShell.QaCapture <output.png>");
    return 2;
}

string outputPath = Path.GetFullPath(args[0]);
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
System.Drawing.Rectangle bounds = Forms.Screen.PrimaryScreen?.Bounds
    ?? throw new InvalidOperationException("No primary display was found.");

using System.Drawing.Bitmap bitmap = new(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
{
    nint destination = graphics.GetHdc();
    nint source = GetDC(0);
    try
    {
        const uint SourceCopyWithLayeredWindows = 0x40CC0020;
        if (!BitBlt(destination, 0, 0, bounds.Width, bounds.Height,
            source, bounds.Left, bounds.Top, SourceCopyWithLayeredWindows))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
    }
    finally
    {
        int released = ReleaseDC(0, source);
        if (released == 0)
        {
            Console.Error.WriteLine("Warning: the desktop device context could not be released.");
        }
        graphics.ReleaseHdc(destination);
    }
}

bitmap.Save(outputPath, ImageFormat.Png);
Console.WriteLine(JsonSerializer.Serialize(new
{
    outputPath,
    bounds.X,
    bounds.Y,
    bounds.Width,
    bounds.Height,
    dpiAwarenessApplied,
}));
return 0;

static int RestoreExplorerTaskbars()
{
    const int SwShow = 5;
    int restored = 0;
    nint primary = FindWindow("Shell_TrayWnd", null);
    if (primary != 0)
    {
        ShowWindow(primary, SwShow);
        restored++;
    }

    nint current = 0;
    while ((current = FindWindowEx(0, current, "Shell_SecondaryTrayWnd", null)) != 0)
    {
        ShowWindow(current, SwShow);
        restored++;
    }

    return restored;
}

static int CaptureWindow(string title, string output)
{
    nint window = FindWindow(null, title);
    if (window == 0 || !GetWindowRect(window, out Rect bounds))
    {
        Console.Error.WriteLine($"Window not found: {title}");
        return 4;
    }

    int width = bounds.Right - bounds.Left;
    int height = bounds.Bottom - bounds.Top;
    using System.Drawing.Bitmap bitmap = new(width, height, PixelFormat.Format32bppArgb);
    using System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap);
    nint deviceContext = graphics.GetHdc();
    bool rendered;
    try
    {
        rendered = PrintWindow(window, deviceContext, 2);
    }
    finally
    {
        graphics.ReleaseHdc(deviceContext);
    }

    string fullOutput = Path.GetFullPath(output);
    Directory.CreateDirectory(Path.GetDirectoryName(fullOutput)!);
    bitmap.Save(fullOutput, ImageFormat.Png);
    int cloaked = 0;
    int cloakedResult = DwmGetWindowAttribute(window, 14, out cloaked, sizeof(int));
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        outputPath = fullOutput,
        rendered,
        visible = IsWindowVisible(window),
        topmost = (GetWindowLongPtr(window, -20).ToInt64() & 0x8) != 0,
        cloaked,
        cloakedResult,
        bounds.Left,
        bounds.Top,
        width,
        height,
    }));
    return rendered ? 0 : 5;
}

[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
static extern nint FindWindow(string? className, string? windowName);

[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
static extern nint FindWindowEx(nint parent, nint childAfter, string? className, string? windowName);

[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool ShowWindow(nint hWnd, int command);

[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool GetWindowRect(nint hWnd, out Rect rectangle);

[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool IsWindowVisible(nint hWnd);

[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool PrintWindow(nint hWnd, nint hdc, uint flags);

[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
static extern nint GetWindowLongPtr(nint hWnd, int index);

[DllImport("dwmapi.dll")]
static extern int DwmGetWindowAttribute(nint hWnd, int attribute, out int value, int valueSize);

[DllImport("user32.dll")]
static extern nint GetDC(nint hWnd);

[DllImport("user32.dll")]
static extern int ReleaseDC(nint hWnd, nint hdc);

[DllImport("gdi32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool BitBlt(
    nint destination,
    int destinationX,
    int destinationY,
    int width,
    int height,
    nint source,
    int sourceX,
    int sourceY,
    uint operation);

[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool SetProcessDpiAwarenessContext(nint value);

[StructLayout(LayoutKind.Sequential)]
struct Rect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}
