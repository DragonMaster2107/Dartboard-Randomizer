using Dartboard_Randomizer.Core.Board;
using Dartboard_Randomizer.Core.Models;

namespace Dartboard_Randomizer.Core.Scoring;

/// <summary>
/// Auswertung der Aufdeck-Anteile im Hidden-Modus — wer hat wie viele Felder freigelegt.
/// Reine Funktionen, damit die Statistik-Seite keine Logik trägt.
/// </summary>
public static class RevealStats
{
    /// <summary>
    /// Wie viele Felder jeder Spieler aufgedeckt hat, indiziert wie
    /// <see cref="GameState.Players"/>.
    /// </summary>
    public static IReadOnlyList<int> CountsByPlayer(GameState state)
    {
        var counts = new int[state.Players.Count];
        foreach (var index in state.RevealedBy.Values)
        {
            // Defensiv gegen manipulierten/veralteten Storage.
            if (index >= 0 && index < counts.Length)
                counts[index]++;
        }
        return counts;
    }

    /// <summary>
    /// Die Anzahl der Felder, die überhaupt aufgedeckt werden <b>kann</b> — also alle
    /// Positionen abzüglich der gemeinsamen Sicherheitsfelder.
    /// <para>
    /// ⚠ Die Sicherheitsfelder fliegen aus dem Nenner, statt als eigener Anteil zu
    /// erscheinen: sie waren nie verdeckt, gehören also nicht in eine Quote über
    /// „aufgedeckte Felder". Ohne Conquest ist die Menge leer und der Nenner sind alle 82.
    /// </para>
    /// </summary>
    public static int Revealable(GameState state)
        => BoardLayout.AllPositions.Count - state.SharedPositions.Count;

    /// <summary>
    /// Alles, was keinem Spieler zugerechnet wird — der graue Rest im Diagramm. Setzt sich
    /// aus <see cref="UncreditedReveals"/> und <see cref="StillHidden"/> zusammen.
    /// </summary>
    public static int Unattributed(GameState state)
        => Math.Max(0, Revealable(state) - state.RevealedBy.Count);

    /// <summary>
    /// Felder, die per „Reveal all" sichtbar wurden und deshalb <b>niemandem</b> gehören.
    /// <para>
    /// Jede aufgedeckte Position ist entweder einem Spieler zugeordnet, ein gemeinsames
    /// Sicherheitsfeld oder eben per Knopfdruck aufgedeckt — daraus folgt die Differenz.
    /// </para>
    /// <para>
    /// ⚠ Der Grund, warum „Owned by" mehr Felder zeigen kann als „Revealed by": nach einem
    /// „Reveal all" erobern Treffer weiterhin Felder, decken aber keine mehr auf.
    /// </para>
    /// </summary>
    public static int UncreditedReveals(GameState state)
        => Math.Max(0, state.RevealedPositions.Count - state.RevealedBy.Count - state.SharedPositions.Count);

    /// <summary>Felder, die tatsächlich noch verdeckt sind.</summary>
    public static int StillHidden(GameState state)
        => Math.Max(0, Unattributed(state) - UncreditedReveals(state));
}
