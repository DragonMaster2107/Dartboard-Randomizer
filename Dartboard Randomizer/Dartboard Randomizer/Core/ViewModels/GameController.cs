using Dartboard_Randomizer.Core.Board;
using Dartboard_Randomizer.Core.Models;
using Dartboard_Randomizer.Core.Scoring;

namespace Dartboard_Randomizer.Core.ViewModels;

/// <summary>
/// App-weiter Spielzustand — das "Shared ViewModel". Wird per DI (Singleton) in die
/// Pages injiziert und trägt den Zustand über Navigationen hinweg. Das
/// <see cref="Changed"/>-Event ist das Gegenstück zu INotifyPropertyChanged.
/// </summary>
public sealed class GameController
{
    private readonly Stack<GameState> _undo = new();

    public GameState? Current { get; private set; }

    public bool HasActiveGame => Current is not null;

    public bool CanUndo => _undo.Count > 0;

    /// <summary>Wird ausgelöst, wenn sich der Spielzustand ändert.</summary>
    public event Action? Changed;

    /// <summary>Startet ein neues Spiel aus den Setup-Einstellungen.</summary>
    public void StartGame(GameSettings settings)
    {
        _undo.Clear();
        Current = GameState.CreateNew(settings);
        NotifyChanged();
    }

    /// <summary>
    /// Verarbeitet einen geworfenen Dart. Bei einem Board-Treffer im Hidden-Modus wird
    /// zusätzlich die getroffene Position aufgedeckt (Teil des State → Undo nimmt es zurück).
    /// </summary>
    public void RecordThrow(FieldValue dart, BoardPosition? reveal = null)
    {
        if (Current is null || Current.IsFinished)
            return;

        _undo.Push(Current);

        var next = ScoringEngine.ApplyThrow(Current, dart);
        if (reveal is BoardPosition pos && !next.RevealedPositions.Contains(pos))
            next = next with { RevealedPositions = new HashSet<BoardPosition>(next.RevealedPositions) { pos } };

        Current = next;
        NotifyChanged();
    }

    /// <summary>Macht den letzten Dart rückgängig (auch über Spielerwechsel/Bust hinweg).</summary>
    public void Undo()
    {
        if (_undo.Count == 0)
            return;

        Current = _undo.Pop();
        NotifyChanged();
    }

    /// <summary>Stellt ein gespeichertes Spiel wieder her (z.B. nach Reload).</summary>
    public void Restore(GameState state)
    {
        _undo.Clear();
        Current = state;
        NotifyChanged();
    }

    /// <summary>Beendet das aktuelle Spiel und verwirft den Zustand.</summary>
    public void EndGame()
    {
        _undo.Clear();
        Current = null;
        NotifyChanged();
    }

    private void NotifyChanged() => Changed?.Invoke();
}
