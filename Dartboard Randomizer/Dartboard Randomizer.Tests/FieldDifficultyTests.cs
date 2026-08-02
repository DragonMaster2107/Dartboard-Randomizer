using Dartboard_Randomizer.Core.Board;

namespace Dartboard_Randomizer.Tests;

public class FieldDifficultyTests
{
    [Fact]
    public void Rings_are_ordered_from_easiest_to_hardest()
    {
        // Vom Nutzer vorgegebene Rangfolge.
        var order = new[]
        {
            BoardRing.OuterSingle,
            BoardRing.InnerSingle,
            BoardRing.OuterBull,
            BoardRing.Double,
            BoardRing.Triple,
            BoardRing.InnerBull,
        };

        var costs = order.Select(FieldDifficulty.Of).ToList();

        Assert.Equal(costs.OrderBy(c => c), costs);   // streng aufsteigend
        Assert.Equal(costs.Distinct().Count(), costs.Count);
    }

    [Fact]
    public void Singles_are_cheaper_than_the_rings()
    {
        Assert.True(FieldDifficulty.Of(BoardRing.OuterSingle) < FieldDifficulty.Of(BoardRing.Double));
        Assert.True(FieldDifficulty.Of(BoardRing.InnerSingle) < FieldDifficulty.Of(BoardRing.Double));
        Assert.True(FieldDifficulty.Of(BoardRing.Double) < FieldDifficulty.Of(BoardRing.Triple));
    }

    [Fact]
    public void Map_uses_the_easiest_position_a_value_sits_on()
    {
        var board = BoardLayout.Standard();
        var map = FieldDifficulty.Map(board, BoardLayout.AllPositions);

        // Single 20 liegt auf Inner (1) UND Outer (0) -> die leichtere zählt.
        Assert.Equal(FieldDifficulty.Of(BoardRing.OuterSingle), map[new FieldValue(20, Multiplier.Single)]);
        Assert.Equal(FieldDifficulty.Of(BoardRing.Double), map[new FieldValue(20, Multiplier.Double)]);
        Assert.Equal(FieldDifficulty.Of(BoardRing.Triple), map[new FieldValue(20, Multiplier.Triple)]);
        Assert.Equal(FieldDifficulty.Of(BoardRing.OuterBull), map[new FieldValue(25, Multiplier.Single)]);
        Assert.Equal(FieldDifficulty.Of(BoardRing.InnerBull), map[new FieldValue(25, Multiplier.Double)]);
    }

    [Fact]
    public void Map_only_looks_at_the_positions_it_is_given()
    {
        var board = BoardLayout.Standard();

        // Nur die Inner-Single-Position von 20 -> die günstigere Outer zählt hier nicht.
        var map = FieldDifficulty.Map(board, new[] { new BoardPosition(20, BoardRing.InnerSingle) });

        Assert.Equal(FieldDifficulty.Of(BoardRing.InnerSingle), map[new FieldValue(20, Multiplier.Single)]);
        Assert.Single(map);
    }

    [Fact]
    public void Map_follows_the_shuffle_not_the_standard_board()
    {
        var shuffled = BoardLayout.Shuffled(seed: 2024);
        var map = FieldDifficulty.Map(shuffled, BoardLayout.AllPositions);

        // Die Kosten einer Wertigkeit richten sich nach ihrer neuen Position.
        foreach (var (value, cost) in map)
        {
            var cheapest = shuffled.PositionsOf(value).Min(p => FieldDifficulty.Of(p.Ring));
            Assert.Equal(cheapest, cost);
        }
    }
}
