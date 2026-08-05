using Dartboard_Randomizer.Core.Board;
using Dartboard_Randomizer.Core.Models;
using Dartboard_Randomizer.Core.Scoring;
using Dartboard_Randomizer.Core.ViewModels;

namespace Dartboard_Randomizer.Tests;

/// <summary>
/// Aufdeck-Statistik: wer hat wie viele Felder freigelegt (Basis für Donut, Tabellenspalte
/// und die Board-Einfärbung in der Statistik).
/// </summary>
public class RevealStatsTests
{
    private const int Seed = 4242;

    private static readonly BoardLayout Board = BoardLayout.Shuffled(Seed);

    private static GameController Started(bool conquest = false, int players = 2)
    {
        var controller = new GameController();
        controller.StartGame(new GameSettings(
            PlayerNames: Enumerable.Range(1, players).Select(i => $"P{i}").ToArray(),
            StartingScore: 501,
            OutMode: OutMode.Straight,
            Randomize: true,
            HiddenValues: true,
            Seed: Seed)
        {
            Conquest = conquest,
        });
        return controller;
    }

    private static BoardPosition PositionOf(FieldValue value, int index = 0)
        => Board.PositionsOf(value).ElementAt(index);

    private static FieldValue Triple(int n) => new(n, Multiplier.Triple);

    [Fact]
    public void Revealing_a_field_records_the_thrower()
    {
        var c = Started();
        var pos = PositionOf(Triple(20));

        c.RecordThrow(Triple(20), pos);

        Assert.Equal(0, c.Current!.RevealedBy[pos]);
        Assert.Equal(new[] { 1, 0 }, RevealStats.CountsByPlayer(c.Current));
    }

    [Fact]
    public void Hitting_an_already_revealed_field_does_not_change_the_revealer()
    {
        var c = Started();
        var pos = PositionOf(Triple(20));

        c.RecordThrow(Triple(20), pos);   // P1 deckt auf
        c.RecordThrow(FieldValue.Miss);
        c.RecordThrow(FieldValue.Miss);   // -> P2
        c.RecordThrow(Triple(20), pos);   // P2 trifft dasselbe Feld

        Assert.Equal(0, c.Current!.RevealedBy[pos]); // bleibt bei P1
        Assert.Equal(new[] { 1, 0 }, RevealStats.CountsByPlayer(c.Current));
    }

    [Fact]
    public void The_revealer_is_the_thrower_even_on_the_third_dart()
    {
        // Regression: nach dem dritten Dart wechselt der Spieler — aufgedeckt hat aber der,
        // der geworfen hat.
        var c = Started();
        var pos = PositionOf(Triple(20));

        c.RecordThrow(FieldValue.Miss);
        c.RecordThrow(FieldValue.Miss);
        c.RecordThrow(Triple(20), pos);   // dritter Dart von P1

        Assert.Equal(1, c.Current!.CurrentPlayerIndex); // P2 ist dran ...
        Assert.Equal(0, c.Current.RevealedBy[pos]);     // ... aufgedeckt hat P1
    }

    [Fact]
    public void Undo_takes_the_reveal_attribution_back()
    {
        var c = Started();
        var pos = PositionOf(Triple(20));
        c.RecordThrow(Triple(20), pos);

        c.Undo();

        Assert.Empty(c.Current!.RevealedBy);
        Assert.Equal(new[] { 0, 0 }, RevealStats.CountsByPlayer(c.Current));
    }

    [Fact]
    public void RevealAll_is_not_attributed_to_anyone()
    {
        var c = Started();
        var pos = PositionOf(Triple(20));
        c.RecordThrow(Triple(20), pos); // ein erspieltes Feld

        c.RevealAll();

        // Alles sichtbar, aber nur das erspielte Feld ist zugeordnet.
        Assert.Equal(BoardLayout.AllPositions.Count, c.Current!.RevealedPositions.Count);
        Assert.Single(c.Current.RevealedBy);
        Assert.Equal(new[] { 1, 0 }, RevealStats.CountsByPlayer(c.Current));
    }

    [Fact]
    public void Safe_fields_are_not_attributed_and_leave_the_denominator()
    {
        var conquest = Started(conquest: true).Current!;
        var plain = Started(conquest: false).Current!;

        // Conquest: D1 + eine S1 sind von Anfang an sichtbar, aber von niemandem aufgedeckt.
        Assert.Equal(2, conquest.SharedPositions.Count);
        Assert.Empty(conquest.RevealedBy);
        Assert.Equal(BoardLayout.AllPositions.Count - 2, RevealStats.Revealable(conquest));

        // Ohne Conquest gibt es keine Sicherheitsfelder -> alle Positionen zählen.
        Assert.Equal(BoardLayout.AllPositions.Count, RevealStats.Revealable(plain));
    }

    [Fact]
    public void Unattributed_covers_everything_no_player_revealed()
    {
        var c = Started();
        c.RecordThrow(Triple(20), PositionOf(Triple(20)));

        var state = c.Current!;

        Assert.Equal(RevealStats.Revealable(state) - 1, RevealStats.Unattributed(state));
    }

    [Fact]
    public void Counts_ignore_a_player_index_out_of_range()
    {
        // Defensiv gegen manipulierten Storage.
        var state = Started().Current! with
        {
            RevealedBy = new Dictionary<BoardPosition, int>
            {
                [PositionOf(Triple(20))] = 0,
                [PositionOf(Triple(19))] = 99,
            },
        };

        Assert.Equal(new[] { 1, 0 }, RevealStats.CountsByPlayer(state));
    }

    [Fact]
    public void Reveal_attribution_survives_a_conquest_owner_change()
    {
        // Aufdecker bleibt fix, auch wenn der Besitz später wechselt — genau deshalb sind
        // RevealedBy und FieldOwners getrennt.
        var c = Started(conquest: true);
        var pos = PositionOf(Triple(20));

        c.RecordThrow(Triple(20), pos); // P1 deckt auf und erobert
        var state = c.Current!;
        Assert.Equal(0, state.RevealedBy[pos]);
        Assert.Equal(0, state.FieldOwners[pos]);

        // P1 gilt als fertig -> Feld ist wieder frei, P2 erobert es neu
        var players = state.Players.ToArray();
        players[0] = players[0] with { Score = 0, FinishRank = 1 };
        state = state with { Players = players, CurrentPlayerIndex = 1, CurrentTurn = Array.Empty<FieldValue>() };
        state = ScoringEngine.ApplyThrow(state, Triple(20), pos);

        Assert.Equal(1, state.FieldOwners[pos]);  // Besitz gewechselt
        Assert.Equal(0, state.RevealedBy[pos]);   // Aufdecker unverändert
    }
}
