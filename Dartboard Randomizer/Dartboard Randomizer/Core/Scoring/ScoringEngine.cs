using Dartboard_Randomizer.Core.Board;
using Dartboard_Randomizer.Core.Models;

namespace Dartboard_Randomizer.Core.Scoring;

/// <summary>
/// Reine Spiellogik: wendet einen einzelnen Dart auf den Spielzustand an und liefert
/// den neuen Zustand. Keine UI, kein State — voll unit-testbar.
/// </summary>
public static class ScoringEngine
{
    /// <summary>
    /// Wendet einen Dart an. <paramref name="position"/> ist die getroffene physische
    /// Position — nur im Conquest-Modus relevant (dort entscheidet sie, <b>wem</b> die Punkte
    /// angerechnet werden). Ohne Position bzw. ohne Conquest-Modus gilt wie immer der Werfer.
    /// </summary>
    public static GameState ApplyThrow(GameState state, FieldValue dart, BoardPosition? position = null)
    {
        if (!state.AcceptsThrows)
            return state;

        var thrower = state.CurrentPlayerIndex;

        // Der Dart zählt für die Statistik des WERFERS, egal wie der Zug ausgeht und egal,
        // wem die Punkte am Ende zugutekommen.
        var working = state with
        {
            Players = UpdatePlayer(state.Players, thrower, p => p with { DartsThrown = p.DartsThrown + 1 }),
            CurrentTurn = state.CurrentTurn.Append(dart).ToArray(),
        };

        // Besitz auflösen: freie (und wieder freigewordene) Felder beansprucht der Werfer,
        // besetzte rechnen dem Besitzer an.
        var scoredBy = thrower;
        if (state.Conquest && position is BoardPosition pos)
        {
            scoredBy = FieldOwnership.ScoringPlayer(state, pos, thrower);
            if (FieldOwnership.ConquersOnHit(state, pos))
                working = working with { FieldOwners = WithOwner(state.FieldOwners, pos, thrower) };
        }

        // Sabotage (Conquest-Untermodus): ein Treffer auf ein FREMDES Feld wird dem Besitzer
        // aufgeschlagen statt abgezogen. Checkout und Bust sind dabei ausgeschlossen — der
        // Score kann nur steigen, die 0 also nie erreichen.
        if (state.Sabotage && scoredBy != thrower)
        {
            working = working with
            {
                Players = UpdatePlayer(working.Players, scoredBy, p => p with { Score = p.Score + dart.Points }),
            };
            return AfterDart(working, state);
        }

        var newScore = working.Players[scoredBy].Score - dart.Points;

        // Ausgecheckt: 0 UND gültiger Checkout gemäß Out-Modus. Im Conquest-Modus kann das
        // auch einen ANDEREN Spieler treffen — man kann also für jemanden beenden.
        if (newScore == 0 && IsValidCheckout(dart, state.OutMode))
            return ApplyFinish(working, state, scoredBy, thrower);

        if (IsBust(newScore, dart, state.OutMode))
        {
            // ⚠ Nur der EIGENE Bust lässt den Zug verfallen. Würde der Treffer ein fremdes
            // Feld überwerfen, verpufft er nur (der Fremde hat keinen laufenden Zug, der
            // verfallen könnte) und der Werfer wirft seine restlichen Darts.
            if (scoredBy != thrower)
                return AfterDart(working, state);

            var reverted = UpdatePlayer(working.Players, thrower, p => p with
            {
                Score = state.TurnStartScore,
                ScoreProgression = Append(p.ScoreProgression, state.TurnStartScore),
            });
            return EndTurn(working with { Players = reverted });
        }

        // Regulär: Punkte beim Zielspieler abziehen.
        working = working with
        {
            Players = UpdatePlayer(working.Players, scoredBy, p => p with { Score = newScore }),
        };
        return AfterDart(working, state);
    }

    /// <summary>
    /// Setzt nach einem Auscheck-Stopp fort.
    /// <para>
    /// ⚠ Hat der Werfer einen <b>anderen</b> Spieler fertig gemacht (Conquest-Modus), läuft
    /// seine Runde normal weiter — es wird nur gewechselt, wenn er selbst ausgecheckt hat
    /// oder seine drei Darts durch sind.
    /// </para>
    /// </summary>
    public static GameState ResumeAfterFinish(GameState state)
    {
        if (!state.AwaitingContinueDecision)
            return state;

        var resumed = state with { AwaitingContinueDecision = false };

        if (resumed.CurrentPlayer.HasFinished || resumed.CurrentTurn.Count >= 3)
            return EndTurn(resumed);

        return resumed;
    }

