using StrataShell.Core.Input;

namespace StrataShell.Core.Tests.Input;

public sealed class WindowsKeyStateMachineTests
{
    [Fact]
    public void BareWindowsKeyIsSuppressedAndTogglesOnRelease()
    {
        WindowsKeyStateMachine machine = new();

        WindowsKeyDecision down = machine.Process(
            WindowsKeyStateMachine.LeftWindowsKey,
            KeyTransition.Down,
            isInjected: false);
        WindowsKeyDecision up = machine.Process(
            WindowsKeyStateMachine.LeftWindowsKey,
            KeyTransition.Up,
            isInjected: false);

        Assert.True(down.Suppress);
        Assert.False(down.TogglePanel);
        Assert.True(up.Suppress);
        Assert.True(up.TogglePanel);
    }

    [Fact]
    public void WindowsShortcutForwardsModifierAndDoesNotToggle()
    {
        WindowsKeyStateMachine machine = new();
        machine.Process(WindowsKeyStateMachine.LeftWindowsKey, KeyTransition.Down, false);

        WindowsKeyDecision shortcut = machine.Process(0x45, KeyTransition.Down, false);
        WindowsKeyDecision keyUp = machine.Process(0x45, KeyTransition.Up, false);
        WindowsKeyDecision windowsUp = machine.Process(
            WindowsKeyStateMachine.LeftWindowsKey,
            KeyTransition.Up,
            false);

        Assert.True(shortcut.ForwardWindowsKeyDown);
        Assert.Equal(WindowsKeyStateMachine.LeftWindowsKey, shortcut.WindowsVirtualKey);
        Assert.False(shortcut.Suppress);
        Assert.Equal(default, keyUp);
        Assert.False(windowsUp.Suppress);
        Assert.False(windowsUp.TogglePanel);
    }

    [Fact]
    public void InjectedEventsDoNotMutateState()
    {
        WindowsKeyStateMachine machine = new();

        WindowsKeyDecision injected = machine.Process(
            WindowsKeyStateMachine.LeftWindowsKey,
            KeyTransition.Down,
            isInjected: true);
        WindowsKeyDecision physicalUp = machine.Process(
            WindowsKeyStateMachine.LeftWindowsKey,
            KeyTransition.Up,
            isInjected: false);

        Assert.Equal(default, injected);
        Assert.Equal(default, physicalUp);
    }

    [Fact]
    public void ResetRequestsSyntheticReleaseOnlyAfterShortcutForwarding()
    {
        WindowsKeyStateMachine machine = new();
        machine.Process(WindowsKeyStateMachine.RightWindowsKey, KeyTransition.Down, false);
        machine.Process(0x52, KeyTransition.Down, false);

        WindowsKeyResetDecision reset = machine.Reset();

        Assert.True(reset.ReleaseForwardedWindowsKey);
        Assert.Equal(WindowsKeyStateMachine.RightWindowsKey, reset.WindowsVirtualKey);
    }
}
