namespace Dartboard_Randomizer.Core.Models;

/// <summary>
/// Der vollständige, unveränderliche Zustand eines laufenden Spiels.
/// Immutable, damit später ein Undo-Stack (<c>Stack&lt;GameState&gt;</c>) trivial wird:
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

    public PlayerState CurrentPlayer => Players[CurrentPlayerIndex];

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
    };
}
