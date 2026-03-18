namespace Custom5v5.Application.DTOs.Players;

public record UpdatePeakRequest(
    string? PeakTier,
    int? PeakDivision,
    string? PeakSeason,
    int PeakLp
);