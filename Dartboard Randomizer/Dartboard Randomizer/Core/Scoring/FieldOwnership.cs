using Dartboard_Randomizer.Core.Board;
using Dartboard_Randomizer.Core.Models;

namespace Dartboard_Randomizer.Core.Scoring;

/// <summary>
/// Die Besitz-Regeln des Conquest-Modifikators — rein und seitenwirkungsfrei, damit sie ohne
/// UI testbar sind (die Hidden-Variante der Checkout-Markierung war genau deshalb schon
/// einmal fehlerhaft, siehe <c>CheckoutTargets</c>).
/// <para>
/// Kernregel: Wer eine Position <b>zuerst trifft</b>, besitzt sie. Jeder weitere Treffer
/// darauf — von wem auch immer — wird dem Besitzer angerechnet. Der Besitz hängt an der
/// <b>Position</b>, nicht an der Wertigkeit: die beiden Single-7-Felder sind zwei
/// getrennte Eroberungen.
/// </para>
/// </summary>
public static class FieldOwnership
{
    /// <summary>
    /// Der Spieler, dem diese Position aktuell Punkte einbringt — oder <c>null</c>, wenn
    /// sie frei ist.
    /// <para>
    /// ⚠ Ein Besitzer, der <b>ausgecheckt</b> hat, zählt nicht mehr: sein Feld wird wieder
    /// frei und ist neu beanspruchbar. Sonst blieben am Spielende Felder übrig, die
    /// niemandem mehr nützen.
    /// </para>
    /// <para>⚠ Die gemeinsamen Sicherheitsfelder (<see cref="SafeFields"/>) sind nie besetzt.</para>
    /// </summary>
    public static int? ActiveOwner(GameState state, BoardPosition position)
    {
        if (!state.Conquest || state.SharedPositions.Contains(position))
            return null;

        if (!state.FieldOwners.TryGetValue(position, out var index))
            return null;

        // Defensiv gegen manipulierten/veralteten Storage.
        if (index < 0 || index >= state.Players.Count)
            return null;

        return state.Players[index].HasFinished ? null : index;
    }

    /// <summary>
    /// Wem der Dart auf diese Position angerechnet wird: dem Besitzer, sonst dem Werfer
    /// (freie, gemeinsame und wieder freigewordene Felder gehen an den Werfer).
    /// </summary>
    public static int ScoringPlayer(GameState state, BoardPosition? position, int throwerIndex)
        => position is BoardPosition pos ? ActiveOwner(state, pos) ?? throwerIndex : throwerIndex;

    /// <summary>
    /// Beansprucht der Werfer diese Position mit dem Treffer? Nur freie, nicht gemeinsame
    /// Felder wechseln den Besitzer.
    /// </summary>
    public static bool ConquersOnHit(GameState state, BoardPosition position)
        => state.Conquest
           && !state.SharedPositions.Contains(position)
           && ActiveOwner(state, position) is null;

    /// <summary>
    /// Die Positionen, aus denen der Spieler selbst Punkte zieht: eigene, gemeinsame und
    /// freie. Basis für Checkout-Vorschlag und -Markierung — fremde Felder würden dort
    /// einen Weg vorschlagen, der die Punkte dem Gegner schenkt.
    /// </summary>
    public static IReadOnlySet<BoardPosition> UsableBy(
        GameState state, int playerIndex, IEnumerable<BoardPosition> candidates)
    {
        var usable = new HashSet<BoardPosition>();
        foreach (var position in candidates)
        {
            var owner = ActiveOwner(state, position);
            if (owner is null || owner == playerIndex)
                usable.Add(position);
        }
        return usable;
    }

    /// <summary>
    /// Die Positionen, die einem <b>anderen</b>, noch mitspielenden Spieler gehören — die
    /// werden auf dem Board rot markiert. Felder ausgecheckter Spieler sind wieder frei
    /// und erscheinen deshalb nicht.
    /// </summary>
    public static IReadOnlySet<BoardPosition> ForeignTo(GameState state, int playerIndex)
    {
        var foreign = new HashSet<BoardPosition>();
        if (!state.Conquest)
            return foreign;

        foreach (var position in state.FieldOwners.Keys)
        {
            var owner = ActiveOwner(state, position);
            if (owner is int index && index != playerIndex)
                foreign.Add(position);
        }
        return foreign;
    }
}
