using Dartboard_Randomizer.Core.Board;
using Dartboard_Randomizer.Core.Models;
using Dartboard_Randomizer.Core.ViewModels;

namespace Dartboard_Randomizer.Tests;

public class GameControllerThrowTests
{
    private static GameController Started(int score = 501, OutMode mode = OutMode.Double, int players = 2)
    {
        var controller = new GameController();
        var names = Enumerable.Range(1, players).Select(i => $"P{i}").ToArray();
        controller.StartGame(new GameSettings(names, score, mode, false, false, null));
        return controller;
    }

    [Fact]
    public void RecordThrow_updates_the_state_and_enables_undo()
    {
        var c = Started();

        c.RecordThrow(new FieldValue(20, Multiplier.Triple));

        Assert.Equal(441, c.Current!.Players[0].Score);
        Assert.True(c.CanUndo);
    }

    [Fact]
    public void Undo_restores_the_previous_state()
    {
        var c = Started();
        c.RecordThrow(new FieldValue(20, Multiplier.Triple));

        c.Undo();

        Assert.Equal(501, c.Current!.Players[0].Score);
        Assert.Empty(c.Current.CurrentTurn);
        Assert.False(c.CanUndo);
    }

    [Fact]
    public void Undo_steps_back_across_a_player_switch()
    {
        var c = Started();
        c.RecordThrow(new FieldValue(20, Multiplier.Single));
        c.RecordThrow(new FieldValue(20, Multiplier.Single));
        c.RecordThrow(new FieldValue(20, Multiplier.Single)); // 3rd dart -> switch to P2

        Assert.Equal(1, c.Current!.CurrentPlayerIndex);

        c.Undo(); // back to P1 with 2 darts thrown

        Assert.Equal(0, c.Current!.CurrentPlayerIndex);
        Assert.Equal(2, c.Current.CurrentTurn.Count);
    }

    [Fact]
    public void RecordThrow_is_ignored_after_the_game_is_won()
    {
        var c = Started(score: 40, mode: OutMode.Double, players: 1);
        c.RecordThrow(new FieldValue(20, Multiplier.Double)); // wins

        var scoreAfterWin = c.Current!.Players[0].Score;
        c.RecordThrow(new FieldValue(20, Multiplier.Single)); // ignored

        Assert.True(c.Current!.IsOver);
        Assert.Equal(scoreAfterWin, c.Current.Players[0].Score);
    }

    [Fact]
    public void Undo_on_empty_history_does_nothing()
    {
        var c = Started();

        c.Undo();

        Assert.Equal(501, c.Current!.Players[0].Score);
        Assert.False(c.CanUndo);
    }

    [Fact]
    public void Board_hit_reveals_the_position_and_undo_hides_it_again()
    {
        var c = Started(score: 100, mode: OutMode.Straight, players: 1);
        var pos = new BoardPosition(20, BoardRing.Triple);

        c.RecordThrow(new FieldValue(20, Multiplier.Triple), pos);
        Assert.Contains(pos, c.Current!.RevealedPositions);

        c.Undo();
        Assert.DoesNotContain(pos, c.Current!.RevealedPositions);
    }

    [Fact]
    public void RevealDoesNotScore_first_hit_scores_zero_but_repeat_hit_counts()
    {
        var c = new GameController();
        c.StartGame(new GameSettings(new[] { "Alice" }, 501, OutMode.Straight, Randomize: true, HiddenValues: true, Seed: 7)
        {
            RevealDoesNotScore = true,
        });

        var pos = new BoardPosition(20, BoardRing.Triple);
        var value = new FieldValue(20, Multiplier.Triple); // 60 points

        // first hit only reveals -> no score
        c.RecordThrow(value, pos);
        Assert.Equal(501, c.Current!.Players[0].Score);
        Assert.Contains(pos, c.Current.RevealedPositions);

        // hitting the now-revealed field scores normally
        c.RecordThrow(value, pos);
        Assert.Equal(441, c.Current!.Players[0].Score);
    }
}
