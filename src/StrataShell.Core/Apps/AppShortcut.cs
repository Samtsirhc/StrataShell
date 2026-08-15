namespace StrataShell.Core.Apps;

/// <summary>Launchable shortcut discovered from the Windows Start menu.</summary>
/// <param name="Name">Display name.</param>
/// <param name="Path">Shell-executable shortcut or application path.</param>
public sealed record AppShortcut(string Name, string Path);
