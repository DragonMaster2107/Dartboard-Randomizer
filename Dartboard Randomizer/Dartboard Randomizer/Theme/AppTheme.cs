using MudBlazor;

namespace Dartboard_Randomizer.Theme;

/// <summary>
/// Zentrale Design-Definition der App — das Gegenstück zum WPF-ResourceDictionary,
/// nur in C#. Alle MudBlazor-Controls ziehen ihre Farben automatisch aus dieser Palette.
/// Farbe global ändern = hier eine Zeile.
/// </summary>
public static class AppTheme
{
    public static readonly MudTheme Dartboard = new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#d32f2f",      // Dart-Rot
            Secondary = "#2e7d32",    // Grün (Board)
            Background = "#121212",
            Surface = "#1e1e1e",
            AppbarBackground = "#1e1e1e",
            DrawerBackground = "#1e1e1e",
            TextPrimary = "#f5f5f5",
            TextSecondary = "#b0b0b0",
        },
        PaletteLight = new PaletteLight
        {
            Primary = "#d32f2f",
            Secondary = "#2e7d32",
        },
    };
}
