using Dartboard_Randomizer.Core.Models;
using Dartboard_Randomizer.Core.ViewModels;

namespace Dartboard_Randomizer.Tests;

public class GameControllerTests
{
    private static GameSettings SampleSettings()
        => new(new[] { "Alice", "Bob" }, 501, OutMode.Double, false, false, null);

    [Fact]
    public void New_controller_has_no_active_game()
    {
        var controller = new GameController();

        Assert.False(controller.HasActiveGame);
        Assert.Null(controller.Current);
    }

    [Fact]
    public void StartGame_creates_an_active_game_from_the_settings()
    {
        var controller = new GameController();

        controller.StartGame(SampleSettings());

        Assert.True(controller.HasActiveGame);
        Assert.NotNull(controller.Current);
        Assert.Equal(2, controller.Current!.Players.Count);
        Assert.Equal(501, controller.Current.CurrentPlayer.Score);
    }

    [Fact]
    public void StartGame_raises_Changed()
    {
        var controller = new GameController();
        var raised = 0;
        controller.Changed += () => raised++;

        controller.StartGame(SampleSettings());

        Assert.Equal(1, raised);
    }

    [Fact]
    public void EndGame_clears_the_state_and_raises_Changed()
    {
        var controller = new GameController();
        controller.StartGame(SampleSettings());
        var raised = 0;
        controller.Changed += () => raised++;

        controller.EndGame();

        Assert.False(controller.HasActiveGame);
        Assert.Null(controller.Current);
        Assert.Equal(1, raised);
    }
}
