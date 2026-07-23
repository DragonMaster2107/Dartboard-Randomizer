namespace Dartboard_Randomizer.Core.Board;

/// <summary>Die radiale Zone eines physischen Feldes auf der Scheibe.</summary>
public enum BoardRing
{
    InnerSingle,
    OuterSingle,
    Triple,
    Double,
    OuterBull,
    InnerBull,
}

/// <summary>
/// Eine physische Position auf der Scheibe (wo der Dart landet) — wertneutral.
/// <see cref="Number"/> ist die aufgedruckte Zahl 1..20 (bzw. 25 für die Bulls),
/// also nur die Orientierung; welche Wertigkeit dort liegt, sagt das <see cref="BoardLayout"/>.
/// </summary>
public readonly record struct BoardPosition(int Number, BoardRing Ring);
