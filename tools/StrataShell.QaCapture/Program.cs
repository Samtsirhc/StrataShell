using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Automation;
using Forms = System.Windows.Forms;

bool dpiAwarenessApplied = SetProcessDpiAwarenessContext(new nint(-4));

if (args is ["--inspect-accessibility-title", var accessibilityTitle])
{
    return InspectAccessibility(accessibilityTitle);
}

if (args is ["--focus-accessibility-title", var focusWindowTitle, var focusElementName])
{
    return FocusAccessibility(focusWindowTitle, focusElementName);
}

if (args is ["--invoke-accessibility-title", var invokeWindowTitle, var invokeElementName, var expectedPopupName])
{
    return InvokeAccessibility(invokeWindowTitle, invokeElementName, expectedPopupName);
}

if (args is ["--window-host", var windowCountText] &&
    int.TryParse(windowCountText, out int windowCount) &&
    windowCount is >= 1 and <= 40)
{
    Forms.Application.SetHighDpiMode(Forms.HighDpiMode.PerMonitorV2);
    for (int index = 0; index < windowCount; index++)
    {
        Forms.Form form = new()
        {
            Text = $"StrataShell QA Window {index + 1:D2}",
            Width = 420,
            Height = 240,
            StartPosition = Forms.FormStartPosition.Manual,
            Location = new System.Drawing.Point(80 + ((index % 6) * 38), 80 + ((index % 5) * 34)),
        };
        form.Controls.Add(new Forms.Label
        {
            Text = $"Taskbar overflow witness {index + 1:D2}",
            AutoSize = true,
            Location = new System.Drawing.Point(24, 24),
        });
        form.Show();
    }

    Forms.Application.Run();
    return 0;
}

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

if (args is ["--capture-virtual", var virtualOutput])
{
    return CaptureDesktop(Forms.SystemInformation.VirtualScreen, virtualOutput, dpiAwarenessApplied);
}

if (args is ["--inspect-bottom-windows"])
{
    return InspectBottomWindows();
}

if (args.Length == 4 && string.Equals(args[0], "--capture-bottom-strip", StringComparison.OrdinalIgnoreCase))
{
    if (!int.TryParse(args[1], out int stripScreenIndex) ||
        !int.TryParse(args[2], out int stripHeight) ||
        stripScreenIndex < 0 || stripScreenIndex >= Forms.Screen.AllScreens.Length ||
        stripHeight <= 0)
    {
        Console.Error.WriteLine(JsonSerializer.Serialize(new
        {
            error = "Invalid bottom-strip arguments.",
            screenCount = Forms.Screen.AllScreens.Length,
            args,
        }));
        return 6;
    }

    string stripOutput = args[3];
    System.Drawing.Rectangle screenBounds = Forms.Screen.AllScreens[stripScreenIndex].Bounds;
    int boundedHeight = Math.Min(stripHeight, screenBounds.Height);
    System.Drawing.Rectangle stripBounds = new(
        screenBounds.Left,
        screenBounds.Bottom - boundedHeight,
        screenBounds.Width,
        boundedHeight);
    return CaptureDesktop(stripBounds, stripOutput, dpiAwarenessApplied);
}

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: StrataShell.QaCapture <output.png>");
    return 2;
}

System.Drawing.Rectangle bounds = Forms.Screen.PrimaryScreen?.Bounds
    ?? throw new InvalidOperationException("No primary display was found.");
return CaptureDesktop(bounds, args[0], dpiAwarenessApplied);

static int CaptureDesktop(System.Drawing.Rectangle bounds, string output, bool dpiAwarenessApplied)
{
    string outputPath = Path.GetFullPath(output);
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
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
}

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

static int InspectBottomWindows()
{
    System.Drawing.Rectangle screen = Forms.Screen.PrimaryScreen?.Bounds
        ?? throw new InvalidOperationException("No primary display was found.");
    int threshold = screen.Bottom - 220;
    List<object> windows = [];
    int zOrder = 0;
    EnumWindows((window, _) =>
    {
        int currentZ = zOrder++;
        if (!IsWindowVisible(window) || !GetWindowRect(window, out Rect rect) || rect.Bottom <= threshold)
        {
            return true;
        }

        StringBuilder title = new(512);
        StringBuilder className = new(256);
        _ = GetWindowText(window, title, title.Capacity);
        _ = GetClassName(window, className, className.Capacity);
        windows.Add(new
        {
            zOrder = currentZ,
            handle = $"0x{window.ToInt64():X}",
            title = title.ToString(),
            className = className.ToString(),
            topmost = (GetWindowLongPtr(window, -20).ToInt64() & 0x8) != 0,
            rect.Left,
            rect.Top,
            rect.Right,
            rect.Bottom,
        });
        return true;
    }, 0);

    Console.WriteLine(JsonSerializer.Serialize(new { screen, windows }, QaJson.Indented));
    return 0;
}

