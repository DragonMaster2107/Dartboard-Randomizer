namespace Dartboard_Randomizer.Core.Models;

/// <summary>
/// Die zuletzt im Setup gewählten Optionen — werden beim nächsten Öffnen von
/// <c>GameSetup</c> wieder vorbelegt (eigener localStorage-Key, unabhängig vom Spielstand).
/// <para>
/// Der <c>Seed</c> ist bewusst NICHT enthalten: sonst würde jedes Folgespiel dasselbe
/// gemischte Board bekommen. Er wird pro Spiel neu erzeugt.
/// </para>
/// </summary>
public sealed record SetupDefaults(
    int StartingScore,
    OutMode OutMode,
    bool Randomize,
    bool HiddenValues,
    bool RevealDoesNotScore,
    bool RandomOrder,
    GameMode Mode = GameMode.X01,
    bool Conquest = false,
    bool Sabotage = false)
{
    public static SetupDefaults Initial { get; } = new(
        StartingScore: 501,
        OutMode: OutMode.Double,
        Randomize: false,
        HiddenValues: false,
        RevealDoesNotScore: false,
        RandomOrder: true,
        Mode: GameMode.X01,
        Conquest: false,
        Sabotage: false);

    /// <summary>
    /// Bereinigt gespeicherte Kombinationen, die die UI gar nicht erlauben würde
    /// (Hidden ohne Randomize, First-hit/Conquest ohne Hidden, Sabotage ohne Conquest,
    /// Modifikatoren in einem Modus, der sie nicht unterstützt) — schützt vor
    /// altem/manipuliertem Storage.
    /// </summary>
    public SetupDefaults Sanitized()
    {
        var mode = GameModes.Get(Mode);              // unbekannter Modus -> X01
        var randomize = mode.SupportsBoardModifiers && Randomize;
        var hidden = randomize && HiddenValues;
        var conquest = hidden && Conquest;
        return this with
        {
            StartingScore = StartingScore < 2 ? 501 : StartingScore,
            Mode = mode.Id,
            Randomize = randomize,
            HiddenValues = hidden,
            RevealDoesNotScore = hidden && RevealDoesNotScore,
            Conquest = conquest,
            Sabotage = conquest && Sabotage,
        };
    }
}
