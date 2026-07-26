namespace Dartboard_Randomizer.Core.Models;

/// <summary>
/// Zustand eines Spielers. Immutable (record) — Änderungen erzeugen per <c>with</c>
/// eine neue Instanz, was den Undo-Stack einfach macht.
/// </summary>
public sealed record PlayerState(string Name, int Score)
{
    /// <summary>Anzahl geworfener Darts (inkl. Misses und Bust-Darts) — Basis für den Average.</summary>
    public int DartsThrown { get; init; }

    /// <summary>Höchste in einer einzelnen Runde (3 Darts) erzielte Punktzahl.</summary>
    public int HighestTurn { get; init; }

    /// <summary>Reihenfolge des Auscheckens (1 = zuerst fertig), oder null falls nicht fertig.</summary>
    public int? FinishRank { get; init; }

    /// <summary>
    /// Reststand nach jeder abgeschlossenen Runde (Index 0 = Startpunktzahl) — Datenbasis
    /// fürs Burndown-Chart.
    /// </summary>
    public IReadOnlyList<int> ScoreProgression { get; init; } = Array.Empty<int>();

    public bool HasFinished => FinishRank is not null;
}
