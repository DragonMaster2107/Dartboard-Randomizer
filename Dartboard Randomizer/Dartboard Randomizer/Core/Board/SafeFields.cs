namespace Dartboard_Randomizer.Core.Board;

/// <summary>
/// Die Positionen, die im Conquest-Modus <b>allen</b> Spielern gehören und von niemandem
/// beansprucht werden können: die Wertigkeit <b>D1</b> und <b>EINE</b> der beiden
/// <b>Single 1</b>.
/// <para>
/// Sinn: ein garantierter Ausweg. Mit S1 (1 Punkt) kommt jeder Spieler auf jeden geraden
/// Rest herunter und mit D1 (2 Punkte) auf 0 — auch bei Double- oder Master-Out. Ohne
/// diese Ausnahme könnte ein Spieler von den Gegnern komplett zugemauert werden.
/// </para>
/// <para>
/// Hängt nur am Layout, ist also über den Seed reproduzierbar. Von den beiden
/// Single-1-Positionen wird bewusst nur die <b>erste in kanonischer Reihenfolge</b>
/// freigegeben — die zweite bleibt ein normal beanspruchbares Feld.
/// </para>
/// </summary>
public static class SafeFields
{
    private static readonly FieldValue Double1 = new(1, Multiplier.Double);
    private static readonly FieldValue Single1 = new(1, Multiplier.Single);

    public static IReadOnlySet<BoardPosition> For(BoardLayout layout)
    {
        var shared = new HashSet<BoardPosition>();
        shared.UnionWith(layout.PositionsOf(Double1));          // D1 liegt genau einmal
        shared.UnionWith(layout.PositionsOf(Single1).Take(1));  // von den zwei S1 nur eine
        return shared;
    }
}
