using Dartboard_Randomizer.Core.Board;
using Dartboard_Randomizer.Core.Models;
using Dartboard_Randomizer.Core.Scoring;

namespace Dartboard_Randomizer.Tests;

public class CheckoutCalculatorTests
{
    // Alle Wertigkeiten einer Standard-Scheibe.
    private static readonly IReadOnlyCollection<FieldValue> Standard =
        BoardLayout.AllPositions.Select(BoardLayout.Standard().ValueAt).Distinct().ToList();

    private static FieldValue FV(int n, Multiplier m) => new(n, m);

    [Fact]
    public void Single_dart_double_out()
    {
        var path = CheckoutCalculator.Suggest(40, 1, OutMode.Double, Standard);
        Assert.Equal(new[] { FV(20, Multiplier.Double) }, path);
    }

    [Fact]
    public void Bull_finishes_fifty_on_double_out()
    {
        var path = CheckoutCalculator.Suggest(50, 1, OutMode.Double, Standard);
        Assert.NotNull(path);
        Assert.Single(path!);
        Assert.True(path![0].IsBull && path[0].IsDouble); // inner bull
    }

    [Fact]
    public void Master_out_allows_triple_finish()
    {
        var path = CheckoutCalculator.Suggest(60, 1, OutMode.Master, Standard);
        Assert.Equal(new[] { FV(20, Multiplier.Triple) }, path);
    }

    [Fact]
    public void Two_dart_checkout_prefers_high_first_dart()
    {
        var path = CheckoutCalculator.Suggest(100, 2, OutMode.Double, Standard);
        Assert.Equal(new[] { FV(20, Multiplier.Triple), FV(20, Multiplier.Double) }, path); // 60 + 40
    }

    [Fact]
    public void Prefers_fewer_darts()
    {
        // 40 can be done in one dart (D20) even with 3 darts available.
        var path = CheckoutCalculator.Suggest(40, 3, OutMode.Double, Standard);
        Assert.Single(path!);
    }

    [Fact]
    public void No_checkout_when_out_of_range_for_darts_left()
    {
        Assert.Null(CheckoutCalculator.Suggest(170, 2, OutMode.Double, Standard));
    }

    [Fact]
    public void Reachable_170_with_three_darts()
    {
        var path = CheckoutCalculator.Suggest(170, 3, OutMode.Double, Standard);
        Assert.NotNull(path);
        Assert.Equal(170, path!.Sum(v => v.Points));
        Assert.True(ScoringEngine.IsValidCheckout(path[^1], OutMode.Double));
    }

    [Fact]
    public void Straight_out_finishes_on_any_field()
    {
        var path = CheckoutCalculator.Suggest(7, 1, OutMode.Straight, Standard);
        Assert.Equal(new[] { FV(7, Multiplier.Single) }, path);
    }

    [Fact]
    public void Hidden_mode_only_uses_available_values()
    {
        // 10 rest, double-out, but the lowest available double is D6 (12) -> impossible.
        var available = new[]
        {
            FV(6, Multiplier.Double),  // 12
            FV(7, Multiplier.Double),  // 14
            FV(20, Multiplier.Single), // 20
            FV(5, Multiplier.Single),  // 5
        };
        Assert.Null(CheckoutCalculator.Suggest(10, 3, OutMode.Double, available));
    }

    [Fact]
    public void Hidden_mode_finds_checkout_with_available_double()
    {
        // 10 rest, and D5 IS available -> single-dart finish.
        var available = new[] { FV(5, Multiplier.Double), FV(20, Multiplier.Single) };
        var path = CheckoutCalculator.Suggest(10, 1, OutMode.Double, available);
        Assert.Equal(new[] { FV(5, Multiplier.Double) }, path);
    }
}
