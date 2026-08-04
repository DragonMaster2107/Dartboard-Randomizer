using Dartboard_Randomizer.Core.Models;

namespace Dartboard_Randomizer.Tests;

/// <summary>
/// Modus-Registry und das Bereinigen gespeicherter Setups — reine Logik, deshalb hier
/// und nicht in der Page.
/// </summary>
public class GameModeTests
{
    [Fact]
    public void All_ContainsX01()
    {
        Assert.Contains(GameModes.All, m => m.Id == GameMode.X01);
    }

    [Fact]
    public void All_HasUniqueIds()
    {
        Assert.Equal(GameModes.All.Count, GameModes.All.Select(m => m.Id).Distinct().Count());
    }

    [Fact]
    public void Get_UnknownMode_FallsBackToX01()
    {
        // Simuliert einen Wert aus altem/manipuliertem Storage.
        Assert.Equal(GameMode.X01, GameModes.Get((GameMode)99).Id);
    }

    [Fact]
    public void Sanitized_UnknownMode_BecomesX01()
    {
        var setup = SetupDefaults.Initial with { Mode = (GameMode)99 };

        Assert.Equal(GameMode.X01, setup.Sanitized().Mode);
    }

    [Fact]
    public void Sanitized_KeepsModifiersForModeThatSupportsThem()
    {
        var setup = SetupDefaults.Initial with
        {
            Mode = GameMode.X01,
            Randomize = true,
            HiddenValues = true,
            RevealDoesNotScore = true,
        };

        var clean = setup.Sanitized();

        Assert.True(clean.Randomize);
        Assert.True(clean.HiddenValues);
        Assert.True(clean.RevealDoesNotScore);
    }

    [Fact]
    public void Sanitized_HiddenWithoutRandomize_IsDropped()
    {
        var setup = SetupDefaults.Initial with
        {
            Randomize = false,
            HiddenValues = true,
            RevealDoesNotScore = true,
        };

        var clean = setup.Sanitized();

        Assert.False(clean.HiddenValues);
        Assert.False(clean.RevealDoesNotScore);
    }

    [Fact]
    public void Initial_DefaultsToX01()
    {
        Assert.Equal(GameMode.X01, SetupDefaults.Initial.Mode);
    }

    [Fact]
    public void X01_MustStayZero()
    {
        // Gespeicherte Stände von vor der Modus-Einführung haben kein Mode-Feld;
        // System.Text.Json belegt es mit default(GameMode) -> muss X01 sein.
        Assert.Equal(0, (int)GameMode.X01);
    }

    [Fact]
    public void CreateNew_CarriesModeIntoState()
    {
        var settings = new GameSettings(
            PlayerNames: new[] { "A" },
            StartingScore: 501,
            OutMode: OutMode.Double,
            Randomize: false,
            HiddenValues: false,
            Seed: null)
        {
            Mode = GameMode.X01,
        };

        Assert.Equal(GameMode.X01, GameState.CreateNew(settings).Mode);
    }
}
