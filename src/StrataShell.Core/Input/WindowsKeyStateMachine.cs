namespace StrataShell.Core.Input;

/// <summary>
/// Pure state machine that distinguishes a bare Windows-key gesture from a
/// Windows shortcut. Native input forwarding is performed by the caller.
/// </summary>
public sealed class WindowsKeyStateMachine
{
    /// <summary>Virtual-key value for the left Windows key.</summary>
    public const uint LeftWindowsKey = 0x5B;

    /// <summary>Virtual-key value for the right Windows key.</summary>
    public const uint RightWindowsKey = 0x5C;

    private bool isHeld;
    private bool isPendingBareGesture;
    private bool wasForwarded;
    private uint heldVirtualKey;

    /// <summary>Processes one low-level keyboard event.</summary>
    public WindowsKeyDecision Process(uint virtualKey, KeyTransition transition, bool isInjected)
    {
        if (isInjected)
        {
            return default;
        }

        bool isWindowsKey = virtualKey is LeftWindowsKey or RightWindowsKey;
        if (isWindowsKey)
        {
            return transition == KeyTransition.Down
                ? ProcessWindowsKeyDown(virtualKey)
                : ProcessWindowsKeyUp();
        }

        if (transition == KeyTransition.Down && isHeld && isPendingBareGesture)
        {
            isPendingBareGesture = false;
            wasForwarded = true;
            return new WindowsKeyDecision(
                Suppress: false,
                TogglePanel: false,
                ForwardWindowsKeyDown: true,
                WindowsVirtualKey: heldVirtualKey);
        }

        return default;
    }

    /// <summary>Resets state and reports whether a synthetic key-up is needed.</summary>
    public WindowsKeyResetDecision Reset()
    {
        WindowsKeyResetDecision result = new(wasForwarded && isHeld, heldVirtualKey);
        isHeld = false;
        isPendingBareGesture = false;
        wasForwarded = false;
        heldVirtualKey = 0;
        return result;
    }

    private WindowsKeyDecision ProcessWindowsKeyDown(uint virtualKey)
    {
        if (!isHeld)
        {
            isHeld = true;
            isPendingBareGesture = true;
            wasForwarded = false;
            heldVirtualKey = virtualKey;
        }

        // Suppress the physical key down until we know whether it begins a
        // shortcut. Repeated physical Windows-key downs are suppressed too.
        return new WindowsKeyDecision(true, false, false, heldVirtualKey);
    }

    private WindowsKeyDecision ProcessWindowsKeyUp()
    {
        if (!isHeld)
        {
            return default;
        }

        bool togglePanel = isPendingBareGesture && !wasForwarded;
        bool suppress = !wasForwarded;
        Reset();
        return new WindowsKeyDecision(suppress, togglePanel, false, 0);
    }
}

/// <summary>Keyboard key direction.</summary>
public enum KeyTransition
{
    /// <summary>A key-down or system-key-down event.</summary>
    Down,

    /// <summary>A key-up or system-key-up event.</summary>
    Up,
}

/// <summary>Action requested after processing a keyboard event.</summary>
public readonly record struct WindowsKeyDecision(
    bool Suppress,
    bool TogglePanel,
    bool ForwardWindowsKeyDown,
    uint WindowsVirtualKey);

/// <summary>Native cleanup requested when the state machine is reset.</summary>
public readonly record struct WindowsKeyResetDecision(
    bool ReleaseForwardedWindowsKey,
    uint WindowsVirtualKey);
