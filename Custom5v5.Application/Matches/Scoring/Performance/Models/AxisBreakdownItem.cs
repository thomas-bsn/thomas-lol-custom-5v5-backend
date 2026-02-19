// AxisBreakdownItem.cs
namespace Custom5v5.Application.Matches.Scoring.Performance.Models
{
    public sealed class AxisBreakdownItem
    {
        // Exemple: "Deaths", "VisionScore", "Gold/min"
        public string Label { get; init; } = string.Empty;

        // Valeur brute affichable (string pour éviter les formats foireux côté UI)
        // Exemple: "7", "42", "385.5"
        public string Value { get; init; } = string.Empty;

        // Contribution à l’axe (peut être négative)
        // Exemple: +6.3, -11.3
        public double Points { get; init; }

        // Optionnel: petite explication (tooltip)
        public string? Note { get; init; }
    }
}