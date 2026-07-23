using Dartboard_Randomizer.Core.Models;

namespace Dartboard_Randomizer.Tests;

public class GameStateTests
{
    private static GameSettings Settings(
        int startingScore = 501,
        OutMode outMode = OutMode.Double,
        bool randomize = false,
        bool hidden = false,
        int? seed = null,
        params string[] players)
        => new(players.Length == 0 ? new[] { "Alice", "Bob" } : players,
               startingScore, outMode, randomize, hidden, seed);

    [Fact]
    public void CreateNew_gives_every_player_the_starting_score()
    {
        var state = GameState.CreateNew(Settings(startingScore: 301, players: new[] { "Alice", "Bob", "Cara" }));

        Assert.Equal(3, state.Players.Count);
        Assert.All(state.Players, p => Assert.Equal(301, p.Score));
    }

    [Fact]
    public void CreateNew_starts_with_the_first_player()
    {
        var state = GameState.CreateNew(Settings(players: new[] { "Alice", "Bob" }));

        Assert.Equal(0, state.CurrentPlayerIndex);
        Assert.Equal("Alice", state.CurrentPlayer.Name);
    }

    [Fact]
    public void CreateNew_carries_over_all_game_options()
    {
        var state = GameState.CreateNew(
            Settings(startingScore: 701, outMode: OutMode.Master, randomize: true, hidden: true, seed: 42,
                     players: new[] { "Alice" }));

        Assert.Equal(701, state.StartingScore);
        Assert.Equal(OutMode.Master, state.OutMode);
        Assert.True(state.Randomize);
        Assert.True(state.HiddenValues);
        Assert.Equal(42, state.Seed);
    }

    [Fact]
    public void CreateNew_keeps_player_order()
    {
        var state = GameState.CreateNew(Settings(players: new[] { "Cara", "Alice", "Bob" }));

        Assert.Equal(new[] { "Cara", "Alice", "Bob" }, state.Players.Select(p => p.Name));
    }
}
