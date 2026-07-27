using Dartboard_Randomizer.Core.Board;

namespace Dartboard_Randomizer.Core.Models;

/// <summary>
/// Der vollständige, unveränderliche Zustand eines laufenden Spiels.
/// Immutable, damit der Undo-Stack (<c>Stack&lt;GameState&gt;</c>) trivial wird.
/// </summary>
public sealed record GameState
{
    public required IReadOnlyList<PlayerState> Players { get; init; }
    public int CurrentPlayerIndex { get; init; }
    public OutMode OutMode { get; init; }
    public int StartingScore { get; init; }
    public bool Randomize { get; init; }
    public bool HiddenValues { get; init; }
    public bool RevealDoesNotScore { get; init; }
    public int? Seed { get; init; }

    /// <summary>Die bereits geworfenen Darts der aktuellen Runde (0..3).</summary>
    public IReadOnlyList<FieldValue> CurrentTurn { get; init; } = Array.Empty<FieldValue>();

    /// <summary>Punktestand des aktuellen Spielers zu Rundenbeginn — für Bust-Revert.</summary>
    public int TurnStartScore { get; init; }

    /// <summary>Bereits aufgedeckte Positionen im Hidden-Modus (gilt fürs ganze Spiel).</summary>
    public IReadOnlySet<BoardPosition> RevealedPositions { get; init; } = new HashSet<BoardPosition>();

    /// <summary>
    /// Ein Spieler hat gerade ausgecheckt, aber es sind noch Spieler übrig — das Spiel
    /// pausiert und wartet auf die Entscheidung "ausspielen oder beenden".
    /// </summary>
    public bool AwaitingContinueDecision { get; init; }

    /// <summary>Das Spiel ist vorbei (alle fertig oder vorzeitig beendet).</summary>
    public bool IsOver { get; init; }

    public PlayerState CurrentPlayer => Players[CurrentPlayerIndex];

    /// <summary>Nimmt gerade Würfe entgegen? (Nicht während der Ausspiel-Abfrage oder nach Spielende.)</summary>
    public bool AcceptsThrows => !IsOver && !AwaitingContinueDecision;

    /// <summary>Spieler nach Platzierung: Fertige zuerst (nach Rang), dann Rest nach Reststand.</summary>
    public IReadOnlyList<PlayerState> Ranking =>
        Players.OrderBy(p => p.FinishRank ?? int.MaxValue)
               .ThenBy(p => p.Score)
               .ToList();

    /// <summary>Erzeugt den Anfangszustand aus den Setup-Einstellungen.</summary>
    public static GameState CreateNew(GameSettings settings) => new()
    {
        Players = settings.PlayerNames
            .Select(name => new PlayerState(name, settings.StartingScore)
            {
                ScoreProgression = new List<int> { settings.StartingScore },
            })
            .ToList(),
        CurrentPlayerIndex = 0,
        OutMode = settings.OutMode,
        StartingScore = settings.StartingScore,
        Randomize = settings.Randomize,
        HiddenValues = settings.HiddenValues,
        RevealDoesNotScore = settings.RevealDoesNotScore,
        Seed = settings.Seed,
        CurrentTurn = Array.Empty<FieldValue>(),
        TurnStartScore = settings.StartingScore,
        RevealedPositions = new HashSet<BoardPosition>(),
        AwaitingContinueDecision = false,
        IsOver = false,
    };
}
