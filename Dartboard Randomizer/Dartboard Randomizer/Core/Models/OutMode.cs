namespace Dartboard_Randomizer.Core.Models;

/// <summary>
/// Wie ein Spiel beendet werden darf (Checkout-Regel).
/// </summary>
public enum OutMode
{
    /// <summary>Beliebiges Feld beendet das Spiel.</summary>
    Straight,

    /// <summary>Muss auf einem Double (oder Bull) enden.</summary>
    Double,

    /// <summary>Muss auf einem Double oder Triple (oder Bull) enden.</summary>
    Master,
}
