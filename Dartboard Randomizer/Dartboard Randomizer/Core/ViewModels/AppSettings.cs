namespace Dartboard_Randomizer.Core.ViewModels;

/// <summary>
/// App-weite Einstellungen (per DI Singleton). <see cref="Changed"/> lässt abhängige
/// Komponenten (z.B. das Board) neu zeichnen.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Vertauscht die schwarzen/weißen Single-Felder der Scheibe.</summary>
    public bool SwapBoardColors { get; private set; }

    /// <summary>Zeigt auf Mobilgeräten ein festes „Turn-HUD"-Banner oben (Standard: aus).</summary>
    public bool ShowTurnBanner { get; private set; }

    public event Action? Changed;

    public void SetSwapBoardColors(bool value)
    {
        if (value == SwapBoardColors)
            return;

        SwapBoardColors = value;
        Changed?.Invoke();
    }

    public void SetShowTurnBanner(bool value)
    {
        if (value == ShowTurnBanner)
            return;

        ShowTurnBanner = value;
        Changed?.Invoke();
    }
}
