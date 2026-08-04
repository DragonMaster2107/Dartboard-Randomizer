namespace Dartboard_Randomizer.Core.Models;

/// <summary>
/// Das Ergebnis des Setup-Screens — alles, was zum Starten eines Spiels nötig ist.
/// Wird von <c>GameSetup</c> erzeugt und an den <c>GameController</c> übergeben.
/// </summary>
public sealed record GameSettings(
    IReadOnlyList<string> PlayerNames,
    int StartingScore,
    OutMode OutMode,
    bool Randomize,
    bool HiddenValues,
    int? Seed)
{
    /// <summary>
    /// Der gewählte Regelsatz. Aktuell gibt es nur <see cref="GameMode.X01"/>; die
    /// Property trägt die Auswahl aber schon durch State und Persistenz, damit weitere
    /// Modi nur noch ihre Regeln mitbringen müssen.
    /// </summary>
    public GameMode Mode { get; init; } = GameMode.X01;

    /// <summary>
    /// Nur im Hidden-Modus: Der Dart, der ein Feld ERSTMALIG aufdeckt, zählt 0.
    /// Erst Treffer auf bereits aufgedeckte Felder zählen für den Score.
    /// </summary>
    public bool RevealDoesNotScore { get; init; }

    /// <summary>
    /// Nur im Hidden-Modus: Wer ein Feld zuerst trifft, <b>besitzt</b> es — jeder weitere
    /// Treffer darauf zählt für den Besitzer statt für den Werfer.
    /// </summary>
    public bool Conquest { get; init; }
}
