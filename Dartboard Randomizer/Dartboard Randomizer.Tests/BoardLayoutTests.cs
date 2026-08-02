using Dartboard_Randomizer.Core.Board;

namespace Dartboard_Randomizer.Tests;

public class BoardLayoutTests
{
    [Fact]
    public void Board_has_82_positions()
    {
        Assert.Equal(82, BoardLayout.AllPositions.Count);
    }

    [Fact]
    public void Standard_layout_maps_positions_to_their_real_values()
    {
        var board = BoardLayout.Standard();

        Assert.Equal(60, board.ValueAt(new BoardPosition(20, BoardRing.Triple)).Points);
        Assert.Equal(40, board.ValueAt(new BoardPosition(20, BoardRing.Double)).Points);
        Assert.Equal(19, board.ValueAt(new BoardPosition(19, BoardRing.InnerSingle)).Points);
        Assert.Equal(25, board.ValueAt(new BoardPosition(25, BoardRing.OuterBull)).Points);
        Assert.Equal(50, board.ValueAt(new BoardPosition(25, BoardRing.InnerBull)).Points);
    }

    [Fact]
    public void Shuffled_keeps_the_same_multiset_of_values_as_standard()
    {
        var standard = ValuesOf(BoardLayout.Standard());
        var shuffled = ValuesOf(BoardLayout.Shuffled(seed: 12345));

        Assert.Equal(standard, shuffled); // same values, just reassigned
    }

    [Fact]
    public void Shuffled_actually_moves_values_around()
    {
        var standard = BoardLayout.Standard();
        var shuffled = BoardLayout.Shuffled(seed: 12345);

        var moved = BoardLayout.AllPositions.Count(p => standard.ValueAt(p) != shuffled.ValueAt(p));

        Assert.True(moved > 40, $"expected a real shuffle, only {moved} positions changed");
    }

    [Fact]
    public void Same_seed_produces_an_identical_board()
    {
        var a = BoardLayout.Shuffled(seed: 999);
        var b = BoardLayout.Shuffled(seed: 999);

        Assert.All(BoardLayout.AllPositions, p => Assert.Equal(a.ValueAt(p), b.ValueAt(p)));
    }

    [Fact]
    public void Different_seeds_produce_different_boards()
    {
        var a = BoardLayout.Shuffled(seed: 1);
        var b = BoardLayout.Shuffled(seed: 2);

        Assert.Contains(BoardLayout.AllPositions, p => a.ValueAt(p) != b.ValueAt(p));
    }

    [Fact]
    public void Shuffled_exposes_its_seed()
    {
        Assert.Equal(42, BoardLayout.Shuffled(42).Seed);
        Assert.Null(BoardLayout.Standard().Seed);
    }

    // ---------- PositionsOf: die Umkehrung von ValueAt (fürs Checkout-Highlight) ----------

    [Theory]
    [InlineData(1)]      // Standard-Layout
    [InlineData(77)]     // gemischt
    [InlineData(4711)]   // gemischt
    public void PositionsOf_round_trips_through_ValueAt(int seed)
    {
        var board = seed == 1 ? BoardLayout.Standard() : BoardLayout.Shuffled(seed);

        // Jede Position muss über ihre eigene Wertigkeit wiederauffindbar sein.
        Assert.All(BoardLayout.AllPositions,
            p => Assert.Contains(p, board.PositionsOf(board.ValueAt(p))));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(77)]
    public void PositionsOf_finds_both_singles_but_only_one_triple_or_double(int seed)
    {
        var board = seed == 1 ? BoardLayout.Standard() : BoardLayout.Shuffled(seed);

        // Singles gibt es doppelt (inner + outer) — beide Positionen sind gültige Treffer.
        Assert.Equal(2, board.PositionsOf(new FieldValue(20, Multiplier.Single)).Count());
        Assert.Single(board.PositionsOf(new FieldValue(20, Multiplier.Triple)));
        Assert.Equal(2, board.PositionsOf(new FieldValue(7, Multiplier.Single)).Count());
        Assert.Single(board.PositionsOf(new FieldValue(7, Multiplier.Double)));

        // Bull: 25 einfach (outer) und 50 (inner) je genau einmal.
        Assert.Single(board.PositionsOf(new FieldValue(25, Multiplier.Single)));
        Assert.Single(board.PositionsOf(new FieldValue(25, Multiplier.Double)));
    }

    [Fact]
    public void PositionsOf_returns_the_shuffled_position_not_the_standard_one()
    {
        var board = BoardLayout.Shuffled(seed: 2024);
        var t20 = new FieldValue(20, Multiplier.Triple);

        var pos = Assert.Single(board.PositionsOf(t20));

        Assert.Equal(t20, board.ValueAt(pos));
        // Der Wert ist gewandert — sonst wäre das Highlight im Shuffle-Modus sinnlos.
        Assert.NotEqual(t20, BoardLayout.Standard().ValueAt(pos));
    }

    [Fact]
    public void PositionsOf_returns_nothing_for_a_value_that_is_not_on_the_board()
    {
        var board = BoardLayout.Shuffled(seed: 5);

        Assert.Empty(board.PositionsOf(new FieldValue(21, Multiplier.Single)));
        Assert.Empty(board.PositionsOf(new FieldValue(25, Multiplier.Triple)));
        Assert.Empty(board.PositionsOf(FieldValue.Miss));
    }

    private static List<int> ValuesOf(BoardLayout board)
        => BoardLayout.AllPositions
            .Select(p => board.ValueAt(p))
            .Select(v => v.BaseNumber * 100 + (int)v.Multiplier) // stable key per (number, multiplier)
            .OrderBy(x => x)
            .ToList();
}
