using Dartboard_Randomizer.Core.Board;
using Dartboard_Randomizer.Core.Models;

namespace Dartboard_Randomizer.Core.Scoring;

/// <summary>
/// Reine Spiellogik: wendet einen einzelnen Dart auf den Spielzustand an und liefert
/// den neuen Zustand. Keine UI, kein State — dadurch voll unit-testbar.
/// </summary>
public static class ScoringEngine
{
    public static GameState ApplyThrow(GameState state, FieldValue dart)
    {
        if (state.IsFinished)
            return state;

        var index = state.CurrentPlayerIndex;
        var newScore = state.CurrentPlayer.Score - dart.Points;
        var turn = state.CurrentTurn.Append(dart).ToArray();

        // Gewonnen: genau 0 UND gültiger Checkout gemäß Out-Modus (über die Wertigkeit).
        if (newScore == 0 && IsValidCheckout(dart, state.OutMode))
        {
            return state with
            {
                Players = WithScore(state.Players, index, 0),
                CurrentTurn = turn,
                WinnerIndex = index,
            };
        }

        // Bust: Runde verfällt, zurück auf Rundenstart-Score, nächster Spieler.
        if (IsBust(newScore, dart, state.OutMode))
            return EndTurn(RevertTurn(state));

        // Regulär: Punkte abziehen, Dart der Runde hinzufügen.
        var advanced = state with
        {
            Players = WithScore(state.Players, index, newScore),
            CurrentTurn = turn,
        };

        // Nach dem 3. Dart automatisch zum nächsten Spieler.
        return turn.Length >= 3 ? EndTurn(advanced) : advanced;
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
            return true;                                // 0 auf die falsche Art erreicht
        if (newScore == 1 && mode != OutMode.Straight)
            return true;                                // aus 1 ist kein Double/Master-Finish möglich
        return false;
    }

    // Score des aktuellen Spielers auf den Rundenstart zurücksetzen (Runden-Darts verwerfen).
    private static GameState RevertTurn(GameState state) => state with
    {
        Players = WithScore(state.Players, state.CurrentPlayerIndex, state.TurnStartScore),
    };

    private static GameState EndTurn(GameState state)
    {
        var next = (state.CurrentPlayerIndex + 1) % state.Players.Count;
        return state with
        {
            CurrentPlayerIndex = next,
            CurrentTurn = Array.Empty<FieldValue>(),
            TurnStartScore = state.Players[next].Score,
        };
    }

    private static IReadOnlyList<PlayerState> WithScore(IReadOnlyList<PlayerState> players, int index, int score)
    {
        var copy = players.ToArray();
        copy[index] = copy[index] with { Score = score };
        return copy;
    }
}
