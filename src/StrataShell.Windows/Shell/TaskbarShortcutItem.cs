using System.Windows.Media;
using StrataShell.Core.Apps;

namespace StrataShell.Windows.Shell;

/// <summary>Visual quick-launch item used by the custom taskbar.</summary>
public sealed record TaskbarShortcutItem(AppShortcut Shortcut, ImageSource? Icon);
