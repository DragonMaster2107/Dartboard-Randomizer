using Dartboard_Randomizer.Core.Board;
using Dartboard_Randomizer.Core.Models;
using Dartboard_Randomizer.Core.Scoring;
using Dartboard_Randomizer.Core.ViewModels;

namespace Dartboard_Randomizer.Tests;

/// <summary>
/// Der Conquest-Modifikator: getroffene Felder gehören dem Spieler, der sie zuerst getroffen
/// hat — Punkte gehen an den Besitzer, nicht an den Werfer.
/// </summary>
public class ConquestTests
{
    private const int Seed = 12345;

    private static readonly BoardLayout Board = BoardLayout.Shuffled(Seed);

    private static GameState NewGame(
        int score = 501, int players = 2, OutMode mode = OutMode.Double)
        => GameState.CreateNew(new GameSettings(
            PlayerNames: Enumerable.Range(1, players).Select(i => $"P{i}").ToArray(),
            StartingScore: score,
            OutMode: mode,
            Randomize: true,
            HiddenValues: true,
            Seed: Seed)
        {
            Conquest = true,
        });

    /// <summary>Eine Position, auf der diese Wertigkeit liegt (Index bei Singles: 0 oder 1).</summary>
    private static BoardPosition PositionOf(FieldValue value, int index = 0)
        => Board.PositionsOf(value).ElementAt(index);

    private static FieldValue Triple(int n) => new(n, Multiplier.Triple);
    private static FieldValue Single(int n) => new(n, Multiplier.Single);
    private static FieldValue Double(int n) => new(n, Multiplier.Double);

    // ---------- Besitz ----------

    [Fact]
    public void First_hit_claims_the_field_for_the_thrower()
    {
        var state = NewGame();
        var pos = PositionOf(Triple(20));

        var next = ScoringEngine.ApplyThrow(state, Triple(20), pos);

        Assert.Equal(0, FieldOwnership.ActiveOwner(next, pos));
        Assert.Equal(441, next.Players[0].Score); // der claimende Dart zählt für ihn selbst
    }

