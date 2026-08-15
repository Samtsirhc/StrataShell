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

    [Theory]
    [InlineData(WindowsKeyStateMachine.LeftWindowsKey)]
    [InlineData(WindowsKeyStateMachine.RightWindowsKey)]
    public void RepeatedKeyDownStillProducesOneBareToggle(uint windowsKey)
    {
        WindowsKeyStateMachine machine = new();

        Assert.True(machine.Process(windowsKey, KeyTransition.Down, false).Suppress);
        Assert.True(machine.Process(windowsKey, KeyTransition.Down, false).Suppress);
        WindowsKeyDecision release = machine.Process(windowsKey, KeyTransition.Up, false);
        WindowsKeyDecision duplicateRelease = machine.Process(windowsKey, KeyTransition.Up, false);

        Assert.True(release.Suppress);
        Assert.True(release.TogglePanel);
        Assert.Equal(default, duplicateRelease);
    }

    [Fact]
    public void MultipleShortcutKeysForwardWindowsModifierOnlyOnce()
    {
        WindowsKeyStateMachine machine = new();
        machine.Process(WindowsKeyStateMachine.LeftWindowsKey, KeyTransition.Down, false);

        WindowsKeyDecision first = machine.Process(0x10, KeyTransition.Down, false);
        WindowsKeyDecision second = machine.Process(0x53, KeyTransition.Down, false);
        WindowsKeyDecision release = machine.Process(WindowsKeyStateMachine.LeftWindowsKey, KeyTransition.Up, false);

        Assert.True(first.ForwardWindowsKeyDown);
        Assert.False(second.ForwardWindowsKeyDown);
        Assert.False(release.Suppress);
        Assert.False(release.TogglePanel);
    }

    [Fact]
    public void TenThousandBareGesturesRemainIndependent()
    {
        WindowsKeyStateMachine machine = new();
        int toggles = 0;

        for (int index = 0; index < 10_000; index++)
        {
            WindowsKeyDecision down = machine.Process(WindowsKeyStateMachine.LeftWindowsKey, KeyTransition.Down, false);
            WindowsKeyDecision up = machine.Process(WindowsKeyStateMachine.LeftWindowsKey, KeyTransition.Up, false);
            Assert.True(down.Suppress);
            Assert.True(up.Suppress);
            if (up.TogglePanel)
            {
                toggles++;
            }
        }

        Assert.Equal(10_000, toggles);
        Assert.Equal(default, machine.Reset());
    }
}
