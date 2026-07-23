namespace Dartboard_Randomizer.Core.Board;

/// <summary>
/// Die Wertigkeit eines Treffers (Basiszahl × Multiplikator).
/// Das Bull wird als BaseNumber 25 modelliert: Single 25 = Outer Bull (25 Punkte),
/// Double 25 = Inner Bull (50 Punkte, zählt fürs Double-Out).
/// </summary>
public readonly record struct FieldValue(int BaseNumber, Multiplier Multiplier)
{
    public int Points => BaseNumber * (int)Multiplier;

    public bool IsBull => BaseNumber == 25;
    public bool IsDouble => Multiplier == Multiplier.Double;
    public bool IsTriple => Multiplier == Multiplier.Triple;

    /// <summary>Kurzlabel fürs Board, z.B. "T20", "D16", "25", "BULL".</summary>
    public string ShortLabel => Multiplier switch
    {
        Multiplier.Triple => $"T{BaseNumber}",
        Multiplier.Double => IsBull ? "BULL" : $"D{BaseNumber}",
        _ => IsBull ? "25" : BaseNumber.ToString(),
    };
}
