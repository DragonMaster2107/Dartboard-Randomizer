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
        if (Current is null || !Current.AcceptsThrows)
            return;

        _undo.Push(Current);

        // Option "RevealDoesNotScore": ein Dart, der ein Feld ERSTMALIG aufdeckt, zählt 0.
        var isNewReveal = reveal is BoardPosition rp && !Current.RevealedPositions.Contains(rp);
        var scoringDart = isNewReveal && Current.HiddenValues && Current.RevealDoesNotScore
            ? FieldValue.Miss
            : dart;

        // ⚠ VOR ApplyThrow festhalten: nach dem dritten Dart hat der Spieler gewechselt,
        // aufgedeckt hat das Feld aber der, der geworfen hat.
        var revealer = Current.CurrentPlayerIndex;

        // Die Position geht mit in die Engine: im Conquest-Modus entscheidet sie, wem die
        // Punkte angerechnet werden (und wer das Feld beansprucht).
        var next = ScoringEngine.ApplyThrow(Current, scoringDart, reveal);
        if (reveal is BoardPosition pos && !next.RevealedPositions.Contains(pos))
        {
            next = next with
            {
                RevealedPositions = new HashSet<BoardPosition>(next.RevealedPositions) { pos },
                // Aufdecker für die Statistik merken (nur beim ERSTEN Aufdecken).
                RevealedBy = new Dictionary<BoardPosition, int>(next.RevealedBy) { [pos] = revealer },
            };
        }

        Current = next;
        NotifyChanged();
    }

    /// <summary>
    /// Deckt im Hidden-Modus das komplette Board auf (rückgängig machbar).
    /// <para>
    /// ⚠ <c>RevealedBy</c> bleibt unberührt: diese Felder hat kein Spieler erspielt, sie
    /// dürfen also in der Aufdeck-Statistik niemandem angerechnet werden.
    /// </para>
    /// </summary>
    public void RevealAll()
    {
        if (Current is null || !Current.AcceptsThrows)
            return;

        _undo.Push(Current);
        Current = Current with { RevealedPositions = new HashSet<BoardPosition>(BoardLayout.AllPositions) };
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

    /// <summary>Antwort auf die Ausspiel-Abfrage: "Ja" — die restlichen Spieler spielen weiter.</summary>
    public void ContinuePlaying()
    {
        if (Current is null || !Current.AwaitingContinueDecision)
            return;

        Current = ScoringEngine.ResumeAfterFinish(Current);
        NotifyChanged();
    }

    /// <summary>Antwort auf die Ausspiel-Abfrage: "Nein" — Spiel sofort beenden (→ Statistik).</summary>
    public void EndNow()
    {
        if (Current is null || Current.IsOver)
            return;

        Current = Current with { AwaitingContinueDecision = false, IsOver = true };
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
