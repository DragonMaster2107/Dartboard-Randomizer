using Dartboard_Randomizer.Core.Board;
using Dartboard_Randomizer.Core.Models;

namespace Dartboard_Randomizer.Core.Scoring;

/// <summary>
/// Reine Spiellogik: wendet einen einzelnen Dart auf den Spielzustand an und liefert
/// den neuen Zustand. Keine UI, kein State — voll unit-testbar.
/// </summary>
public static class ScoringEngine
{
    public static GameState ApplyThrow(GameState state, FieldValue dart)
    {
        if (!state.AcceptsThrows)
            return state;

        var index = state.CurrentPlayerIndex;
        var player = state.Players[index];
        var newScore = player.Score - dart.Points;
        var turn = state.CurrentTurn.Append(dart).ToArray();

        // Der Dart zählt für die Statistik, egal wie der Zug ausgeht.
        var players = UpdatePlayer(state.Players, index, p => p with { DartsThrown = p.DartsThrown + 1 });

        // Gewonnen / ausgecheckt: 0 UND gültiger Checkout gemäß Out-Modus.
        if (newScore == 0 && IsValidCheckout(dart, state.OutMode))
        {
            var turnScored = state.TurnStartScore; // von TurnStartScore auf 0
            var rank = state.Players.Count(p => p.HasFinished) + 1;
            players = UpdatePlayer(players, index, p => p with
            {
                Score = 0,
                FinishRank = rank,
                HighestTurn = Math.Max(p.HighestTurn, turnScored),
                ScoreProgression = Append(p.ScoreProgression, 0),
            });

            var stillPlaying = players.Any(p => !p.HasFinished);
            return state with
            {
                Players = players,
                CurrentTurn = turn,
                IsOver = !stillPlaying,
                AwaitingContinueDecision = stillPlaying,
            };
        }

        // Bust: Runde verfällt, zurück auf Rundenstart-Score, nächster Spieler.
        if (IsBust(newScore, dart, state.OutMode))
        {
            players = UpdatePlayer(players, index, p => p with
            {
                Score = state.TurnStartScore,
                ScoreProgression = Append(p.ScoreProgression, state.TurnStartScore),
            });
            return EndTurn(state with { Players = players });
        }

        // Regulär: Punkte abziehen.
        players = UpdatePlayer(players, index, p => p with { Score = newScore });
        var advanced = state with { Players = players, CurrentTurn = turn };

        // Nach dem 3. Dart: Rundenpunkte für die Statistik festhalten, dann Spielerwechsel.
        if (turn.Length >= 3)
        {
            var turnScored = state.TurnStartScore - newScore;
            players = UpdatePlayer(advanced.Players, index, p => p with
            {
                HighestTurn = Math.Max(p.HighestTurn, turnScored),
                ScoreProgression = Append(p.ScoreProgression, newScore),
            });
            return EndTurn(advanced with { Players = players });
        }

        return advanced;
    }

    /// <summary>Setzt nach einem Auscheck-Stopp fort: nächster noch nicht fertiger Spieler.</summary>
    public static GameState ResumeAfterFinish(GameState state)
    {
        if (!state.AwaitingContinueDecision)
            return state;

        return EndTurn(state with { AwaitingContinueDecision = false });
    }

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
