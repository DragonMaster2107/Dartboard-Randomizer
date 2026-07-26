using Dartboard_Randomizer.Core.Board;
using Dartboard_Randomizer.Core.Models;
using Dartboard_Randomizer.Core.Scoring;

namespace Dartboard_Randomizer.Tests;

public class ScoringEngineTests
{
    private static GameState Game(int score, OutMode mode = OutMode.Straight, int players = 1)
    {
        var names = Enumerable.Range(1, players).Select(i => $"P{i}").ToArray();
        return GameState.CreateNew(new GameSettings(names, score, mode, false, false, null));
    }

    private static FieldValue FV(int number, Multiplier m) => new(number, m);

    // ---------- reguläre Züge ----------

    [Fact]
    public void Throw_subtracts_points_and_stays_on_player()
    {
        var result = ScoringEngine.ApplyThrow(Game(501), FV(20, Multiplier.Triple));

        Assert.Equal(441, result.Players[0].Score);
        Assert.Equal(0, result.CurrentPlayerIndex);
        Assert.Single(result.CurrentTurn);
    }

    [Fact]
    public void Switches_to_next_player_after_three_darts()
    {
        var state = Game(501, players: 2);
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Single));
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Single));
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Single));

        Assert.Equal(1, state.CurrentPlayerIndex);
        Assert.Empty(state.CurrentTurn);
        Assert.Equal(441, state.Players[0].Score);
        Assert.Equal(501, state.TurnStartScore); // start score of the new player
    }

    [Fact]
    public void Miss_counts_as_a_dart_but_scores_nothing()
    {
        var state = Game(100, players: 2);
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss);
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss);
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss);

        Assert.Equal(100, state.Players[0].Score);
        Assert.Equal(1, state.CurrentPlayerIndex);
    }

    // ---------- Bust ----------

    [Fact]
    public void Overthrow_busts_and_reverts_to_turn_start()
    {
        var result = ScoringEngine.ApplyThrow(Game(40, OutMode.Double, players: 2), FV(20, Multiplier.Triple));

        Assert.Equal(40, result.Players[0].Score); // reverted
        Assert.Equal(1, result.CurrentPlayerIndex); // turn ended
    }

    [Fact]
    public void Leaving_one_busts_on_double_out()
    {
        var result = ScoringEngine.ApplyThrow(Game(21, OutMode.Double, players: 2), FV(20, Multiplier.Single));

        Assert.Equal(21, result.Players[0].Score);
        Assert.Equal(1, result.CurrentPlayerIndex);
    }

    [Fact]
    public void Reaching_zero_without_a_double_busts_on_double_out()
    {
        var result = ScoringEngine.ApplyThrow(Game(20, OutMode.Double, players: 2), FV(20, Multiplier.Single));

        Assert.Equal(20, result.Players[0].Score);
        Assert.Equal(1, result.CurrentPlayerIndex);
        Assert.False(result.IsOver);
    }

    [Fact]
    public void Bust_reverts_all_darts_of_the_turn()
    {
        var state = Game(100, OutMode.Straight, players: 2);
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Single)); // 80
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Triple)); // 20
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Triple)); // -40 -> bust

        Assert.Equal(100, state.Players[0].Score); // whole turn reverted
        Assert.Equal(1, state.CurrentPlayerIndex);
    }

    // ---------- Checkout / Gewinn ----------

    [Fact]
    public void Double_out_wins_on_a_double()
    {
        var result = ScoringEngine.ApplyThrow(Game(40, OutMode.Double), FV(20, Multiplier.Double));

        Assert.True(result.IsOver);
        Assert.Equal(1, result.Players[0].FinishRank);
        Assert.Equal(0, result.Players[0].Score);
    }

    [Fact]
    public void Double_out_wins_on_the_inner_bull()
    {
        var result = ScoringEngine.ApplyThrow(Game(50, OutMode.Double), new FieldValue(25, Multiplier.Double));

        Assert.True(result.IsOver);
    }

    [Fact]
    public void Straight_out_wins_on_any_field()
    {
        var result = ScoringEngine.ApplyThrow(Game(20, OutMode.Straight), FV(20, Multiplier.Single));

        Assert.True(result.IsOver);
    }

    [Fact]
    public void Master_out_wins_on_a_triple()
    {
        var result = ScoringEngine.ApplyThrow(Game(60, OutMode.Master), FV(20, Multiplier.Triple));

        Assert.True(result.IsOver);
    }

    [Fact]
    public void Master_out_busts_on_a_single_finish()
    {
        var result = ScoringEngine.ApplyThrow(Game(20, OutMode.Master, players: 2), FV(20, Multiplier.Single));

        Assert.False(result.IsOver);
        Assert.Equal(20, result.Players[0].Score);
    }

    [Fact]
    public void Throws_are_ignored_once_the_game_is_finished()
    {
        var won = ScoringEngine.ApplyThrow(Game(40, OutMode.Double), FV(20, Multiplier.Double));
        var after = ScoringEngine.ApplyThrow(won, FV(20, Multiplier.Single));

        Assert.Same(won, after);
    }

    // ---------- Mehrere Spieler: Finish / Ausspielen ----------

    [Fact]
    public void Checkout_with_others_remaining_pauses_for_the_playout_decision()
    {
        var result = ScoringEngine.ApplyThrow(Game(40, OutMode.Double, players: 2), FV(20, Multiplier.Double));

        Assert.True(result.AwaitingContinueDecision);
        Assert.False(result.IsOver);
        Assert.Equal(1, result.Players[0].FinishRank);
        Assert.Equal(0, result.CurrentPlayerIndex); // not advanced yet
    }

    [Fact]
    public void ResumeAfterFinish_advances_to_the_next_unfinished_player()
    {
        var finished = ScoringEngine.ApplyThrow(Game(40, OutMode.Double, players: 2), FV(20, Multiplier.Double));

        var resumed = ScoringEngine.ResumeAfterFinish(finished);

        Assert.False(resumed.AwaitingContinueDecision);
        Assert.Equal(1, resumed.CurrentPlayerIndex);
    }

    [Fact]
    public void Last_player_to_check_out_ends_the_game()
    {
        var state = Game(40, OutMode.Double, players: 2);
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Double)); // P1 finishes
        state = ScoringEngine.ResumeAfterFinish(state);                     // play on -> P2
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Double)); // P2 finishes

        Assert.True(state.IsOver);
        Assert.False(state.AwaitingContinueDecision);
        Assert.Equal(1, state.Players[0].FinishRank);
        Assert.Equal(2, state.Players[1].FinishRank);
    }

    [Fact]
    public void Finished_players_are_skipped_in_the_rotation()
    {
        // P1 finishes, resume; from then on turns should stay on P2.
        var state = Game(40, OutMode.Double, players: 2);
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Double)); // P1 out
        state = ScoringEngine.ResumeAfterFinish(state);                     // -> P2

        state = ScoringEngine.ApplyThrow(state, FV(1, Multiplier.Single));
        state = ScoringEngine.ApplyThrow(state, FV(1, Multiplier.Single));
        state = ScoringEngine.ApplyThrow(state, FV(1, Multiplier.Single));  // P2's 3 darts

        Assert.Equal(1, state.CurrentPlayerIndex); // still P2, P1 skipped
    }

    // ---------- Statistik ----------

    [Fact]
    public void Darts_thrown_are_counted()
    {
        var state = Game(501);
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Single));
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss);
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Single));

        Assert.Equal(3, state.Players[0].DartsThrown);
    }

    [Fact]
    public void Highest_turn_tracks_the_best_round()
    {
        var state = Game(501);
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Triple));
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Triple));
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Triple)); // 180, turn ends
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Single));
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Single));
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Single)); // 60, lower

        Assert.Equal(180, state.Players[0].HighestTurn);
    }

    [Fact]
    public void Score_progression_records_the_remaining_after_each_turn()
    {
        var state = Game(501);
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Single));
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Single));
        state = ScoringEngine.ApplyThrow(state, FV(20, Multiplier.Single)); // turn 1: 501 -> 441

        Assert.Equal(new[] { 501, 441 }, state.Players[0].ScoreProgression);
    }

    [Theory]
    [InlineData(OutMode.Straight, 5, Multiplier.Single, true)]
    [InlineData(OutMode.Double, 5, Multiplier.Single, false)]
    [InlineData(OutMode.Double, 5, Multiplier.Double, true)]
    [InlineData(OutMode.Master, 5, Multiplier.Triple, true)]
    [InlineData(OutMode.Master, 5, Multiplier.Single, false)]
    public void IsValidCheckout_follows_the_out_mode(OutMode mode, int number, Multiplier m, bool expected)
    {
        Assert.Equal(expected, ScoringEngine.IsValidCheckout(new FieldValue(number, m), mode));
    }
}
