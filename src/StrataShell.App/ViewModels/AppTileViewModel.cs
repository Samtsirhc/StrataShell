using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using StrataShell.Core.Apps;

namespace StrataShell.App.ViewModels;

/// <summary>Presentation model for one Start-menu shortcut.</summary>
public sealed class AppTileViewModel : INotifyPropertyChanged
{
    private ImageSource? icon;
    private bool isPinned;

    /// <summary>Creates a tile that can be shown immediately while its icon loads.</summary>
    public AppTileViewModel(AppShortcut shortcut) => Shortcut = shortcut;

    /// <summary>Raised when the asynchronously extracted icon becomes available.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the launchable shortcut.</summary>
    public AppShortcut Shortcut { get; }

    /// <summary>Gets the user-facing app name.</summary>
    public string Name => Shortcut.Name;

    /// <summary>Gets a single-letter fallback when icon extraction is pending or fails.</summary>
    public string Initial => string.IsNullOrWhiteSpace(Name) ? "?" : Name[..1].ToUpperInvariant();

    /// <summary>Gets or sets the frozen icon image.</summary>
    public ImageSource? Icon
    {
        get => icon;
        set
        {
            if (ReferenceEquals(icon, value))
            {
                return;
            }

            icon = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
        }
    }

    /// <summary>Gets or sets whether this app is pinned to quick launch.</summary>
    public bool IsPinned
    {
        get => isPinned;
        set
        {
            if (isPinned == value)
            {
                return;
            }

            isPinned = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPinned)));
        }
    }
}
