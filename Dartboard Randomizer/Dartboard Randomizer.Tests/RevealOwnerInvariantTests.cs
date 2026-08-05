using Dartboard_Randomizer.Core.Board;
using Dartboard_Randomizer.Core.Models;
using Dartboard_Randomizer.Core.Scoring;
using Dartboard_Randomizer.Core.ViewModels;

namespace Dartboard_Randomizer.Tests;

/// <summary>
/// Suchtest: Unter welchen Verläufen laufen <c>RevealedBy</c> und <c>FieldOwners</c>
/// auseinander? Beide werden pro Treffer gesetzt, dürfen also (ohne "Reveal all") nie
/// unterschiedlich viele Einträge haben.
/// </summary>
public class RevealOwnerInvariantTests
{
    private static GameController Started(int seed, int score, OutMode outMode, bool revealNoScore)
    {
        var c = new GameController();
        c.StartGame(new GameSettings(
            PlayerNames: new[] { "A", "B", "C" },
            StartingScore: score,
            OutMode: outMode,
            Randomize: true,
            HiddenValues: true,
            Seed: seed)
        {
            Conquest = true,
            RevealDoesNotScore = revealNoScore,
        });
        return c;
    }

    [Fact]
    public void Owners_and_revealers_stay_in_sync_across_many_playthroughs()
    {
        var failures = new List<string>();

        foreach (var seed in new[] { 1, 7, 42, 1234, 99999 })
        foreach (var score in new[] { 30, 101, 501 })
        foreach (var outMode in new[] { OutMode.Straight, OutMode.Double })
        foreach (var revealNoScore in new[] { false, true })
        {
            var c = Started(seed, score, outMode, revealNoScore);
            var layout = BoardLayout.Shuffled(seed);
            var rng = new Mulberry32(seed);

            for (var dart = 0; dart < 200; dart++)
            {
                var state = c.Current!;
                if (!state.AcceptsThrows)
                {
                    if (state.IsOver)
                        break;
                    c.ContinuePlaying(); // Ausspiel-Abfrage: weiterspielen
                    continue;
                }

                var pos = BoardLayout.AllPositions[rng.Next(BoardLayout.AllPositions.Count)];
                c.RecordThrow(layout.ValueAt(pos), pos);

                var s = c.Current!;
                if (s.FieldOwners.Count != s.RevealedBy.Count)
                {
                    var onlyOwned = s.FieldOwners.Keys.Where(k => !s.RevealedBy.ContainsKey(k)).ToList();
                    var onlyRevealed = s.RevealedBy.Keys.Where(k => !s.FieldOwners.ContainsKey(k)).ToList();
                    failures.Add(
                        $"seed={seed} score={score} out={outMode} revealNoScore={revealNoScore} dart={dart}: " +
                        $"owners={s.FieldOwners.Count} revealed={s.RevealedBy.Count} " +
                        $"onlyOwned=[{string.Join(",", onlyOwned)}] onlyRevealed=[{string.Join(",", onlyRevealed)}] " +
                        $"shared=[{string.Join(",", s.SharedPositions)}] " +
                        $"finished={string.Join(",", s.Players.Select(p => p.HasFinished))}");
                    break;
                }
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public void UncreditedReveals_is_zero_in_a_normal_game()
    {
        var c = Started(seed: 7, score: 501, OutMode.Straight, revealNoScore: false);
        var layout = BoardLayout.Shuffled(7);

        foreach (var pos in BoardLayout.AllPositions.Take(6))
            c.RecordThrow(layout.ValueAt(pos), pos);

        var s = c.Current!;

        Assert.Equal(0, RevealStats.UncreditedReveals(s));
        Assert.Equal(RevealStats.Unattributed(s), RevealStats.StillHidden(s));
    }

    [Fact]
    public void UncreditedReveals_counts_the_fields_opened_by_the_button()
    {
        var c = Started(seed: 7, score: 501, OutMode.Straight, revealNoScore: false);
        var layout = BoardLayout.Shuffled(7);
        c.RecordThrow(layout.ValueAt(BoardLayout.AllPositions[0]), BoardLayout.AllPositions[0]);

        c.RevealAll();
        var s = c.Current!;

        // Alles außer dem erspielten Feld und den Sicherheitsfeldern ist unverdient sichtbar.
        var expected = BoardLayout.AllPositions.Count - 1 - s.SharedPositions.Count;
        Assert.Equal(expected, RevealStats.UncreditedReveals(s));

        // Nach "Reveal all" ist nichts mehr verdeckt — das graue "Unrevealed" muss 0 sein.
        Assert.Equal(0, RevealStats.StillHidden(s));

        // Und die Aufteilung deckt den gesamten grauen Rest ab.
        Assert.Equal(RevealStats.Unattributed(s),
            RevealStats.UncreditedReveals(s) + RevealStats.StillHidden(s));
    }

    [Fact]
    public void After_RevealAll_owners_outgrow_revealers()
    {
        // Hypothese für den gemeldeten Unterschied: "Reveal all" deckt alles auf, ohne es
        // jemandem zuzuschreiben. Jeder Treffer danach erobert (FieldOwners wächst), deckt
        // aber nichts mehr auf (RevealedBy bleibt stehen).
        var c = Started(seed: 7, score: 501, OutMode.Straight, revealNoScore: false);
        var layout = BoardLayout.Shuffled(7);

        c.RecordThrow(layout.ValueAt(BoardLayout.AllPositions[0]), BoardLayout.AllPositions[0]);
        c.RevealAll();

        foreach (var pos in BoardLayout.AllPositions.Skip(1).Take(5))
            c.RecordThrow(layout.ValueAt(pos), pos);

        var s = c.Current!;

        Assert.True(s.FieldOwners.Count > s.RevealedBy.Count,
            $"owners={s.FieldOwners.Count} revealed={s.RevealedBy.Count}");
        Assert.Single(s.RevealedBy);           // nur der Treffer VOR "Reveal all"
        Assert.Equal(6, s.FieldOwners.Count);  // alle Treffer erobern
    }
}