    /// <summary>Ein Spieler ist auf 0 — Platzierung vergeben und ggf. die Ausspiel-Abfrage stellen.</summary>
    private static GameState ApplyFinish(GameState working, GameState before, int finisher, int thrower)
    {
        var rank = working.Players.Count(p => p.HasFinished) + 1;
        var players = UpdatePlayer(working.Players, finisher, p => p with
        {
            Score = 0,
            FinishRank = rank,
            // Rundenpunkte nur, wenn er selbst geworfen hat (von TurnStartScore auf 0).
            HighestTurn = finisher == thrower ? Math.Max(p.HighestTurn, before.TurnStartScore) : p.HighestTurn,
            ScoreProgression = Append(p.ScoreProgression, 0),
        });

        var stillPlaying = players.Any(p => !p.HasFinished);
        var next = working with
        {
            Players = players,
            PendingFinisherIndex = finisher,
            IsOver = !stillPlaying,
            AwaitingContinueDecision = stillPlaying,
        };

        // Hat der Werfer einen Fremden mit seinem LETZTEN Dart fertig gemacht, ist seine
        // Runde vorbei — die Rundenstatistik gehört jetzt geschrieben, der Spielerwechsel
        // folgt erst nach der Ausspiel-Abfrage (ResumeAfterFinish).
        if (finisher != thrower && next.CurrentTurn.Count >= 3)
            next = WriteTurnStats(next, before);

        return next;
    }

    /// <summary>Nach dem 3. Dart: Rundenstatistik festhalten, dann Spielerwechsel.</summary>
    private static GameState AfterDart(GameState working, GameState before)
        => working.CurrentTurn.Count < 3
            ? working
            : EndTurn(WriteTurnStats(working, before));

    /// <summary>
    /// Rundenpunkte des Werfers für die Statistik. Basis ist immer sein EIGENER Score —
    /// Punkte, die im Conquest-Modus an andere gegangen sind, zählen hier nicht mit.
    /// </summary>
    private static GameState WriteTurnStats(GameState working, GameState before)
    {
        var index = working.CurrentPlayerIndex;
        var ownScore = working.Players[index].Score;
        var turnScored = before.TurnStartScore - ownScore;

        return working with
        {
            Players = UpdatePlayer(working.Players, index, p => p with
            {
                HighestTurn = Math.Max(p.HighestTurn, turnScored),
                ScoreProgression = Append(p.ScoreProgression, ownScore),
            }),
        };
    }

    private static IReadOnlyDictionary<BoardPosition, int> WithOwner(
        IReadOnlyDictionary<BoardPosition, int> owners, BoardPosition position, int playerIndex)
        => new Dictionary<BoardPosition, int>(owners) { [position] = playerIndex };

    /// <summary>Darf mit diesem Dart auf 0 beendet werden?</summary>
    public static bool IsValidCheckout(FieldValue dart, OutMode mode) => mode switch
    {
        OutMode.Straight => true,
        OutMode.Double => dart.IsDouble,               // inkl. Inner Bull (Double 25)
        OutMode.Master => dart.IsDouble || dart.IsTriple,
        _ => false,
    };

    private static bool IsBust(int newScore, FieldValue dart, OutMode mode)
    {
        if (newScore < 0)
            return true;
        if (newScore == 0 && !IsValidCheckout(dart, mode))
            return true;
        if (newScore == 1 && mode != OutMode.Straight)
            return true;
        return false;
    }

    private static GameState EndTurn(GameState state)
    {
        var next = NextUnfinished(state.Players, state.CurrentPlayerIndex);
        return state with
        {
            CurrentPlayerIndex = next,
            CurrentTurn = Array.Empty<FieldValue>(),
            TurnStartScore = state.Players[next].Score,
        };
    }

    private static int NextUnfinished(IReadOnlyList<PlayerState> players, int from)
    {
        var n = players.Count;
        for (var step = 1; step <= n; step++)
        {
            var idx = (from + step) % n;
            if (!players[idx].HasFinished)
                return idx;
        }
        return from; // alle fertig (wird in dem Fall nicht mehr aufgerufen)
    }

    private static IReadOnlyList<int> Append(IReadOnlyList<int> list, int value) =>
        new List<int>(list) { value };

    private static IReadOnlyList<PlayerState> UpdatePlayer(
        IReadOnlyList<PlayerState> players, int index, Func<PlayerState, PlayerState> update)
    {
        var copy = players.ToArray();
        copy[index] = update(copy[index]);
        return copy;
    }
}
