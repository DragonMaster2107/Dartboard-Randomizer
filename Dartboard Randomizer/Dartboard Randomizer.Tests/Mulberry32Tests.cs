using Dartboard_Randomizer.Core.Board;

namespace Dartboard_Randomizer.Tests;

public class Mulberry32Tests
{
    [Fact]
    public void Same_seed_yields_the_same_sequence()
    {
        var a = new Mulberry32(7);
        var b = new Mulberry32(7);

        for (var i = 0; i < 50; i++)
            Assert.Equal(a.NextUInt(), b.NextUInt());
    }

    [Fact]
    public void Different_seeds_diverge()
    {
        var a = new Mulberry32(1);
        var b = new Mulberry32(2);

        var anyDifferent = false;
        for (var i = 0; i < 10; i++)
            anyDifferent |= a.NextUInt() != b.NextUInt();

        Assert.True(anyDifferent);
    }

    [Fact]
    public void Next_stays_within_range()
    {
        var rng = new Mulberry32(123);

        for (var i = 0; i < 1000; i++)
        {
            var value = rng.Next(10);
            Assert.InRange(value, 0, 9);
        }
    }

    [Fact]
    public void Next_throws_for_non_positive_bound()
    {
        var rng = new Mulberry32(123);
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.Next(0));
    }
}
