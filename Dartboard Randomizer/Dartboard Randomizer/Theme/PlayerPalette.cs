namespace Dartboard_Randomizer.Theme;

/// <summary>
/// Die Spielerfarben für alle Auswertungs-Ansichten (Burndown, Aufdeck-Anteile,
/// Board-Highlight). Zentral, damit ein Spieler überall dieselbe Farbe hat — sonst
/// wären die drei Ansichten nicht mehr vergleichbar.
/// <para>
/// ⚠ Der Index ist immer die Position in <c>GameState.Players</c> (die Startreihenfolge),
/// <b>nicht</b> im <c>Ranking</c> — sonst wechseln die Farben mit der Platzierung.
/// </para>
/// </summary>
public static class PlayerPalette
{
    private static readonly string[] Colors =
        { "#d32f2f", "#1e88e5", "#43a047", "#fbc02d", "#8e24aa", "#00acc1", "#f4511e", "#3949ab" };

    /// <summary>Grau für Felder, die noch verdeckt sind.</summary>
    public const string Neutral = "#5f6368";

    /// <summary>
    /// Helleres Grau für Felder, die per „Reveal all" aufgedeckt wurden und deshalb keinem
    /// Spieler zugerechnet werden — sichtbar, aber unverdient.
    /// </summary>
    public const string NeutralUncredited = "#9aa0a6";

    public static string For(int playerIndex)
        => Colors[Math.Abs(playerIndex) % Colors.Length];
}
