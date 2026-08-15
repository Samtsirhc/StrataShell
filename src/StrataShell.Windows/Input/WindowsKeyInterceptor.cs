using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using StrataShell.Core.Input;

namespace StrataShell.Windows.Input;

/// <summary>
/// Intercepts a bare Windows-key gesture while forwarding Windows shortcuts.
/// Disposing the interceptor always removes the native hook.
/// </summary>
public sealed class WindowsKeyInterceptor : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private const uint LlkhfInjected = 0x10;
    private const uint InputKeyboard = 1;
    private const uint KeyEventExtendedKey = 0x0001;
    private const uint KeyEventKeyUp = 0x0002;

    private readonly WindowsKeyStateMachine stateMachine = new();
    private readonly SynchronizationContext synchronizationContext;
    private readonly LowLevelKeyboardProc callback;
    private nint hookHandle;
    private bool disposed;

    /// <summary>Initializes an interceptor bound to the current UI context.</summary>
    public WindowsKeyInterceptor()
    {
        synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();
        callback = HookCallback;
    }

    /// <summary>Raised asynchronously after a bare Windows-key gesture.</summary>
    public event EventHandler? ToggleRequested;

    /// <summary>Gets whether the native hook is active.</summary>
    public bool IsEnabled => hookHandle != 0;

    /// <summary>Installs the low-level keyboard hook.</summary>
    public void Enable()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (IsEnabled)
        {
            return;
        }

        using Process process = Process.GetCurrentProcess();
        using ProcessModule? module = process.MainModule;
        nint moduleHandle = GetModuleHandle(module?.ModuleName);
        hookHandle = SetWindowsHookEx(WhKeyboardLl, callback, moduleHandle, 0);
        if (hookHandle == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to install the Windows-key hook.");
        }
    }

    /// <summary>Removes the hook and releases any synthetic modifier state.</summary>
    public void Disable()
    {
        WindowsKeyResetDecision reset = stateMachine.Reset();
        if (reset.ReleaseForwardedWindowsKey)
        {
            SendWindowsKey(reset.WindowsVirtualKey, keyUp: true);
        }

        if (hookHandle == 0)
        {
            return;
        }

        nint handle = hookHandle;
        hookHandle = 0;
        if (!UnhookWindowsHookEx(handle))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to remove the Windows-key hook.");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        try
        {
            Disable();
        }
        finally
        {
            disposed = true;
        }
    }

    private nint HookCallback(int code, nint wParam, nint lParam)
    {
        if (code < 0 || !IsEnabled)
        {
            return CallNextHookEx(hookHandle, code, wParam, lParam);
        }

        try
        {
            KeyboardHookData data = Marshal.PtrToStructure<KeyboardHookData>(lParam);
            int message = unchecked((int)wParam);
            KeyTransition? transition = message switch
            {
                WmKeyDown or WmSysKeyDown => KeyTransition.Down,
                WmKeyUp or WmSysKeyUp => KeyTransition.Up,
                _ => null,
            };

            if (transition is null)
            {
                return CallNextHookEx(hookHandle, code, wParam, lParam);
            }

            WindowsKeyDecision decision = stateMachine.Process(
                data.VirtualKey,
                transition.Value,
                (data.Flags & LlkhfInjected) != 0);

            if (decision.ForwardWindowsKeyDown)
            {
                SendWindowsKey(decision.WindowsVirtualKey, keyUp: false);
            }

            if (decision.TogglePanel)
            {
                synchronizationContext.Post(
                    static state => ((WindowsKeyInterceptor)state!).ToggleRequested?.Invoke(state, EventArgs.Empty),
                    this);
            }

            if (decision.Suppress)
            {
                return 1;
            }
        }
        catch
        {
            // A global input hook must fail open. Passing the event preserves
            // system input even if an unexpected interop failure occurs.
        }

        return CallNextHookEx(hookHandle, code, wParam, lParam);
    }

    private static void SendWindowsKey(uint virtualKey, bool keyUp)
    {
        Input input = new()
        {
            Type = InputKeyboard,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInput
                {
                    VirtualKey = checked((ushort)virtualKey),
                    Flags = KeyEventExtendedKey | (keyUp ? KeyEventKeyUp : 0),
                },
            },
        };

        if (SendInput(1, [input], Marshal.SizeOf<Input>()) != 1)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to forward the Windows key.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct KeyboardHookData
    {
        public readonly uint VirtualKey;
        public readonly uint ScanCode;
        public readonly uint Flags;
        public readonly uint Time;
        public readonly nint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }

    private delegate nint LowLevelKeyboardProc(int code, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(
        int hookType,
        LowLevelKeyboardProc callback,
        nint module,
        uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hook);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hook, int code, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint GetModuleHandle(string? moduleName);
}
