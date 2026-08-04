using MudBlazor; // nur für die Icon-Konstanten der Registry (reine Strings)

namespace Dartboard_Randomizer.Core.Models;

/// <summary>
/// Der Regelsatz eines Spiels — die erste der zwei Achsen (die zweite sind die
/// Board-Modifikatoren Randomize/Hidden/…, die auf JEDEN Modus draufpassen).
/// <para>
/// ⚠ <see cref="X01"/> muss 0 bleiben: gespeicherte Spielstände/Setups aus der Zeit vor
/// den Modi enthalten das Feld nicht, und System.Text.Json belegt fehlende Werte mit
/// <c>default</c> — so landen alte Daten automatisch auf X01.
/// </para>
/// </summary>
public enum GameMode
{
    X01 = 0,
}

/// <summary>
/// Die Kategorie im Modus-Picker. Rein zur Gruppierung der Karten — sobald es viele
/// Modi gibt, ist das der Unterschied zwischen einer Liste und einer Übersicht.
/// </summary>
public enum GameModeCategory
{
    Countdown,
}

/// <summary>
/// Beschreibt einen Spielmodus für die UI. Ein neuer Modus = ein Eintrag in
/// <see cref="GameModes.All"/>; der Setup-Screen selbst muss dafür nicht angefasst werden.
/// </summary>
/// <param name="Id">Persistierter Schlüssel des Modus.</param>
/// <param name="Name">Anzeigename (Karte + Panel-Titel).</param>
/// <param name="Tagline">Ein-Satz-Regel für die Karte im Picker.</param>
/// <param name="Icon">Material-Icon für Karte und Modus-Zeile.</param>
/// <param name="Category">Gruppe im Picker.</param>
/// <param name="MinPlayers">Kleinste sinnvolle Spielerzahl — sperrt den Start-Button.</param>
/// <param name="SupportsBoardModifiers">
/// Ob Randomize/Hidden für diesen Modus überhaupt greifen. Steuert, ob das
/// Modifiers-Panel angezeigt wird.
/// </param>
public sealed record GameModeDefinition(
    GameMode Id,
    string Name,
    string Tagline,
    string Icon,
    GameModeCategory Category,
    int MinPlayers,
    bool SupportsBoardModifiers);

/// <summary>
/// Registry aller verfügbaren Spielmodi. Einzige Quelle für den Modus-Picker.
/// </summary>
public static class GameModes
{
    public static GameModeDefinition X01 { get; } = new(
        Id: GameMode.X01,
        Name: "X01",
        Tagline: "Count down from a starting score to exactly zero.",
        Icon: Icons.Material.Filled.TrendingDown,
        Category: GameModeCategory.Countdown,
        MinPlayers: 1,
        SupportsBoardModifiers: true);

    public static IReadOnlyList<GameModeDefinition> All { get; } = new[] { X01 };

    /// <summary>
    /// Definition zu einem Modus. Fällt auf X01 zurück, damit ein unbekannter Wert aus
    /// altem/manipuliertem Storage die App nicht sprengt.
    /// </summary>
    public static GameModeDefinition Get(GameMode mode)
        => All.FirstOrDefault(m => m.Id == mode) ?? X01;
}
