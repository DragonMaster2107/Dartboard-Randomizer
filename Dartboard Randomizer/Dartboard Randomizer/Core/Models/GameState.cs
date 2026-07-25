using Dartboard_Randomizer.Core.Board;

namespace Dartboard_Randomizer.Core.Models;

/// <summary>
/// Der vollständige, unveränderliche Zustand eines laufenden Spiels.
/// Immutable, damit der Undo-Stack (<c>Stack&lt;GameState&gt;</c>) trivial wird:
/// jeder Zug erzeugt per <c>with</c> einen neuen Zustand.
/// </summary>
public sealed record GameState
{
    public required IReadOnlyList<PlayerState> Players { get; init; }
    public int CurrentPlayerIndex { get; init; }
    public OutMode OutMode { get; init; }
    public int StartingScore { get; init; }
    public bool Randomize { get; init; }
    public bool HiddenValues { get; init; }
    public int? Seed { get; init; }

    /// <summary>Die bereits geworfenen Darts der aktuellen Runde (0..3).</summary>
    public IReadOnlyList<FieldValue> CurrentTurn { get; init; } = Array.Empty<FieldValue>();

    /// <summary>Punktestand des aktuellen Spielers zu Rundenbeginn — für Bust-Revert.</summary>
    public int TurnStartScore { get; init; }

    /// <summary>Index des Gewinners, oder null solange niemand gecheckt hat.</summary>
    public int? WinnerIndex { get; init; }

    /// <summary>
    /// Bereits aufgedeckte Positionen im Hidden-Modus — gilt fürs ganze Spiel und lebt
    /// deshalb hier im State (nicht in der Board-Komponente), übersteht Navigation & Reload.
    /// </summary>
    public IReadOnlySet<BoardPosition> RevealedPositions { get; init; } = new HashSet<BoardPosition>();

    public PlayerState CurrentPlayer => Players[CurrentPlayerIndex];
    public bool IsFinished => WinnerIndex is not null;
    public PlayerState? Winner => WinnerIndex is int i ? Players[i] : null;
    public int DartsThrownThisTurn => CurrentTurn.Count;

    /// <summary>Erzeugt den Anfangszustand aus den Setup-Einstellungen.</summary>
    public static GameState CreateNew(GameSettings settings) => new()
    {
        Players = settings.PlayerNames
            .Select(name => new PlayerState(name, settings.StartingScore))
            .ToList(),
        CurrentPlayerIndex = 0,
        OutMode = settings.OutMode,
        StartingScore = settings.StartingScore,
        Randomize = settings.Randomize,
        HiddenValues = settings.HiddenValues,
        Seed = settings.Seed,
        CurrentTurn = Array.Empty<FieldValue>(),
        TurnStartScore = settings.StartingScore,
        WinnerIndex = null,
        RevealedPositions = new HashSet<BoardPosition>(),
    };
}
