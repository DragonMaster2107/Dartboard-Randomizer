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
    private const string PlayersKey = "dartboard.lastPlayers";
    private readonly IJSRuntime _js;

    public GameStorage(IJSRuntime js) => _js = js;

    /// <summary>Merkt sich die zuletzt verwendeten Spielernamen (fürs Vorbefüllen des Setups).</summary>
    public async Task SaveLastPlayersAsync(IReadOnlyList<string> names)
    {
        var json = JsonSerializer.Serialize(names);
        await _js.InvokeVoidAsync("localStorage.setItem", PlayersKey, json);
    }

    public async Task<List<string>> LoadLastPlayersAsync()
    {
        var json = await _js.InvokeAsync<string?>("localStorage.getItem", PlayersKey);
        if (string.IsNullOrEmpty(json))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private const string SetupKey = "dartboard.lastSetup";

    /// <summary>Merkt sich die zuletzt gewählten Setup-Optionen (fürs Vorbelegen des Setups).</summary>
    public async Task SaveLastSetupAsync(SetupDefaults setup)
    {
        var json = JsonSerializer.Serialize(setup);
        await _js.InvokeVoidAsync("localStorage.setItem", SetupKey, json);
    }

    public async Task<SetupDefaults?> LoadLastSetupAsync()
    {
        var json = await _js.InvokeAsync<string?>("localStorage.getItem", SetupKey);
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<SetupDefaults>(json)?.Sanitized();
        }
        catch
        {
            return null; // korruptes/veraltetes Format -> Standardwerte
        }
    }

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

    private const string SwapColorsKey = "dartboard.settings.swapColors";

    public async Task SaveBoardColorSwapAsync(bool value)
        => await _js.InvokeVoidAsync("localStorage.setItem", SwapColorsKey, value ? "1" : "0");

    public async Task<bool> LoadBoardColorSwapAsync()
        => await _js.InvokeAsync<string?>("localStorage.getItem", SwapColorsKey) == "1";

    private const string ShowBannerKey = "dartboard.settings.showTurnBanner";

    public async Task SaveShowTurnBannerAsync(bool value)
        => await _js.InvokeVoidAsync("localStorage.setItem", ShowBannerKey, value ? "1" : "0");

    public async Task<bool> LoadShowTurnBannerAsync()
        => await _js.InvokeAsync<string?>("localStorage.getItem", ShowBannerKey) == "1";

    private const string HighlightCheckoutKey = "dartboard.settings.highlightCheckout";

    public async Task SaveHighlightCheckoutAsync(bool value)
        => await _js.InvokeVoidAsync("localStorage.setItem", HighlightCheckoutKey, value ? "1" : "0");

    /// <summary>
    /// ⚠ Anders als die übrigen Einstellungen ist diese standardmäßig <b>an</b>: nur ein
    /// explizites "0" schaltet ab, ein fehlender Eintrag gilt als „an".
    /// </summary>
    public async Task<bool> LoadHighlightCheckoutAsync()
        => await _js.InvokeAsync<string?>("localStorage.getItem", HighlightCheckoutKey) != "0";

    private sealed record PersistedGame(
        List<PlayerState> Players,
        int CurrentPlayerIndex,
        OutMode OutMode,
        int StartingScore,
        bool Randomize,
        bool HiddenValues,
        bool RevealDoesNotScore,
        int? Seed,
        List<FieldValue> CurrentTurn,
        int TurnStartScore,
        bool AwaitingContinueDecision,
        bool IsOver,
        List<BoardPosition> RevealedPositions)
    {
        public static PersistedGame From(GameState s) => new(
            s.Players.ToList(),
            s.CurrentPlayerIndex,
            s.OutMode,
            s.StartingScore,
            s.Randomize,
            s.HiddenValues,
            s.RevealDoesNotScore,
            s.Seed,
            s.CurrentTurn.ToList(),
            s.TurnStartScore,
            s.AwaitingContinueDecision,
            s.IsOver,
            s.RevealedPositions.ToList());

        public GameState ToState() => new()
        {
            Players = Players,
            CurrentPlayerIndex = CurrentPlayerIndex,
            OutMode = OutMode,
            StartingScore = StartingScore,
            Randomize = Randomize,
            HiddenValues = HiddenValues,
            RevealDoesNotScore = RevealDoesNotScore,
            Seed = Seed,
            CurrentTurn = CurrentTurn,
            TurnStartScore = TurnStartScore,
            AwaitingContinueDecision = AwaitingContinueDecision,
            IsOver = IsOver,
            RevealedPositions = RevealedPositions.ToHashSet(),
        };
    }
}