    [Fact]
    public void Later_hits_score_for_the_owner_not_the_thrower()
    {
        var state = NewGame();
        var pos = PositionOf(Triple(20));

        // P1 claimed das Feld und wirft seinen Zug zu Ende -> P2 ist dran
        state = ScoringEngine.ApplyThrow(state, Triple(20), pos);
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss);
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss);
        Assert.Equal(1, state.CurrentPlayerIndex);

        // P2 trifft das Feld von P1 -> 60 Punkte gehen an P1
        var after = ScoringEngine.ApplyThrow(state, Triple(20), pos);

        Assert.Equal(381, after.Players[0].Score); // 441 - 60
        Assert.Equal(501, after.Players[1].Score); // unverändert
    }

    [Fact]
    public void The_two_single_positions_are_claimed_separately()
    {
        var state = NewGame();
        var first = PositionOf(Single(7), 0);
        var second = PositionOf(Single(7), 1);
        Assert.NotEqual(first, second);

        state = ScoringEngine.ApplyThrow(state, Single(7), first);

        // Nur die getroffene Position gehört P1 — die zweite Single 7 bleibt frei.
        Assert.Equal(0, FieldOwnership.ActiveOwner(state, first));
        Assert.Null(FieldOwnership.ActiveOwner(state, second));
    }

    [Fact]
    public void Thrower_always_counts_the_dart_even_when_someone_else_scores()
    {
        var state = NewGame();
        var pos = PositionOf(Triple(20));
        state = ScoringEngine.ApplyThrow(state, Triple(20), pos); // P1 claimed
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss);
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss); // -> P2

        var after = ScoringEngine.ApplyThrow(state, Triple(20), pos);

        Assert.Equal(1, after.Players[1].DartsThrown); // beim Werfer gezählt
        Assert.Single(after.CurrentTurn);              // und im laufenden Zug
    }

    [Fact]
    public void Without_the_modifier_nothing_is_claimed()
    {
        var state = GameState.CreateNew(new GameSettings(
            new[] { "P1", "P2" }, 501, OutMode.Double,
            Randomize: true, HiddenValues: true, Seed: Seed)); // Conquest = false
        var pos = PositionOf(Triple(20));

        var next = ScoringEngine.ApplyThrow(state, Triple(20), pos);

        Assert.Empty(next.FieldOwners);
        Assert.Null(FieldOwnership.ActiveOwner(next, pos));
    }

    // ---------- Gemeinsame Sicherheitsfelder ----------

    [Fact]
    public void Safe_fields_are_revealed_from_the_start()
    {
        var state = NewGame();

        Assert.Equal(2, state.SharedPositions.Count); // D1 + eine S1
        Assert.All(state.SharedPositions, p => Assert.Contains(p, state.RevealedPositions));
    }

    [Fact]
    public void Safe_fields_hold_the_double_1_and_exactly_one_single_1()
    {
        var shared = SafeFields.For(Board);

        Assert.Contains(PositionOf(Double(1)), shared);
        Assert.Equal(1, shared.Count(p => Board.ValueAt(p) == Single(1)));
    }

    [Fact]
    public void Safe_fields_cannot_be_claimed_and_score_for_the_thrower()
    {
        var state = NewGame();
        var safeDouble1 = PositionOf(Double(1));

        var next = ScoringEngine.ApplyThrow(state, Double(1), safeDouble1);

        Assert.Empty(next.FieldOwners);
        Assert.Null(FieldOwnership.ActiveOwner(next, safeDouble1));
        Assert.Equal(499, next.Players[0].Score); // die 2 Punkte bleiben beim Werfer
    }

    [Fact]
    public void Safe_fields_stay_open_even_after_another_player_hits_them()
    {
        var state = NewGame();
        var safeSingle1 = state.SharedPositions.First(p => Board.ValueAt(p) == Single(1));

        state = ScoringEngine.ApplyThrow(state, Single(1), safeSingle1);
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss);
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss); // -> P2

        var after = ScoringEngine.ApplyThrow(state, Single(1), safeSingle1);

        Assert.Equal(500, after.Players[1].Score); // P2 bekommt seinen eigenen Punkt
        Assert.Null(FieldOwnership.ActiveOwner(after, safeSingle1));
    }

    // ---------- Bust ----------

    [Fact]
    public void Overthrowing_a_foreign_field_fizzles_and_the_turn_goes_on()
    {
        // P2 steht auf 10, P1 wirft auf ein Feld von P2 mit 60 -> würde P2 busten
        var state = NewGame(score: 10);
        var pos = PositionOf(Triple(20));

        // P2 claimed das Feld zuerst
        state = state with { CurrentPlayerIndex = 1, TurnStartScore = 10 };
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss, pos);
        Assert.Equal(1, FieldOwnership.ActiveOwner(state, pos));

        state = state with { CurrentPlayerIndex = 0, CurrentTurn = Array.Empty<FieldValue>(), TurnStartScore = 10 };
        var after = ScoringEngine.ApplyThrow(state, Triple(20), pos);

        Assert.Equal(10, after.Players[1].Score);   // verpufft, kein Punktabzug
        Assert.Equal(0, after.CurrentPlayerIndex);  // P1 bleibt dran
        Assert.Single(after.CurrentTurn);           // und hat noch 2 Darts
    }

    [Fact]
    public void Busting_yourself_still_ends_the_turn()
    {
        var state = NewGame(score: 10);
        var free = PositionOf(Triple(20));

        var after = ScoringEngine.ApplyThrow(state, Triple(20), free); // 60 auf 10 -> Bust

        Assert.Equal(10, after.Players[0].Score);   // zurück auf Rundenstart
        Assert.Equal(1, after.CurrentPlayerIndex);  // Zug verfallen
    }

    // ---------- Auschecken für einen anderen ----------

    [Fact]
    public void You_can_check_out_another_player_and_keep_throwing()
    {
        // P2 steht auf 40 und besitzt ein D20-Feld; P1 trifft es -> P2 ist fertig
        var state = NewGame(score: 40);
        var pos = PositionOf(Double(20));

        state = state with { CurrentPlayerIndex = 1, TurnStartScore = 40 };
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss, pos); // P2 claimed
        state = state with { CurrentPlayerIndex = 0, CurrentTurn = Array.Empty<FieldValue>(), TurnStartScore = 40 };

        var after = ScoringEngine.ApplyThrow(state, Double(20), pos);

        Assert.Equal(0, after.Players[1].Score);
        Assert.Equal(1, after.Players[1].FinishRank);
        Assert.True(after.AwaitingContinueDecision);
        Assert.Equal(1, after.PendingFinisherIndex); // NICHT der Werfer

        // Nach "weiterspielen" wirft P1 seine Runde normal weiter.
        var resumed = ScoringEngine.ResumeAfterFinish(after);
        Assert.Equal(0, resumed.CurrentPlayerIndex);
        Assert.Single(resumed.CurrentTurn);
        Assert.False(resumed.AwaitingContinueDecision);
    }

    [Fact]
    public void Finishing_someone_with_the_last_dart_switches_players_after_the_dialog()
    {
        var state = NewGame(score: 40, players: 3);
        var pos = PositionOf(Double(20));

        state = state with { CurrentPlayerIndex = 1, TurnStartScore = 40 };
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss, pos); // P2 claimed
        state = state with
        {
            CurrentPlayerIndex = 0,
            // P1 hat schon zwei Darts geworfen -> der Treffer ist sein dritter
            CurrentTurn = new[] { FieldValue.Miss, FieldValue.Miss },
            TurnStartScore = state.Players[0].Score,
        };

        var after = ScoringEngine.ApplyThrow(state, Double(20), pos);
        Assert.True(after.AwaitingContinueDecision);

        var resumed = ScoringEngine.ResumeAfterFinish(after);

        Assert.NotEqual(0, resumed.CurrentPlayerIndex); // Zug war voll -> gewechselt
        Assert.Empty(resumed.CurrentTurn);
    }

    [Fact]
    public void Checking_out_yourself_still_ends_the_turn_after_the_dialog()
    {
        // Regression: das bisherige Verhalten darf sich nicht ändern.
        var state = NewGame(score: 40, players: 2);

        var after = ScoringEngine.ApplyThrow(state, Double(20));
        Assert.True(after.AwaitingContinueDecision);
        Assert.Equal(0, after.PendingFinisherIndex);

        var resumed = ScoringEngine.ResumeAfterFinish(after);

        Assert.Equal(1, resumed.CurrentPlayerIndex);
        Assert.Empty(resumed.CurrentTurn);
    }

    // ---------- Besitzer ausgecheckt -> Feld wieder frei ----------

    [Fact]
    public void Fields_of_a_finished_player_become_claimable_again()
    {
        var state = NewGame(score: 40, players: 2);
        // Kleine Wertigkeit, damit der Treffer den Reststand von 40 nicht überwirft.
        var pos = PositionOf(Single(7));

        // P1 claimed das Feld und checkt anschließend selbst aus
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss, pos);
        Assert.Equal(0, FieldOwnership.ActiveOwner(state, pos));

        state = ScoringEngine.ApplyThrow(state, Double(20)); // P1 fertig
        state = ScoringEngine.ResumeAfterFinish(state);
        Assert.True(state.Players[0].HasFinished);

        // Der Besitz ist erloschen ...
        Assert.Null(FieldOwnership.ActiveOwner(state, pos));

        // ... und der nächste Treffer beansprucht das Feld neu, samt Punkten.
        var after = ScoringEngine.ApplyThrow(state, Single(7), pos);
        Assert.Equal(1, FieldOwnership.ActiveOwner(after, pos));
        Assert.Equal(33, after.Players[1].Score); // 40 - 7
    }

    // ---------- Markierung und Checkout-Basis ----------

    [Fact]
    public void ForeignTo_lists_only_other_players_active_fields()
    {
        var state = NewGame();
        var mine = PositionOf(Triple(20));
        var theirs = PositionOf(Triple(19));
        var safe = PositionOf(Double(1));

        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss, mine);   // P1
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss, safe);   // shared, keine Eroberung
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss);         // -> P2
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss, theirs); // P2

        var foreignToP1 = FieldOwnership.ForeignTo(state, 0);

        Assert.Contains(theirs, foreignToP1);
        Assert.DoesNotContain(mine, foreignToP1);
        Assert.DoesNotContain(safe, foreignToP1);
    }

    [Fact]
    public void UsableBy_covers_own_shared_and_free_fields_but_not_foreign_ones()
    {
        var state = NewGame();
        var mine = PositionOf(Triple(20));
        var theirs = PositionOf(Triple(19));
        var free = PositionOf(Triple(18));
        var safe = PositionOf(Double(1));

        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss, mine);   // P1
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss);
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss);         // -> P2
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss, theirs); // P2

        var usable = FieldOwnership.UsableBy(state, 0, BoardLayout.AllPositions);

        Assert.Contains(mine, usable);
        Assert.Contains(free, usable);
        Assert.Contains(safe, usable);
        Assert.DoesNotContain(theirs, usable);
    }

    [Fact]
    public void Checkout_targets_skip_the_foreign_copy_of_a_single()
    {
        // Beide Single-7-Positionen aufgedeckt, eine gehört dem Gegner -> nur die eigene
        // darf markiert werden.
        var state = NewGame();
        var mine = PositionOf(Single(7), 0);
        var theirs = PositionOf(Single(7), 1);

        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss, mine);   // P1
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss);
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss);         // -> P2
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss, theirs); // P2
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss);
        state = ScoringEngine.ApplyThrow(state, FieldValue.Miss);         // -> P1

        var revealed = new HashSet<BoardPosition> { mine, theirs };
        var usable = FieldOwnership.UsableBy(state, 0, BoardLayout.AllPositions);

        var targets = CheckoutTargets.For(
            new[] { Single(7) }, Board, hiddenValues: true, revealed, usable);

        Assert.Single(targets);
        Assert.Equal(mine, targets[0].Position);
    }

    // ---------- Undo ----------

    [Fact]
    public void Undo_takes_the_claim_back()
    {
        var controller = new GameController();
        controller.StartGame(new GameSettings(
            new[] { "P1", "P2" }, 501, OutMode.Double,
            Randomize: true, HiddenValues: true, Seed: Seed) { Conquest = true });

        var pos = PositionOf(Triple(20));
        controller.RecordThrow(Triple(20), pos);
        Assert.Equal(0, FieldOwnership.ActiveOwner(controller.Current!, pos));

        controller.Undo();

        Assert.Null(FieldOwnership.ActiveOwner(controller.Current!, pos));
        Assert.Empty(controller.Current!.FieldOwners);
    }

    [Fact]
    public void RevealDoesNotScore_still_claims_the_field()
    {
        // Beide Modifikatoren zusammen: der aufdeckende Dart zählt 0, beansprucht das Feld aber.
        var controller = new GameController();
        controller.StartGame(new GameSettings(
            new[] { "P1", "P2" }, 501, OutMode.Double,
            Randomize: true, HiddenValues: true, Seed: Seed)
        {
            Conquest = true,
            RevealDoesNotScore = true,
        });

        var pos = PositionOf(Triple(20));
        controller.RecordThrow(Triple(20), pos);

        Assert.Equal(501, controller.Current!.Players[0].Score); // 0 Punkte
        Assert.Equal(0, FieldOwnership.ActiveOwner(controller.Current, pos)); // aber geclaimed
    }

    // ---------- Setup-Kopplung ----------

    [Fact]
    public void Sanitized_drops_claim_without_hidden()
    {
        var setup = SetupDefaults.Initial with
        {
            Randomize = true,
            HiddenValues = false,
            Conquest = true,
        };

        Assert.False(setup.Sanitized().Conquest);
    }

    [Fact]
    public void Sanitized_keeps_claim_with_hidden()
    {
        var setup = SetupDefaults.Initial with
        {
            Randomize = true,
            HiddenValues = true,
            Conquest = true,
        };

        Assert.True(setup.Sanitized().Conquest);
    }
}
