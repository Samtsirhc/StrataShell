using StrataShell.Core.Layout;

namespace StrataShell.Core.Tests.Layout;

public sealed class AdaptiveGridLayoutTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(87, 1)]
    [InlineData(184, 2)]
    [InlineData(960, 10)]
    [InlineData(5000, 12)]
    public void CalculateColumns_AdaptsAndCaps(double width, int expected)
    {
        int columns = AdaptiveGridLayout.CalculateColumns(width, 88, 8);

        Assert.Equal(expected, columns);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-1)]
    public void CalculateColumns_InvalidAvailableWidth_FallsBackToOne(double width)
    {
        Assert.Equal(1, AdaptiveGridLayout.CalculateColumns(width, 88, 8));
    }

    [Fact]
    public void CalculateColumns_InvalidCellWidth_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => AdaptiveGridLayout.CalculateColumns(900, 0, 8));
    }
}
