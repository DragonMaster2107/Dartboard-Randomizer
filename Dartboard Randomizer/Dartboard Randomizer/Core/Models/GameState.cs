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

    /// <summary>Der Regelsatz dieses Spiels (siehe <see cref="GameMode"/>).</summary>
    public GameMode Mode { get; init; } = GameMode.X01;

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
    /// Conquest-Modifikator: getroffene Felder gehören dem Spieler, der sie zuerst getroffen
    /// hat — Punkte gehen an den Besitzer, nicht an den Werfer. Setzt Hidden voraus.
    /// </summary>
    public bool Conquest { get; init; }

    /// <summary>
    /// Position → Spielerindex des Besitzers (nur im Conquest-Modus). Liegt im State, damit
    /// Undo den Besitz mit zurücknimmt und ein Reload ihn behält.
    /// </summary>
    public IReadOnlyDictionary<BoardPosition, int> FieldOwners { get; init; } =
        new Dictionary<BoardPosition, int>();

    /// <summary>
    /// Die gemeinsamen Sicherheitsfelder (D1 + eine S1, siehe <see cref="SafeFields"/>) —
    /// von Anfang an aufgedeckt und nicht beanspruchbar. Wird beim Spielstart aus dem
    /// Layout abgeleitet und mitgeführt, damit die Engine kein Layout kennen muss.
    /// </summary>
    public IReadOnlySet<BoardPosition> SharedPositions { get; init; } = new HashSet<BoardPosition>();

    /// <summary>
    /// Wer die anstehende Ausspiel-Abfrage ausgelöst hat. Im Conquest-Modus kann das ein
    /// <b>anderer</b> Spieler als der Werfer sein, deshalb reicht der CurrentPlayerIndex
    /// für den Dialogtext nicht mehr.
    /// </summary>
    public int? PendingFinisherIndex { get; init; }

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

    /// <summary>
    /// Die gemeinsamen Sicherheitsfelder für dieses Spiel. Nur im Conquest-Modus relevant;
    /// dort ist Randomize (und damit ein Seed) Voraussetzung, das Layout ist also
    /// reproduzierbar ableitbar.
    /// </summary>
    private static IReadOnlySet<BoardPosition> SharedFor(GameSettings settings)
        => settings.Conquest && settings.Seed is int seed
            ? SafeFields.For(BoardLayout.Shuffled(seed))
            : new HashSet<BoardPosition>();

    /// <summary>Erzeugt den Anfangszustand aus den Setup-Einstellungen.</summary>
    public static GameState CreateNew(GameSettings settings)
    {
        var shared = SharedFor(settings);
        return new GameState
        {
            Players = settings.PlayerNames
                .Select(name => new PlayerState(name, settings.StartingScore)
                {
                    ScoreProgression = new List<int> { settings.StartingScore },
                })
                .ToList(),
            CurrentPlayerIndex = 0,
            Mode = settings.Mode,
            OutMode = settings.OutMode,
            StartingScore = settings.StartingScore,
            Randomize = settings.Randomize,
            HiddenValues = settings.HiddenValues,
            RevealDoesNotScore = settings.RevealDoesNotScore,
            Seed = settings.Seed,
            CurrentTurn = Array.Empty<FieldValue>(),
            TurnStartScore = settings.StartingScore,
            // Die Sicherheitsfelder sind von Anfang an sichtbar — das ist ihr Zweck.
            RevealedPositions = new HashSet<BoardPosition>(shared),
            Conquest = settings.Conquest,
            FieldOwners = new Dictionary<BoardPosition, int>(),
            SharedPositions = shared,
            AwaitingContinueDecision = false,
            IsOver = false,
        };
    }
}
