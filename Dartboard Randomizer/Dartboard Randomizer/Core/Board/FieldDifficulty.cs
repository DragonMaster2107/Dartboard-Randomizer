namespace Dartboard_Randomizer.Core.Board;

/// <summary>
/// Wie schwer eine <b>physische</b> Position zu treffen ist — unabhängig davon, welche
/// Wertigkeit dort liegt. Kleiner = leichter.
/// <para>
/// Nur im Randomize-Modus sinnvoll: dort ist die Wertigkeit von der Position entkoppelt,
/// ein „D8" kann also auf einer breiten Single-Fläche liegen und damit leicht zu treffen
/// sein. Auf dem Standardboard würde eine solche Gewichtung die klassischen Wege
/// (T20 T20 D20) zerstören, deshalb wird sie dort nicht angewendet.
/// </para>
/// </summary>
public static class FieldDifficulty
{
    /// <summary>Fallback für unbekannte Ringe — so teuer wie das schwerste Feld.</summary>
    public const int Max = 5;

    /// <summary>Rangfolge vom Nutzer vorgegeben (leicht → schwer).</summary>
    public static int Of(BoardRing ring) => ring switch
    {
        BoardRing.OuterSingle => 0,
        BoardRing.InnerSingle => 1,
        BoardRing.OuterBull => 2,
        BoardRing.Double => 3,
        BoardRing.Triple => 4,
        BoardRing.InnerBull => 5,
        _ => Max,
    };

    /// <summary>
    /// Kosten je Wertigkeit über die übergebenen Positionen. Liegt eine Wertigkeit auf
    /// mehreren Positionen (Singles gibt es doppelt), zählt die <b>leichteste</b> — man
    /// kann ja die bequemere anvisieren.
    /// </summary>
    /// <param name="positions">
    /// Dieselbe Menge, aus der auch die verfügbaren Wertigkeiten stammen — im Hidden-Modus
    /// also nur die aufgedeckten Positionen.
    /// </param>
    public static Dictionary<FieldValue, int> Map(BoardLayout layout, IEnumerable<BoardPosition> positions)
    {
        var map = new Dictionary<FieldValue, int>();

        foreach (var position in positions)
        {
            var value = layout.ValueAt(position);
            var cost = Of(position.Ring);

            if (!map.TryGetValue(value, out var known) || cost < known)
                map[value] = cost;
        }

        return map;
    }
}
