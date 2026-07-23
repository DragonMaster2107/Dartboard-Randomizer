namespace Dartboard_Randomizer.Core.Board;

/// <summary>
/// Kleiner, deterministischer PRNG (mulberry32). Bewusst NICHT System.Random,
/// da dessen interne Sequenz sich zwischen .NET-Versionen ändern kann — hier wollen
/// wir, dass derselbe Seed über Versionen/Plattformen hinweg exakt dasselbe Board ergibt.
/// </summary>
public sealed class Mulberry32
{
    private uint _state;

    public Mulberry32(int seed) => _state = unchecked((uint)seed);

    public uint NextUInt()
    {
        unchecked
        {
            _state += 0x6D2B79F5u;
            uint t = _state;
            t = (t ^ (t >> 15)) * (t | 1u);
            t ^= t + (t ^ (t >> 7)) * (t | 61u);
            return t ^ (t >> 14);
        }
    }

    /// <summary>Ganzzahl im Bereich [0, maxExclusive).</summary>
    public int Next(int maxExclusive)
    {
        if (maxExclusive <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));

        return (int)(NextUInt() % (uint)maxExclusive);
    }
}
