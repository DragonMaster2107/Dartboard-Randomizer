namespace Dartboard_Randomizer.Core.ViewModels;

/// <summary>
/// App-weite Einstellungen (per DI Singleton). <see cref="Changed"/> lässt abhängige
/// Komponenten (z.B. das Board) neu zeichnen.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Vertauscht die schwarzen/weißen Single-Felder der Scheibe.</summary>
    public bool SwapBoardColors { get; private set; }

    public event Action? Changed;

    public void SetSwapBoardColors(bool value)
    {
        if (value == SwapBoardColors)
            return;

        SwapBoardColors = value;
        Changed?.Invoke();
    }
}
