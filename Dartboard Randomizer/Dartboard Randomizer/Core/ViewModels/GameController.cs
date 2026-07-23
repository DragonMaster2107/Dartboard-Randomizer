using Dartboard_Randomizer.Core.Models;

namespace Dartboard_Randomizer.Core.ViewModels;

/// <summary>
/// App-weiter Spielzustand — das "Shared ViewModel". Wird per DI (Singleton) in die
/// Pages injiziert und trägt den Zustand über Navigationen hinweg (Pages verlieren
/// ihren eigenen State beim Wechsel). Das <see cref="Changed"/>-Event ist das
/// Gegenstück zu INotifyPropertyChanged: Komponenten abonnieren es und rufen
/// darauf StateHasChanged auf.
/// </summary>
public sealed class GameController
{
    public GameState? Current { get; private set; }

    public bool HasActiveGame => Current is not null;

    /// <summary>Wird ausgelöst, wenn sich der Spielzustand ändert.</summary>
    public event Action? Changed;

    /// <summary>Startet ein neues Spiel aus den Setup-Einstellungen.</summary>
    public void StartGame(GameSettings settings)
    {
        Current = GameState.CreateNew(settings);
        NotifyChanged();
    }

    /// <summary>Beendet das aktuelle Spiel und verwirft den Zustand.</summary>
    public void EndGame()
    {
        Current = null;
        NotifyChanged();
    }

    private void NotifyChanged() => Changed?.Invoke();
}
