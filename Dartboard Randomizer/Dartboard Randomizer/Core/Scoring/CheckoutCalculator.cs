using Dartboard_Randomizer.Core.Board;
using Dartboard_Randomizer.Core.Models;

namespace Dartboard_Randomizer.Core.Scoring;

/// <summary>
/// Findet den besten Weg, das Spiel mit den restlichen Darts zu beenden — nur aus den
/// tatsächlich verfügbaren Wertigkeiten (im Hidden-Modus also nur aufgedeckte Felder).
/// Liefert die kürzeste Dart-Folge (fewest darts) oder null, wenn kein Checkout möglich ist.
/// </summary>
public static class CheckoutCalculator
{
    /// <param name="difficulty">
    /// Optionale Trefferkosten je Wertigkeit (siehe <see cref="FieldDifficulty"/>). Ist sie
    /// gesetzt (Randomize-Modus), wird unter allen Wegen gleicher Länge der <b>physisch am
    /// leichtesten zu treffende</b> gewählt. Ohne sie gilt das alte Verhalten: erster
    /// gefundener Weg bei absteigend sortierten Werten.
    /// </param>
    public static IReadOnlyList<FieldValue>? Suggest(
        int remaining, int dartsLeft, OutMode outMode, IReadOnlyCollection<FieldValue> available,
        IReadOnlyDictionary<FieldValue, int>? difficulty = null)
    {
        if (remaining <= 0 || dartsLeft <= 0)
            return null;

        var candidates = available.Where(v => v.Points > 0).Distinct();

        // Ohne Gewichtung: höhere Werte zuerst -> "schönere" Vorschläge (z.B. T20 vor kleinen
        // Feldern). Mit Gewichtung: leichteste Felder zuerst, bei Gleichstand der höhere Wert.
        // Diese Sortierung trägt die Suche: der erste passende Finisher ist dann der billigste.
        var values = (difficulty is null
                ? candidates.OrderByDescending(v => v.Points)
                : candidates.OrderBy(v => Cost(v, difficulty)).ThenByDescending(v => v.Points))
            .ToList();

        // Kürzeste Lösung bevorzugen: erst 1 Dart, dann 2, dann 3 ...
        // Weniger Darts schlägt bewusst auch einen leichter treffbaren längeren Weg.
        for (var depth = 1; depth <= dartsLeft; depth++)
        {
            var path = difficulty is null
                ? Find(remaining, depth, outMode, values)
                : FindCheapest(remaining, depth, outMode, values, difficulty);

            if (path != null)
                return path;
        }

        return null;
    }

    private static int Cost(FieldValue value, IReadOnlyDictionary<FieldValue, int> difficulty)
        => difficulty.TryGetValue(value, out var c) ? c : FieldDifficulty.Max;

    // ---------- ohne Gewichtung: erster Treffer gewinnt (unverändertes Altverhalten) ----------

    private static List<FieldValue>? Find(int remaining, int depth, OutMode outMode, List<FieldValue> values)
    {
        if (depth == 1)
        {
            foreach (var v in values)
                if (v.Points == remaining && ScoringEngine.IsValidCheckout(v, outMode))
                    return new List<FieldValue> { v };
            return null;
        }

        foreach (var v in values)
        {
            if (v.Points >= remaining)
                continue; // Zwischen-Dart muss etwas übrig lassen

            var rest = Find(remaining - v.Points, depth - 1, outMode, values);
            if (rest != null)
            {
                rest.Insert(0, v);
                return rest;
            }
        }

        return null;
    }

    // ---------- mit Gewichtung: günstigste Route dieser Tiefe (Branch and Bound) ----------

    private static List<FieldValue>? FindCheapest(
        int remaining, int depth, OutMode outMode, List<FieldValue> values,
        IReadOnlyDictionary<FieldValue, int> difficulty)
    {
        List<FieldValue>? best = null;
        var bestCost = int.MaxValue;
        var path = new FieldValue[depth];

        void Step(int rest, int index, int costSoFar)
        {
            // Teurer als die beste bekannte Route -> dieser Ast kann nichts mehr gewinnen.
            if (costSoFar >= bestCost)
                return;

            if (index == depth - 1)
            {
                foreach (var v in values)
                {
                    if (v.Points != rest || !ScoringEngine.IsValidCheckout(v, outMode))
                        continue;

                    var total = costSoFar + Cost(v, difficulty);
                    if (total >= bestCost)
                        continue;

                    path[index] = v;
                    bestCost = total;
                    best = path.ToList();

                    // values ist nach Kosten sortiert -> der erste Treffer ist hier der beste.
                    break;
                }

                return;
            }

            foreach (var v in values)
            {
                if (v.Points >= rest)
                    continue;

                path[index] = v;
                Step(rest - v.Points, index + 1, costSoFar + Cost(v, difficulty));
            }
        }

        Step(remaining, 0, 0);
        return best;
    }
}
