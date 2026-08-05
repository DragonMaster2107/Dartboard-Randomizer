using Dartboard_Randomizer.Core.Board;
using Dartboard_Randomizer.Core.Models;
using Dartboard_Randomizer.Core.Scoring;
using Dartboard_Randomizer.Core.ViewModels;

namespace Dartboard_Randomizer.Tests;

/// <summary>
/// Sabotage (Conquest-Untermodus): Ein Treffer auf ein <b>fremdes</b> Feld wird dem Besitzer
/// <b>aufgeschlagen</b> statt abgezogen. Eigene, freie und gemeinsame Felder zählen normal.
/// </summary>
public class SabotageTests
{
    private const int Seed = 2024;

    private static readonly BoardLayout Board = BoardLayout.Shuffled(Seed);

    private static GameState NewGame(int score = 501, int players = 2, OutMode mode = OutMode.Double)
        => GameState.CreateNew(new GameSettings(
            PlayerNames: Enumerable.Range(1, players).Select(i => $"P{i}").ToArray(),
            StartingScore: score,
            OutMode: mode,
            Randomize: true,
            HiddenValues: true,
            Seed: Seed)
        {
            Conquest = true,
            Sabotage = true,
        });

    private static BoardPosition PositionOf(FieldValue value, int index = 0)
        => Board.PositionsOf(value).ElementAt(index);

    private static FieldValue Triple(int n) => new(n, Multiplier.Triple);
    private static FieldValue Single(int n) => new(n, Multiplier.Single);
    private static FieldValue Double(int n) => new(n, Multiplier.Double);

