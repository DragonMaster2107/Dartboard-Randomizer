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

    private static List<int> ValuesOf(BoardLayout board)
        => BoardLayout.AllPositions
            .Select(p => board.ValueAt(p))
            .Select(v => v.BaseNumber * 100 + (int)v.Multiplier) // stable key per (number, multiplier)
            .OrderBy(x => x)
            .ToList();
}
