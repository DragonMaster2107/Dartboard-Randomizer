namespace Dartboard_Randomizer.Core.Models;

/// <summary>
/// Zustand eines Spielers während des Spiels. Immutable (record) — Änderungen
/// erzeugen per <c>with</c> eine neue Instanz, was den späteren Undo-Stack einfach macht.
/// </summary>
public sealed record PlayerState(string Name, int Score);
