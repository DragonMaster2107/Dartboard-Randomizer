using Dartboard_Randomizer.Core.Board;
using Dartboard_Randomizer.Core.Models;
using Dartboard_Randomizer.Core.Scoring;

namespace Dartboard_Randomizer.Tests;

/// <summary>
/// Gewichteter Checkout (nur Randomize-Modus): unter Wegen gleicher Länge soll der
/// physisch am leichtesten zu treffende gewinnen.
/// </summary>
public class CheckoutDifficultyTests
{
    private static readonly FieldValue D8 = new(8, Multiplier.Double);    // 16 Punkte
    private static readonly FieldValue S7 = new(7, Multiplier.Single);    //  7
    private static readonly FieldValue T6 = new(6, Multiplier.Triple);    // 18
    private static readonly FieldValue S5 = new(5, Multiplier.Single);    //  5

    private static readonly IReadOnlyCollection<FieldValue> Available = new[] { D8, S7, T6, S5 };

    // Das Beispiel aus der Anforderung: D8 und 7 liegen auf breiten Single-Flächen,
    // T6 und 5 auf den schmalen Ringen. Im Shuffle-Modus ist das genau so möglich.
    private static readonly Dictionary<FieldValue, int> Difficulty = new()
    {
        [D8] = FieldDifficulty.Of(BoardRing.OuterSingle),
        [S7] = FieldDifficulty.Of(BoardRing.OuterSingle),
        [T6] = FieldDifficulty.Of(BoardRing.Triple),
        [S5] = FieldDifficulty.Of(BoardRing.Double),
    };

    [Fact]
    public void Without_weighting_the_hard_ring_route_wins()
    {
        // 23 = 18 (T6) + 5 — beide auf schmalen Ringen, aber punktemäßig "schöner".
        var path = CheckoutCalculator.Suggest(23, 3, OutMode.Straight, Available);

        Assert.Equal(new[] { T6, S5 }, path);
    }

    [Fact]
    public void With_weighting_the_easy_single_route_wins()
    {
        // 23 = 16 (D8) + 7 — beide auf breiten Single-Flächen.
        var path = CheckoutCalculator.Suggest(23, 3, OutMode.Straight, Available, Difficulty);

        Assert.Equal(new[] { D8, S7 }, path);
    }

    [Fact]
    public void Weighting_never_breaks_the_out_rule()
    {
        // Bei Double-Out muss der letzte Dart eine Double-WERTIGKEIT sein — unabhängig
        // davon, wie leicht die Position zu treffen ist.
        var path = CheckoutCalculator.Suggest(23, 3, OutMode.Double, Available, Difficulty);

        Assert.NotNull(path);
        Assert.True(path![^1].IsDouble, $"letzter Dart war {path[^1].ShortLabel}");
        Assert.Equal(23, path.Sum(v => v.Points));
    }

    [Fact]
    public void Fewer_darts_still_beats_an_easier_longer_route()
    {
        var hardButSingleDart = new FieldValue(10, Multiplier.Double);  // 20 Punkte, Triple-Position
        var easySmall = new FieldValue(10, Multiplier.Single);          // 10 Punkte, Outer Single

        var available = new[] { hardButSingleDart, easySmall };
        var difficulty = new Dictionary<FieldValue, int>
        {
            [hardButSingleDart] = FieldDifficulty.Of(BoardRing.Triple),
            [easySmall] = FieldDifficulty.Of(BoardRing.OuterSingle),
        };

        // 20 ginge als 1 Dart (teuer) oder 2× 10 (billig) — kürzer gewinnt.
        var path = CheckoutCalculator.Suggest(20, 3, OutMode.Straight, available, difficulty);

        Assert.Equal(new[] { hardButSingleDart }, path);
    }

    [Fact]
    public void Cheapest_route_is_found_even_when_it_is_not_the_greedy_first_hit()
    {
        // 24: teuer wäre 12+12 (2× Double-Position), billig ist 20+4 (2× Single-Position).
        var d12 = new FieldValue(12, Multiplier.Single);  // 12, auf Double-Position
        var s20 = new FieldValue(20, Multiplier.Single);  // 20, auf Outer Single
        var s4 = new FieldValue(4, Multiplier.Single);    //  4, auf Outer Single

        var available = new[] { d12, s20, s4 };
        var difficulty = new Dictionary<FieldValue, int>
        {
            [d12] = FieldDifficulty.Of(BoardRing.Double),
            [s20] = FieldDifficulty.Of(BoardRing.OuterSingle),
            [s4] = FieldDifficulty.Of(BoardRing.OuterSingle),
        };

        var path = CheckoutCalculator.Suggest(24, 3, OutMode.Straight, available, difficulty);

        Assert.NotNull(path);
        Assert.Equal(2, path!.Count);
        Assert.Equal(24, path.Sum(v => v.Points));
        var cost = path.Sum(v => difficulty[v]);
        Assert.Equal(0, cost); // 20 + 4, beide auf Single-Flächen
    }

    [Fact]
    public void Unknown_values_are_treated_as_hardest_and_not_preferred()
    {
        var known = new FieldValue(10, Multiplier.Single);
        var unknown = new FieldValue(5, Multiplier.Double); // fehlt bewusst in der Map

        var available = new[] { known, unknown };
        var difficulty = new Dictionary<FieldValue, int> { [known] = 0 };

        // 20 = 2× 10 (bekannt, billig) statt 10 + unbekannt.
        var path = CheckoutCalculator.Suggest(20, 3, OutMode.Straight, available, difficulty);

        Assert.Equal(new[] { known, known }, path);
    }

    [Fact]
    public void Weighted_and_unweighted_agree_when_only_one_route_exists()
    {
        var only = new FieldValue(20, Multiplier.Double);
        var available = new[] { only };
        var difficulty = new Dictionary<FieldValue, int> { [only] = FieldDifficulty.Of(BoardRing.Triple) };

        Assert.Equal(
            CheckoutCalculator.Suggest(40, 3, OutMode.Double, available),
            CheckoutCalculator.Suggest(40, 3, OutMode.Double, available, difficulty));
    }

    [Fact]
    public void No_checkout_stays_no_checkout_with_weighting()
    {
        var path = CheckoutCalculator.Suggest(23, 3, OutMode.Double, new[] { T6 }, Difficulty);

        Assert.Null(path);
    }

    [Fact]
    public void On_a_shuffled_board_the_suggestion_lands_on_easier_rings_on_average()
    {
        // Realitätsnaher Gegentest über viele Seeds: die gewichtete Variante darf im Schnitt
        // nicht auf schwereren Positionen landen als die ungewichtete.
        var weighted = 0;
        var plain = 0;
        var compared = 0;

        for (var seed = 1; seed <= 60; seed++)
        {
            var board = BoardLayout.Shuffled(seed);
            var available = BoardLayout.AllPositions.Select(board.ValueAt).Distinct().ToList();
            var difficulty = FieldDifficulty.Map(board, BoardLayout.AllPositions);

            foreach (var remaining in new[] { 23, 41, 60, 77, 96 })
            {
                var a = CheckoutCalculator.Suggest(remaining, 3, OutMode.Double, available, difficulty);
                var b = CheckoutCalculator.Suggest(remaining, 3, OutMode.Double, available);
                if (a is null || b is null)
                    continue;

                compared++;
                weighted += a.Sum(v => difficulty[v]);
                plain += b.Sum(v => difficulty[v]);
            }
        }

        Assert.True(compared > 100, $"zu wenige Vergleiche: {compared}");
        Assert.True(weighted < plain, $"gewichtet {weighted} sollte günstiger sein als ungewichtet {plain}");
    }
}
