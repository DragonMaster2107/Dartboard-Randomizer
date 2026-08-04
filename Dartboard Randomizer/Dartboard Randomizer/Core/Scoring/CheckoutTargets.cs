using Dartboard_Randomizer.Core.Board;

namespace Dartboard_Randomizer.Core.Scoring;

/// <summary>
/// Übersetzt einen Checkout-Vorschlag (Wertigkeiten) in die physischen Positionen, die auf
/// der Scheibe markiert werden. Reine Funktion — deshalb ohne UI testbar.
/// </summary>
public static class CheckoutTargets
{
    /// <summary>Eine zu markierende Position samt Platz in der Wurffolge (0 = nächster Dart).</summary>
    public readonly record struct Target(BoardPosition Position, int Step);

    private static readonly IReadOnlyList<Target> None = Array.Empty<Target>();

    /// <param name="route">Der Checkout-Vorschlag, oder null wenn keiner existiert.</param>
    /// <param name="layout">Das Board dieses Spiels (gemischt oder Standard).</param>
    /// <param name="hiddenValues">Ob das Spiel im Hidden-Modus läuft.</param>
    /// <param name="revealed">Die bereits aufgedeckten Positionen.</param>
    /// <param name="usable">
    /// Optional: nur diese Positionen dürfen markiert werden. Im Conquest-Modus sind das die
    /// Felder, aus denen der Spieler selbst Punkte zieht — sonst würde die Markierung auf
    /// ein gleichwertiges Feld des Gegners zeigen und die Punkte dorthin schenken.
    /// </param>
    public static IReadOnlyList<Target> For(
        IReadOnlyList<FieldValue>? route,
        BoardLayout? layout,
        bool hiddenValues,
        IReadOnlySet<BoardPosition> revealed,
        IReadOnlySet<BoardPosition>? usable = null)
    {
        if (route is null || route.Count == 0 || layout is null)
            return None;

        var steps = new Dictionary<BoardPosition, int>();

        for (var step = 0; step < route.Count; step++)
        {
            // Eine Wertigkeit kann auf zwei Positionen liegen (Singles gibt es doppelt).
            foreach (var pos in layout.PositionsOf(route[step]))
            {
                // ⚠ Im Hidden-Modus NUR aufgedeckte Positionen markieren. Sonst verrät das
                // Highlight den Wert eines noch verdeckten Feldes: Ist eine Single 7
                // aufgedeckt und die zweite nicht, dürfte nur die aufgedeckte leuchten.
                if (hiddenValues && !revealed.Contains(pos))
                    continue;

                // Conquest-Modus: fremde Felder bringen dem Spieler nichts.
                if (usable is not null && !usable.Contains(pos))
                    continue;

                // Kommt ein Feld mehrfach in der Route vor (T20, T20, D20), gewinnt der
                // früheste Schritt — das ist der Wurf, der als Nächstes dorthin geht.
                if (!steps.ContainsKey(pos))
                    steps[pos] = step;
            }
        }

        return steps.Count == 0
            ? None
            : steps.Select(kv => new Target(kv.Key, kv.Value)).ToList();
    }
}
