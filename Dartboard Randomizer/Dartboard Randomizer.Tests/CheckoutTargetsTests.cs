using Dartboard_Randomizer.Core.Board;
using Dartboard_Randomizer.Core.Scoring;

namespace Dartboard_Randomizer.Tests;

public class CheckoutTargetsTests
{
    private static readonly BoardLayout Board = BoardLayout.Standard();
    private static readonly IReadOnlySet<BoardPosition> NothingRevealed = new HashSet<BoardPosition>();

    private static readonly FieldValue Single7 = new(7, Multiplier.Single);
    private static readonly FieldValue Triple20 = new(20, Multiplier.Triple);
    private static readonly FieldValue Double20 = new(20, Multiplier.Double);

    [Fact]
    public void No_route_means_no_targets()
    {
        Assert.Empty(CheckoutTargets.For(null, Board, hiddenValues: false, NothingRevealed));
        Assert.Empty(CheckoutTargets.For(Array.Empty<FieldValue>(), Board, false, NothingRevealed));
    }

    [Fact]
    public void Without_a_layout_there_is_nothing_to_mark()
    {
        Assert.Empty(CheckoutTargets.For(new[] { Triple20 }, layout: null, false, NothingRevealed));
    }

    [Fact]
    public void A_single_maps_to_both_of_its_positions_when_nothing_is_hidden()
    {
        var targets = CheckoutTargets.For(new[] { Single7 }, Board, hiddenValues: false, NothingRevealed);

        Assert.Equal(2, targets.Count);
        Assert.All(targets, t => Assert.Equal(Single7, Board.ValueAt(t.Position)));
        Assert.All(targets, t => Assert.Equal(0, t.Step));
    }

    // Der eigentliche Punkt: im Hidden-Modus darf das Highlight kein verdecktes Feld verraten.
    [Fact]
    public void Hidden_mode_marks_only_the_revealed_one_of_two_identical_singles()
    {
        var both = Board.PositionsOf(Single7).ToList();
        Assert.Equal(2, both.Count); // Vorbedingung: es gibt wirklich zwei

        var revealed = new HashSet<BoardPosition> { both[0] };

        var targets = CheckoutTargets.For(new[] { Single7 }, Board, hiddenValues: true, revealed);

        var only = Assert.Single(targets);
        Assert.Equal(both[0], only.Position);
        Assert.DoesNotContain(targets, t => t.Position == both[1]);
    }

    [Fact]
    public void Hidden_mode_marks_nothing_when_no_position_of_that_value_is_revealed()
    {
        var targets = CheckoutTargets.For(new[] { Single7 }, Board, hiddenValues: true, NothingRevealed);

        Assert.Empty(targets);
    }

    [Fact]
    public void Without_hidden_mode_revealed_is_irrelevant()
    {
        // Gleiche Eingabe wie oben, nur hiddenValues: false -> beide Positionen erlaubt.
        var targets = CheckoutTargets.For(new[] { Single7 }, Board, hiddenValues: false, NothingRevealed);

        Assert.Equal(2, targets.Count);
    }

    [Fact]
    public void A_field_used_twice_keeps_the_earliest_step()
    {
        // T20, T20, D20 -> die T20-Position gehört zu Schritt 0, nicht zu Schritt 1.
        var targets = CheckoutTargets.For(
            new[] { Triple20, Triple20, Double20 }, Board, hiddenValues: false, NothingRevealed);

        Assert.Equal(2, targets.Count);
        Assert.Equal(0, targets.Single(t => Board.ValueAt(t.Position) == Triple20).Step);
        Assert.Equal(2, targets.Single(t => Board.ValueAt(t.Position) == Double20).Step);
    }

    [Fact]
    public void Steps_follow_the_order_of_the_route()
    {
        var targets = CheckoutTargets.For(
            new[] { Triple20, Double20 }, Board, hiddenValues: false, NothingRevealed);

        Assert.Equal(0, targets.Single(t => Board.ValueAt(t.Position) == Triple20).Step);
        Assert.Equal(1, targets.Single(t => Board.ValueAt(t.Position) == Double20).Step);
    }

    [Fact]
    public void Works_on_a_shuffled_board_and_points_at_the_moved_positions()
    {
        var shuffled = BoardLayout.Shuffled(seed: 2024);

        var targets = CheckoutTargets.For(new[] { Triple20 }, shuffled, hiddenValues: false, NothingRevealed);

        var only = Assert.Single(targets);
        Assert.Equal(Triple20, shuffled.ValueAt(only.Position));
    }

    [Fact]
    public void Hidden_mode_on_a_shuffled_board_still_only_marks_revealed_positions()
    {
        var shuffled = BoardLayout.Shuffled(seed: 31337);
        var singlePositions = shuffled.PositionsOf(Single7).ToList();
        var revealed = new HashSet<BoardPosition> { singlePositions[1] };

        var targets = CheckoutTargets.For(new[] { Single7 }, shuffled, hiddenValues: true, revealed);

        var only = Assert.Single(targets);
        Assert.Equal(singlePositions[1], only.Position);
    }
}
