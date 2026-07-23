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
    int? Seed);
