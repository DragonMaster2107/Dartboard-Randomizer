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
    bool RandomOrder)
{
    public static SetupDefaults Initial { get; } = new(
        StartingScore: 501,
        OutMode: OutMode.Double,
        Randomize: false,
        HiddenValues: false,
        RevealDoesNotScore: false,
        RandomOrder: true);

    /// <summary>
    /// Bereinigt gespeicherte Kombinationen, die die UI gar nicht erlauben würde
    /// (Hidden ohne Randomize, First-hit ohne Hidden) — schützt vor altem/manipuliertem Storage.
    /// </summary>
    public SetupDefaults Sanitized()
    {
        var hidden = Randomize && HiddenValues;
        return this with
        {
            StartingScore = StartingScore < 2 ? 501 : StartingScore,
            HiddenValues = hidden,
            RevealDoesNotScore = hidden && RevealDoesNotScore,
        };
    }
}
