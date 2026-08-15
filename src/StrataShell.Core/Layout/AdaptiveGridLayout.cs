namespace StrataShell.Core.Layout;

/// <summary>Deterministic adaptive-grid calculation shared by UI and tests.</summary>
public static class AdaptiveGridLayout
{
    /// <summary>Calculates a stable number of columns for available width.</summary>
    /// <param name="availableWidth">Content width after outer margins.</param>
    /// <param name="minimumCellWidth">Minimum visual cell width.</param>
    /// <param name="gap">Gap between cells.</param>
    /// <param name="maximumColumns">Maximum columns allowed by the design.</param>
    /// <returns>A column count between one and <paramref name="maximumColumns"/>.</returns>
    public static int CalculateColumns(
        double availableWidth,
        double minimumCellWidth,
        double gap,
        int maximumColumns = 12)
    {
        if (!double.IsFinite(availableWidth) || availableWidth <= 0)
        {
            return 1;
        }

        if (!double.IsFinite(minimumCellWidth) || minimumCellWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumCellWidth));
        }

        if (!double.IsFinite(gap) || gap < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gap));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(maximumColumns, 1);

        int columns = (int)Math.Floor((availableWidth + gap) / (minimumCellWidth + gap));
        return Math.Clamp(columns, 1, maximumColumns);
    }
}