static int InspectAccessibility(string title)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(title);
    AutomationElement? window = AutomationElement.RootElement.FindFirst(
        TreeScope.Children,
        new PropertyCondition(AutomationElement.NameProperty, title, PropertyConditionFlags.IgnoreCase));
    if (window is null)
    {
        Console.Error.WriteLine(JsonSerializer.Serialize(new { error = "Accessible window was not found.", title }));
        return 7;
    }

    List<object> elements = [];
    AutomationElementCollection descendants = window.FindAll(TreeScope.Descendants, Condition.TrueCondition);
    foreach (AutomationElement element in descendants)
    {
        try
        {
            AutomationElement.AutomationElementInformation current = element.Current;
            if (!current.IsControlElement)
            {
                continue;
            }

            System.Windows.Rect bounds = current.BoundingRectangle;
            object? serializedBounds = double.IsFinite(bounds.X) &&
                double.IsFinite(bounds.Y) &&
                double.IsFinite(bounds.Width) &&
                double.IsFinite(bounds.Height)
                    ? new { bounds.X, bounds.Y, bounds.Width, bounds.Height }
                    : null;
            elements.Add(new
            {
                current.Name,
                controlType = current.ControlType?.ProgrammaticName.Replace("ControlType.", string.Empty, StringComparison.Ordinal),
                current.AutomationId,
                current.IsEnabled,
                current.IsKeyboardFocusable,
                current.HasKeyboardFocus,
                current.IsOffscreen,
                bounds = serializedBounds,
            });
        }
        catch (ElementNotAvailableException)
        {
            // Dynamic shell collections can change while the tree is read.
        }
    }

    Console.WriteLine(JsonSerializer.Serialize(new
    {
        title,
        processId = window.Current.ProcessId,
        elementCount = elements.Count,
        elements,
    }, QaJson.Indented));
    return elements.Count > 0 ? 0 : 8;
}

static int FocusAccessibility(string title, string elementName)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(title);
    ArgumentException.ThrowIfNullOrWhiteSpace(elementName);
    AutomationElement? window = AutomationElement.RootElement.FindFirst(
        TreeScope.Children,
        new PropertyCondition(AutomationElement.NameProperty, title, PropertyConditionFlags.IgnoreCase));
    if (window is null)
    {
        Console.Error.WriteLine(JsonSerializer.Serialize(new { error = "Accessible window was not found.", title }));
        return 7;
    }

    AutomationElement? element = window.FindFirst(
        TreeScope.Descendants,
        new AndCondition(
            new PropertyCondition(AutomationElement.NameProperty, elementName),
            new PropertyCondition(AutomationElement.IsKeyboardFocusableProperty, true)));
    if (element is null)
    {
        Console.Error.WriteLine(JsonSerializer.Serialize(new
        {
            error = "Focusable accessible element was not found.",
            title,
            elementName,
        }));
        return 9;
    }

    element.SetFocus();
    Thread.Sleep(150);
    bool focused = element.Current.HasKeyboardFocus;
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        title,
        elementName,
        focused,
        processId = element.Current.ProcessId,
        controlType = element.Current.ControlType?.ProgrammaticName.Replace("ControlType.", string.Empty, StringComparison.Ordinal),
    }, QaJson.Indented));
    return focused ? 0 : 10;
}

static int InvokeAccessibility(string title, string elementName, string expectedPopupName)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(title);
    ArgumentException.ThrowIfNullOrWhiteSpace(elementName);
    ArgumentException.ThrowIfNullOrWhiteSpace(expectedPopupName);
    AutomationElement? window = AutomationElement.RootElement.FindFirst(
        TreeScope.Children,
        new PropertyCondition(AutomationElement.NameProperty, title, PropertyConditionFlags.IgnoreCase));
    AutomationElement? element = window?.FindFirst(
        TreeScope.Descendants,
        new PropertyCondition(AutomationElement.NameProperty, elementName));
    if (element is null || !element.TryGetCurrentPattern(InvokePattern.Pattern, out object? patternObject) ||
        patternObject is not InvokePattern invokePattern)
    {
        Console.Error.WriteLine(JsonSerializer.Serialize(new
        {
            error = "Invokable accessible element was not found.",
            title,
            elementName,
        }));
        return 11;
    }

    invokePattern.Invoke();
    bool popupFound = WaitForAccessibleName(expectedPopupName);
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        title,
        elementName,
        expectedPopupName,
        popupFound,
    }, QaJson.Indented));
    return popupFound ? 0 : 12;
}

static bool WaitForAccessibleName(string expectedName)
{
    PropertyCondition nameCondition = new(AutomationElement.NameProperty, expectedName);
    for (int attempt = 0; attempt < 5; attempt++)
    {
        Thread.Sleep(100);
        AutomationElementCollection windows;
        try
        {
            windows = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition);
        }
        catch (ElementNotAvailableException)
        {
            continue;
        }

        foreach (AutomationElement candidateWindow in windows)
        {
            try
            {
                if (string.Equals(candidateWindow.Current.Name, expectedName, StringComparison.Ordinal) ||
                    candidateWindow.FindFirst(TreeScope.Descendants, nameCondition) is not null)
                {
                    return true;
                }
            }
            catch (ElementNotAvailableException)
            {
                // A popup or unrelated application window changed during enumeration.
            }
        }
    }

    return false;
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

[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool EnumWindows(EnumWindowsProc callback, nint state);

[DllImport("user32.dll", CharSet = CharSet.Unicode)]
static extern int GetWindowText(nint window, StringBuilder text, int maximumCount);

[DllImport("user32.dll", CharSet = CharSet.Unicode)]
static extern int GetClassName(nint window, StringBuilder className, int maximumCount);

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

delegate bool EnumWindowsProc(nint window, nint state);

static class QaJson
{
    public static JsonSerializerOptions Indented { get; } = new() { WriteIndented = true };
}
