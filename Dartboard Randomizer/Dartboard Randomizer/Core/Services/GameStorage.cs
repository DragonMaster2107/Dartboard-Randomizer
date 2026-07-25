using System.Text.Json;
using Dartboard_Randomizer.Core.Board;
using Dartboard_Randomizer.Core.Models;
using Microsoft.JSInterop;

namespace Dartboard_Randomizer.Core.Services;

/// <summary>
/// Speichert/lädt das laufende Spiel im localStorage (via JS-Interop), damit ein
/// Reload es nicht verwirft. Serialisiert über ein DTO mit konkreten Typen — die
/// Interface-Properties von <see cref="GameState"/> (IReadOnlyList/IReadOnlySet)
/// lassen sich sonst nicht zuverlässig deserialisieren.
/// </summary>
public sealed class GameStorage
{
    private const string Key = "dartboard.game";
    private readonly IJSRuntime _js;

    public GameStorage(IJSRuntime js) => _js = js;

    public async Task SaveAsync(GameState state)
    {
        var json = JsonSerializer.Serialize(PersistedGame.From(state));
        await _js.InvokeVoidAsync("localStorage.setItem", Key, json);
    }

    public async Task<GameState?> LoadAsync()
    {
        var json = await _js.InvokeAsync<string?>("localStorage.getItem", Key);
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<PersistedGame>(json)?.ToState();
        }
        catch
        {
            return null; // korruptes/veraltetes Format -> ignorieren
        }
    }

    public async Task ClearAsync() => await _js.InvokeVoidAsync("localStorage.removeItem", Key);

    private sealed record PersistedGame(
        List<PlayerState> Players,
        int CurrentPlayerIndex,
        OutMode OutMode,
        int StartingScore,
        bool Randomize,
        bool HiddenValues,
        int? Seed,
        List<FieldValue> CurrentTurn,
        int TurnStartScore,
        int? WinnerIndex,
        List<BoardPosition> RevealedPositions)
    {
        public static PersistedGame From(GameState s) => new(
            s.Players.ToList(),
            s.CurrentPlayerIndex,
            s.OutMode,
            s.StartingScore,
            s.Randomize,
            s.HiddenValues,
            s.Seed,
            s.CurrentTurn.ToList(),
            s.TurnStartScore,
            s.WinnerIndex,
            s.RevealedPositions.ToList());

        public GameState ToState() => new()
        {
            Players = Players,
            CurrentPlayerIndex = CurrentPlayerIndex,
            OutMode = OutMode,
            StartingScore = StartingScore,
            Randomize = Randomize,
            HiddenValues = HiddenValues,
            Seed = Seed,
            CurrentTurn = CurrentTurn,
            TurnStartScore = TurnStartScore,
            WinnerIndex = WinnerIndex,
            RevealedPositions = RevealedPositions.ToHashSet(),
        };
    }
}
