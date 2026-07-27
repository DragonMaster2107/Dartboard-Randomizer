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
    public static IReadOnlyList<FieldValue>? Suggest(
        int remaining, int dartsLeft, OutMode outMode, IReadOnlyCollection<FieldValue> available)
    {
        if (remaining <= 0 || dartsLeft <= 0)
            return null;

        // Höhere Werte zuerst -> "schönere" Vorschläge (z.B. T20 vor kleinen Feldern).
        var values = available
            .Where(v => v.Points > 0)
            .Distinct()
            .OrderByDescending(v => v.Points)
            .ToList();

        // Kürzeste Lösung bevorzugen: erst 1 Dart, dann 2, dann 3 ...
        for (var depth = 1; depth <= dartsLeft; depth++)
        {
            var path = Find(remaining, depth, outMode, values);
            if (path != null)
                return path;
        }

        return null;
    }

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
}