    /// <summary>Bringt das Feld in P1s Besitz und übergibt den Zug an P2.</summary>
    private static GameState ClaimedByFirstPlayer(GameState state, BoardPosition pos)
    {
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss, pos); // claimt ohne Punkte
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss);
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss);      // -> P2
        return state;
    }

    [Fact]
    public void Hitting_a_foreign_field_adds_points_to_the_owner()
    {
        var state = NewGame();
        var pos = PositionOf(Triple(20));
        state = ClaimedByFirstPlayer(state, pos);
        Assert.Equal(1, state.CurrentPlayerIndex);

        var after = ScoringEngine.ApplyThrow(state, Triple(20), pos);

        Assert.Equal(561, after.Players[0].Score); // 501 + 60 statt 441
        Assert.Equal(501, after.Players[1].Score); // Werfer unverändert
    }

    [Fact]
    public void Own_fields_still_count_down()
    {
        var state = NewGame();
        var pos = PositionOf(Triple(20));

        // P1 claimt und trifft im selben Zug erneut -> eigenes Feld, also Abzug
        state = ScoringEngine.ApplyThrow(state, Triple(20), pos);
        Assert.Equal(441, state.Players[0].Score);

        var after = ScoringEngine.ApplyThrow(state, Triple(20), pos);
        Assert.Equal(381, after.Players[0].Score);
    }

    [Fact]
    public void Free_and_safe_fields_still_count_down_for_the_thrower()
    {
        var state = NewGame();
        var safeDouble1 = PositionOf(Double(1));
        var free = PositionOf(Triple(19));

        state = ScoringEngine.ApplyThrow(state, Double(1), safeDouble1);
        Assert.Equal(499, state.Players[0].Score); // gemeinsames Feld: normal abgezogen

        var after = ScoringEngine.ApplyThrow(state, Triple(19), free);
        Assert.Equal(442, after.Players[0].Score); // freies Feld: normal abgezogen
    }

    [Fact]
    public void The_dart_still_counts_for_the_thrower()
    {
        var state = NewGame();
        var pos = PositionOf(Triple(20));
        state = ClaimedByFirstPlayer(state, pos);

        var after = ScoringEngine.ApplyThrow(state, Triple(20), pos);

        Assert.Equal(1, after.Players[1].DartsThrown);
        Assert.Single(after.CurrentTurn);
    }

    [Fact]
    public void Sabotage_can_push_a_score_above_the_starting_value()
    {
        var state = NewGame(score: 40);
        var pos = PositionOf(Triple(20));
        state = ClaimedByFirstPlayer(state, pos);

        var after = ScoringEngine.ApplyThrow(state, Triple(20), pos);

        Assert.Equal(100, after.Players[0].Score); // 40 + 60, kein Bust
        Assert.Equal(1, after.CurrentPlayerIndex); // P2 wirft weiter
        Assert.False(after.Players[0].HasFinished);
    }

    [Fact]
    public void A_foreign_hit_can_never_check_the_owner_out()
    {
        // Ohne Sabotage würde dieser Treffer P1 auf 0 setzen (siehe ConquestTests).
        var state = NewGame(score: 40);
        var pos = PositionOf(Double(20));
        state = ClaimedByFirstPlayer(state, pos);

        var after = ScoringEngine.ApplyThrow(state, Double(20), pos);

        Assert.Equal(80, after.Players[0].Score);
        Assert.Null(after.Players[0].FinishRank);
        Assert.False(after.AwaitingContinueDecision);
        Assert.False(after.IsOver);
    }

    [Fact]
    public void Own_bust_still_ends_the_turn()
    {
        var state = NewGame(score: 10);
        var free = PositionOf(Triple(20));

        var after = ScoringEngine.ApplyThrow(state, Triple(20), free); // 60 auf 10

        Assert.Equal(10, after.Players[0].Score);
        Assert.Equal(1, after.CurrentPlayerIndex);
    }

    [Fact]
    public void You_can_still_check_yourself_out()
    {
        var state = NewGame(score: 40, players: 2);

        var after = ScoringEngine.ApplyThrow(state, Double(20));

        Assert.Equal(0, after.Players[0].Score);
        Assert.Equal(1, after.Players[0].FinishRank);
        Assert.True(after.AwaitingContinueDecision);
    }

    [Fact]
    public void A_finished_owner_frees_the_field_so_it_counts_down_again()
    {
        var state = NewGame(score: 40, players: 2);
        var pos = PositionOf(Single(7));

        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss, pos); // P1 erobert
        state = ScoringEngine.ApplyThrow(state, Double(20));           // P1 fertig
        state = ScoringEngine.ResumeAfterFinish(state);

        // Feld ist frei -> normaler Abzug beim Werfer, keine Sabotage
        var after = ScoringEngine.ApplyThrow(state, Single(7), pos);

        Assert.Equal(33, after.Players[1].Score);
        Assert.Equal(1, FieldOwnership.ActiveOwner(after, pos));
    }

    [Fact]
    public void Reveal_does_not_score_still_wins_over_sabotage()
    {
        // Der aufdeckende Dart zählt 0 — und deckt ein FREIES Feld auf, also gibt es
        // ohnehin keinen fremden Besitzer.
        var controller = new GameController();
        controller.StartGame(new GameSettings(
            new[] { "P1", "P2" }, 501, OutMode.Double,
            Randomize: true, HiddenValues: true, Seed: Seed)
        {
            Conquest = true,
            Sabotage = true,
            RevealDoesNotScore = true,
        });

        var pos = PositionOf(Triple(20));
        controller.RecordThrow(Triple(20), pos);

        Assert.Equal(501, controller.Current!.Players[0].Score);
        Assert.Equal(0, FieldOwnership.ActiveOwner(controller.Current, pos));
    }

    [Fact]
    public void Undo_takes_the_added_points_back()
    {
        var controller = new GameController();
        controller.StartGame(new GameSettings(
            new[] { "P1", "P2" }, 501, OutMode.Double,
            Randomize: true, HiddenValues: true, Seed: Seed) { Conquest = true, Sabotage = true });

        var pos = PositionOf(Triple(20));
        controller.RecordThrow(Triple(20), pos);       // P1 erobert, 441
        controller.RecordThrow(FieldValue.Miss);
        controller.RecordThrow(FieldValue.Miss);       // -> P2
        controller.RecordThrow(Triple(20), pos);       // P2 sabotiert P1 -> 501
        Assert.Equal(501, controller.Current!.Players[0].Score);

        controller.Undo();

        Assert.Equal(441, controller.Current!.Players[0].Score);
    }

    // ---------- Setup-Kopplung ----------

    [Fact]
    public void Sanitized_drops_sabotage_without_conquest()
    {
        var setup = SetupDefaults.Initial with
        {
            Randomize = true,
            HiddenValues = true,
            Conquest = false,
            Sabotage = true,
        };

        Assert.False(setup.Sanitized().Sabotage);
    }

    [Fact]
    public void Sanitized_drops_sabotage_when_hidden_falls_away()
    {
        // Hidden aus -> Conquest fällt weg -> Sabotage muss ebenfalls fallen.
        var setup = SetupDefaults.Initial with
        {
            Randomize = true,
            HiddenValues = false,
            Conquest = true,
            Sabotage = true,
        };

        var clean = setup.Sanitized();

        Assert.False(clean.Conquest);
        Assert.False(clean.Sabotage);
    }

    [Fact]
    public void Sanitized_keeps_sabotage_with_conquest()
    {
        var setup = SetupDefaults.Initial with
        {
            Randomize = true,
            HiddenValues = true,
            Conquest = true,
            Sabotage = true,
        };

        Assert.True(setup.Sanitized().Sabotage);
    }

    [Fact]
    public void Without_sabotage_a_foreign_hit_still_subtracts()
    {
        // Gegenprobe: derselbe Wurf ohne den Modifikator zieht ab (bisheriges Verhalten).
        var state = GameState.CreateNew(new GameSettings(
            new[] { "P1", "P2" }, 501, OutMode.Double,
            Randomize: true, HiddenValues: true, Seed: Seed) { Conquest = true });
        var pos = PositionOf(Triple(20));
        state = ClaimedByFirstPlayer(state, pos);

        var after = ScoringEngine.ApplyThrow(state, Triple(20), pos);

        Assert.Equal(441, after.Players[0].Score);
    }
}
