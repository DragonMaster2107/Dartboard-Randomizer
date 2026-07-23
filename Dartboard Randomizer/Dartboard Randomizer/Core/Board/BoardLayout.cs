namespace Dartboard_Randomizer.Core.Board;

/// <summary>
/// Die Zuordnung physische Position → Wertigkeit für ein Spiel.
/// <see cref="Standard"/> = echte Scheibe. <see cref="Shuffled"/> permutiert NUR die
/// vorhandenen Wertigkeiten über die Positionen (via Seed reproduzierbar) — der
/// Wertevorrat bleibt identisch zur echten Scheibe, das Spiel also fair.
/// </summary>
public sealed class BoardLayout
{
    /// <summary>Alle 82 physischen Positionen in kanonischer Reihenfolge.</summary>
    public static readonly IReadOnlyList<BoardPosition> AllPositions = BuildPositions();

    private static readonly Dictionary<BoardPosition, int> IndexOf =
        AllPositions.Select((p, i) => (p, i)).ToDictionary(x => x.p, x => x.i);

    private readonly FieldValue[] _values; // parallel zu AllPositions

    public int? Seed { get; }

    private BoardLayout(FieldValue[] values, int? seed)
    {
        _values = values;
        Seed = seed;
    }

    public FieldValue ValueAt(BoardPosition position) => _values[IndexOf[position]];

    public static BoardLayout Standard() => new(StandardValues(), seed: null);

    public static BoardLayout Shuffled(int seed)
    {
        var values = StandardValues();
        var rng = new Mulberry32(seed);

        // Fisher-Yates
        for (var i = values.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }

        return new BoardLayout(values, seed);
    }

    private static List<BoardPosition> BuildPositions()
    {
        var list = new List<BoardPosition>(82);
        for (var n = 1; n <= 20; n++)
        {
            list.Add(new BoardPosition(n, BoardRing.InnerSingle));
            list.Add(new BoardPosition(n, BoardRing.OuterSingle));
            list.Add(new BoardPosition(n, BoardRing.Triple));
            list.Add(new BoardPosition(n, BoardRing.Double));
        }
        list.Add(new BoardPosition(25, BoardRing.OuterBull));
        list.Add(new BoardPosition(25, BoardRing.InnerBull));
        return list;
    }

    // Muss exakt der Reihenfolge von BuildPositions() folgen.
    private static FieldValue[] StandardValues()
    {
        var list = new List<FieldValue>(82);
        for (var n = 1; n <= 20; n++)
        {
            list.Add(new FieldValue(n, Multiplier.Single)); // inner single
            list.Add(new FieldValue(n, Multiplier.Single)); // outer single
            list.Add(new FieldValue(n, Multiplier.Triple)); // triple
            list.Add(new FieldValue(n, Multiplier.Double)); // double
        }
        list.Add(new FieldValue(25, Multiplier.Single)); // outer bull  = 25
        list.Add(new FieldValue(25, Multiplier.Double)); // inner bull  = 50
        return list.ToArray();
    }
}
