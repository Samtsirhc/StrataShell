using System.IO;
using StrataShell.Core.Apps;

namespace StrataShell.Windows.Apps;

/// <summary>Discovers launchable shortcuts from user and machine Start menus.</summary>
public static class StartMenuCatalog
{
    /// <summary>Enumerates and de-duplicates Start-menu shortcuts.</summary>
    public static IReadOnlyList<AppShortcut> GetShortcuts()
    {
        string? commonPrograms = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);
        string? userPrograms = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        Dictionary<string, AppShortcut> shortcuts = new(StringComparer.CurrentCultureIgnoreCase);

        foreach (string root in new[] { userPrograms, commonPrograms })
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                continue;
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(root, "*", new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    ReturnSpecialDirectories = false,
                })
                    .Where(static path => path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".appref-ms", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }
            catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
            {
                continue;
            }

            foreach (string path in files)
            {
                string name = Path.GetFileNameWithoutExtension(path).Trim();
                if (name.Length == 0)
                {
                    continue;
                }

                shortcuts.TryAdd(name, new AppShortcut(name, path));
            }
        }

        return shortcuts.Values
            .OrderBy(static shortcut => shortcut.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }
}
