// PerformanceGrade.cs
namespace Custom5v5.Application.Matches.Scoring.Performance.Models
{
    public enum PerformanceGrade
    {
        F = 0,
        D = 1,
        C = 2,
        B = 3,
        A = 4,
        S = 5
    }

    public static class PerformanceGradeRules
    {
        // Simple, stable, ajustable plus tard. Pas de sur-design.
        public static PerformanceGrade FromScore(double score0To100)
        {
            if (score0To100 >= 90) return PerformanceGrade.S;
            if (score0To100 >= 80) return PerformanceGrade.A;
            if (score0To100 >= 70) return PerformanceGrade.B;
            if (score0To100 >= 60) return PerformanceGrade.C;
            if (score0To100 >= 50) return PerformanceGrade.D;
            return PerformanceGrade.F;
        }
    }
}